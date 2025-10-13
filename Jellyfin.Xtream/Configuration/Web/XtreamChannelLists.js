export default function (view) {
  view.addEventListener("viewshow", () => import(
    ApiClient.getUrl("web/ConfigurationPage", {
      name: "Xtream.js",
    })
  ).then((Xtream) => Xtream.default
  ).then((Xtream) => {
    const pluginId = Xtream.pluginConfig.UniqueId;
    Xtream.setTabs(4);

    let currentConfig = null;
    let currentListId = null;
    let matchingQueue = [];
    let currentMatchIndex = 0;
    let allStreams = [];

    // Load configuration
    function loadConfig() {
      return ApiClient.getPluginConfiguration(pluginId).then((config) => {
        currentConfig = config;
        if (!config.ChannelLists) config.ChannelLists = [];
        if (!config.ChannelMappings) config.ChannelMappings = {};
        renderLists();
        return config;
      });
    }

    // Render all lists
    function renderLists() {
      const container = view.querySelector('#channelListsContainer');
      container.innerHTML = '';

      if (!currentConfig.ChannelLists || currentConfig.ChannelLists.length === 0) {
        container.innerHTML = '<p style="color: #888; padding: 20px;">Keine Listen vorhanden. Klicken Sie auf "Neue Liste hinzufügen" um zu beginnen.</p>';
        return;
      }

      currentConfig.ChannelLists.forEach((list) => {
        const mappedCount = list.Entries.filter(entry => {
          const key = `${list.Id}_${entry}`;
          return currentConfig.ChannelMappings[key];
        }).length;

        const listDiv = document.createElement('div');
        listDiv.style.cssText = 'background: #2a2a2a; padding: 20px; margin: 15px 0; border-radius: 6px;';
        listDiv.innerHTML = `
          <div style="display: flex; justify-content: space-between; align-items: center;">
            <div>
              <h3 style="margin: 0 0 10px 0;">${escapeHtml(list.Name)}</h3>
              <p style="margin: 0; color: #888;">
                ${list.Entries.length} Sender gesamt | ${mappedCount} zugeordnet
              </p>
            </div>
            <div>
              <button class="btn-edit-list raised emby-button" data-list-id="${list.Id}">
                <span>Bearbeiten</span>
              </button>
              <button class="btn-match-list raised button-submit emby-button" data-list-id="${list.Id}">
                <span>Sender zuordnen</span>
              </button>
              <button class="btn-delete-list raised emby-button" data-list-id="${list.Id}" style="background: #d32f2f;">
                <span>Löschen</span>
              </button>
            </div>
          </div>
        `;
        container.appendChild(listDiv);
      });

      // Attach event listeners
      container.querySelectorAll('.btn-edit-list').forEach(btn => {
        btn.addEventListener('click', () => editList(btn.getAttribute('data-list-id')));
      });
      container.querySelectorAll('.btn-match-list').forEach(btn => {
        btn.addEventListener('click', () => startMatching(btn.getAttribute('data-list-id')));
      });
      container.querySelectorAll('.btn-delete-list').forEach(btn => {
        btn.addEventListener('click', () => deleteList(btn.getAttribute('data-list-id')));
      });
    }

    function escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text;
      return div.innerHTML;
    }

    // Show modal for adding new list
    view.querySelector('#btnAddList').addEventListener('click', () => {
      currentListId = null;
      view.querySelector('#modalTitle').textContent = 'Neue Senderliste';
      view.querySelector('#listName').value = '';
      view.querySelector('#listContent').value = '';
      view.querySelector('#listModal').style.display = 'block';
    });

    // Edit existing list
    function editList(listId) {
      const list = currentConfig.ChannelLists.find(l => l.Id === listId);
      if (!list) return;

      currentListId = listId;
      view.querySelector('#modalTitle').textContent = 'Senderliste bearbeiten';
      view.querySelector('#listName').value = list.Name;
      view.querySelector('#listContent').value = list.Entries.join('\n');
      view.querySelector('#listModal').style.display = 'block';
    }

    // Delete list
    function deleteList(listId) {
      if (!confirm('Möchten Sie diese Liste wirklich löschen?')) return;

      currentConfig.ChannelLists = currentConfig.ChannelLists.filter(l => l.Id !== listId);

      // Remove mappings
      Object.keys(currentConfig.ChannelMappings).forEach(key => {
        if (key.startsWith(listId + '_')) {
          delete currentConfig.ChannelMappings[key];
        }
      });

      saveConfig().then(() => loadConfig());
    }

    // Save list
    view.querySelector('#btnSaveList').addEventListener('click', () => {
      const name = view.querySelector('#listName').value.trim();
      const content = view.querySelector('#listContent').value;

      if (!name) {
        alert('Bitte geben Sie einen Listennamen ein.');
        return;
      }

      const entries = content.split('\n')
        .map(line => line.trim())
        .filter(line => line.length > 0);

      if (entries.length === 0) {
        alert('Bitte geben Sie mindestens einen Sender ein.');
        return;
      }

      if (currentListId) {
        // Edit existing
        const list = currentConfig.ChannelLists.find(l => l.Id === currentListId);
        if (list) {
          list.Name = name;
          list.Entries = entries;
        }
      } else {
        // Create new
        const newList = {
          Id: generateId(),
          Name: name,
          Entries: entries,
          Created: new Date().toISOString()
        };
        currentConfig.ChannelLists.push(newList);
        currentListId = newList.Id;
      }

      view.querySelector('#listModal').style.display = 'none';

      saveConfig().then(() => {
        loadConfig();
        startMatching(currentListId);
      });
    });

    view.querySelector('#btnCancelModal').addEventListener('click', () => {
      view.querySelector('#listModal').style.display = 'none';
    });

    function generateId() {
      return 'list_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    function saveConfig() {
      Dashboard.showLoadingMsg();
      return ApiClient.updatePluginConfiguration(pluginId, currentConfig).then(() => {
        Dashboard.hideLoadingMsg();
      }).catch(err => {
        Dashboard.hideLoadingMsg();
        alert('Fehler beim Speichern: ' + err);
      });
    }

    // Start matching process
    async function startMatching(listId) {
      const list = currentConfig.ChannelLists.find(l => l.Id === listId);
      if (!list) return;

      Dashboard.showLoadingMsg();

      // Load all streams
      try {
        const response = await Xtream.fetchJson('Xtream/ChannelLists/AllStreams');
        allStreams = Array.isArray(response) ? response : (response.data || response);
        console.log('Loaded streams:', allStreams.length);
      } catch (err) {
        Dashboard.hideLoadingMsg();
        console.error('Load error:', err);
        alert('Fehler beim Laden der Sender: ' + (err.message || JSON.stringify(err)));
        return;
      }

      Dashboard.hideLoadingMsg();

      matchingQueue = list.Entries.map((entry, index) => ({
        listId: list.Id,
        entryName: entry,
        position: index
      }));
      currentMatchIndex = 0;

      view.querySelector('#matchModal').style.display = 'block';
      view.querySelector('#currentMatch').style.display = 'none';
      view.querySelector('#matchComplete').style.display = 'none';

      nextMatch();
    }

    async function nextMatch() {
      if (currentMatchIndex >= matchingQueue.length) {
        // All done
        view.querySelector('#currentMatch').style.display = 'none';
        view.querySelector('#matchComplete').style.display = 'block';
        await saveConfig();
        return;
      }

      const match = matchingQueue[currentMatchIndex];
      const progress = ((currentMatchIndex / matchingQueue.length) * 100).toFixed(0);
      view.querySelector('#matchProgressText').textContent = `${currentMatchIndex + 1} / ${matchingQueue.length}`;
      view.querySelector('#matchProgressBar').style.width = progress + '%';

      view.querySelector('#currentChannelName').textContent = match.entryName;
      view.querySelector('#currentMatch').style.display = 'block';

      // Check if already mapped
      const key = `${match.listId}_${match.entryName}`;
      if (currentConfig.ChannelMappings[key]) {
        // Skip already mapped
        currentMatchIndex++;
        setTimeout(() => nextMatch(), 100);
        return;
      }

      // Fetch matches
      Dashboard.showLoadingMsg();
      try {
        const response = await ApiClient.fetch({
          type: 'POST',
          url: ApiClient.getUrl('Xtream/ChannelLists/Match'),
          contentType: 'application/json',
          data: JSON.stringify({ ChannelName: match.entryName }),
          dataType: 'json'
        });
        Dashboard.hideLoadingMsg();
        const matches = Array.isArray(response) ? response : (response.data || response);
        renderMatches(matches, match);
      } catch (err) {
        Dashboard.hideLoadingMsg();
        console.error('Match error:', err);
        alert('Fehler beim Suchen: ' + (err.message || JSON.stringify(err)));
      }
    }

    function renderMatches(matches, match) {
      const container = view.querySelector('#matchResults');
      container.innerHTML = '';

      // Check if matches is an error object
      if (matches && matches.error) {
        container.innerHTML = `
          <div style="background: #5d1f1f; padding: 20px; border-radius: 6px; margin: 20px 0;">
            <h4 style="color: #f44336; margin-top: 0;">API-Fehler</h4>
            <p style="color: #fff;"><strong>Fehler:</strong> ${escapeHtml(matches.error)}</p>
            <p style="color: #aaa; font-size: 12px;">Typ: ${escapeHtml(matches.type || 'Unknown')}</p>
            ${matches.stackTrace ? `<details style="color: #888; font-size: 11px; margin-top: 10px;"><summary>Stack Trace</summary><pre>${escapeHtml(matches.stackTrace)}</pre></details>` : ''}
            <p style="color: #ffeb3b; margin-top: 15px;">
              ℹ️ Bitte überprüfen Sie Ihre Xtream-Credentials im Tab "Credentials" und stellen Sie sicher, dass mindestens ein Sender im Tab "Live TV" aktiviert ist.
            </p>
          </div>
        `;
        console.error('API Error:', matches);
        return;
      }

      // Ensure matches is an array
      if (!Array.isArray(matches)) {
        console.error('Matches is not an array:', matches);
        container.innerHTML = '<p style="color: #f44336;">Fehler: Ungültige Antwort vom Server</p>';
        return;
      }

      if (matches.length === 0) {
        container.innerHTML = '<p style="color: #888;">Keine Übereinstimmungen gefunden. Bitte wählen Sie manuell aus allen Sendern.</p>';
        return;
      }

      matches.forEach((m, index) => {
        const matchDiv = document.createElement('div');
        matchDiv.style.cssText = 'background: #1a1a1a; padding: 15px; margin: 10px 0; border-radius: 4px; border: 2px solid ' + (index === 0 ? '#00a4dc' : '#333') + '; cursor: pointer;';
        matchDiv.innerHTML = `
          <div style="display: flex; align-items: center; justify-content: space-between;">
            <div style="flex: 1;">
              ${m.StreamIcon ? `<img src="${m.StreamIcon}" style="width: 48px; height: 48px; object-fit: contain; margin-right: 15px; vertical-align: middle;" onerror="this.style.display='none'" />` : ''}
              <strong style="font-size: 16px;">${escapeHtml(m.StreamName)}</strong>
            </div>
            <div style="text-align: right;">
              <div style="color: ${m.IsExact ? '#4caf50' : '#00a4dc'}; font-weight: bold;">
                ${m.IsExact ? '✓ Exakte Übereinstimmung' : `${m.Score}% Übereinstimmung`}
              </div>
              ${index === 0 ? '<div style="color: #00a4dc; font-size: 12px;">Empfohlen</div>' : ''}
            </div>
          </div>
        `;
        matchDiv.addEventListener('click', () => selectMatch(m.StreamId, match));
        container.appendChild(matchDiv);
      });
    }

    function selectMatch(streamId, match) {
      const key = `${match.listId}_${match.entryName}`;
      currentConfig.ChannelMappings[key] = {
        ListId: match.listId,
        EntryName: match.entryName,
        StreamId: streamId,
        IsManual: false,
        Position: match.position
      };

      currentMatchIndex++;
      nextMatch();
    }

    view.querySelector('#btnShowAllStreams').addEventListener('click', () => {
      const match = matchingQueue[currentMatchIndex];
      showAllStreamsModal(match);
    });

    view.querySelector('#btnSkipChannel').addEventListener('click', () => {
      currentMatchIndex++;
      nextMatch();
    });

    function showAllStreamsModal(match) {
      const container = view.querySelector('#matchResults');
      container.innerHTML = '<input type="text" id="streamSearch" placeholder="Sender suchen..." style="width: 100%; padding: 10px; background: #2a2a2a; color: #fff; border: 1px solid #444; border-radius: 4px; margin-bottom: 15px;" />';
      container.innerHTML += '<div id="allStreamsList" style="max-height: 400px; overflow-y: auto;"></div>';

      const searchInput = container.querySelector('#streamSearch');
      const streamsList = container.querySelector('#allStreamsList');

      function renderAllStreams(filter = '') {
        const filtered = filter
          ? allStreams.filter(s => s.StreamName.toLowerCase().includes(filter.toLowerCase()))
          : allStreams;

        streamsList.innerHTML = '';
        filtered.slice(0, 50).forEach(s => {
          const streamDiv = document.createElement('div');
          streamDiv.style.cssText = 'background: #1a1a1a; padding: 10px; margin: 5px 0; border-radius: 4px; cursor: pointer; border: 1px solid #333;';
          streamDiv.innerHTML = `
            ${s.StreamIcon ? `<img src="${s.StreamIcon}" style="width: 32px; height: 32px; object-fit: contain; margin-right: 10px; vertical-align: middle;" onerror="this.style.display='none'" />` : ''}
            <span>${escapeHtml(s.StreamName)}</span>
          `;
          streamDiv.addEventListener('click', () => {
            const key = `${match.listId}_${match.entryName}`;
            currentConfig.ChannelMappings[key] = {
              ListId: match.listId,
              EntryName: match.entryName,
              StreamId: s.StreamId,
              IsManual: true,
              Position: match.position
            };
            currentMatchIndex++;
            nextMatch();
          });
          streamsList.appendChild(streamDiv);
        });

        if (filtered.length > 50) {
          streamsList.innerHTML += `<p style="color: #888; text-align: center; padding: 10px;">Zeige 50 von ${filtered.length} Ergebnissen. Verfeinern Sie Ihre Suche.</p>`;
        }
      }

      searchInput.addEventListener('input', (e) => renderAllStreams(e.target.value));
      renderAllStreams();
    }

    view.querySelector('#btnCloseMatchModal').addEventListener('click', () => {
      view.querySelector('#matchModal').style.display = 'none';
      loadConfig();
    });

    // Reset channel order button
    view.querySelector('#btnResetChannelOrder').addEventListener('click', () => {
      Dashboard.confirm('Möchten Sie wirklich alle Kanalzuordnungen zurücksetzen? Alle Kanäle werden neu vom Provider geladen.', 'Kanalliste zurücksetzen').then(() => {
        Dashboard.showLoadingMsg();
        fetch(ApiClient.getUrl('Xtream/ResetChannelOrder'), {
          method: 'POST',
          headers: {
            'X-Emby-Token': ApiClient.accessToken()
          }
        })
        .then(response => response.json())
        .then(data => {
          Dashboard.hideLoadingMsg();
          if (data.success) {
            Dashboard.alert({
              title: 'Erfolg',
              message: data.message + ' Bitte warten Sie 1-2 Minuten, bis die Kanäle aktualisiert wurden.'
            }).then(() => {
              // Reload config to show cleared mappings
              loadConfig();
            });
          } else {
            Dashboard.alert({
              title: 'Fehler',
              message: 'Fehler beim Zurücksetzen: ' + (data.error || 'Unbekannter Fehler')
            });
          }
        })
        .catch(err => {
          Dashboard.hideLoadingMsg();
          Dashboard.alert({
            title: 'Fehler',
            message: 'Fehler beim Zurücksetzen: ' + err.message
          });
        });
      });
    });

    // Initial load
    loadConfig();
  }));
}
