export default function (view) {
  view.addEventListener("viewshow", () => import(
    ApiClient.getUrl("web/ConfigurationPage", {
      name: "Xtream.js",
    })
  ).then((Xtream) => Xtream.default
  ).then((Xtream) => {
    const pluginId = Xtream.pluginConfig.UniqueId;
    Xtream.setTabs(5);

    Dashboard.showLoadingMsg();
    ApiClient.getPluginConfiguration(pluginId).then(function (config) {
      view.querySelector('#EnableConnectionQueue').checked = config.EnableConnectionQueue ?? true;
      view.querySelector('#EnableExtendedCache').checked = config.EnableExtendedCache ?? true;
      view.querySelector('#MaintenanceStartHour').value = config.MaintenanceStartHour ?? 3;
      view.querySelector('#MaintenanceEndHour').value = config.MaintenanceEndHour ?? 6;
      view.querySelector('#EnableEpgPreload').checked = config.EnableEpgPreload ?? true;
      view.querySelector('#EnableMetadataUpdate').checked = config.EnableMetadataUpdate ?? true;
      view.querySelector('#EnableThumbnailCache').checked = config.EnableThumbnailCache ?? true;
      view.querySelector('#ThumbnailCacheRetentionDays').value = config.ThumbnailCacheRetentionDays ?? 30;
      Dashboard.hideLoadingMsg();

      // Load thumbnail cache stats
      loadThumbnailCacheStats();
    });

    function loadThumbnailCacheStats() {
      fetch(ApiClient.getUrl('Xtream/ThumbnailCacheStats'), {
        method: 'GET',
        headers: {
          'X-Emby-Token': ApiClient.accessToken()
        }
      })
      .then(response => response.json())
      .then(data => {
        view.querySelector('#cacheFileCount').textContent = data.fileCount || 0;
        view.querySelector('#cacheTotalSize').textContent = (data.totalSizeMB || 0).toFixed(2) + ' MB';
      })
      .catch(err => console.error('Failed to load cache stats:', err));
    }

    // Update statistics every 2 seconds
    let statsInterval;
    function updateStats() {
      fetch(ApiClient.getUrl('Xtream/OptimizationStats'), {
        method: 'GET',
        headers: {
          'X-Emby-Token': ApiClient.accessToken()
        }
      })
      .then(response => response.json())
      .then(stats => {
        const statusIndicator = view.querySelector('#statusIndicator');
        const queueSize = view.querySelector('#queueSize');
        const totalRequests = view.querySelector('#totalRequests');
        const cacheHitRate = view.querySelector('#cacheHitRate');
        const thumbnailCacheHitRate = view.querySelector('#thumbnailCacheHitRate');
        const thumbnailCachedImages = view.querySelector('#thumbnailCachedImages');
        const thumbnailCacheRequests = view.querySelector('#thumbnailCacheRequests');

        if (statusIndicator) {
          statusIndicator.innerHTML = stats.isBusy
            ? '<span style="color: #ff9800;">Beschaeftigt</span>'
            : '<span style="color: #4caf50;">Bereit</span>';
        }

        if (queueSize) queueSize.textContent = stats.queuedRequests || 0;
        if (totalRequests) totalRequests.textContent = stats.totalRequests || 0;
        if (cacheHitRate) {
          const rate = stats.cacheHitRate || 0;
          cacheHitRate.textContent = rate.toFixed(1) + '%';
        }

        if (thumbnailCacheHitRate) {
          const thumbRate = stats.thumbnailCacheHitRate || 0;
          thumbnailCacheHitRate.textContent = thumbRate.toFixed(1) + '%';
        }
        if (thumbnailCachedImages) thumbnailCachedImages.textContent = stats.thumbnailCachedImages || 0;
        if (thumbnailCacheRequests) thumbnailCacheRequests.textContent = stats.thumbnailCacheRequests || 0;
      })
      .catch(err => console.error('Failed to fetch stats:', err));
    }

    // Initial stats load (no auto-refresh to avoid counting self)
    updateStats();

    // Clean up on hide
    view.addEventListener('viewhide', () => {
      // Cleanup if needed
    });

    // Clear cache button
    view.querySelector('#clearCacheBtn').addEventListener('click', () => {
      Dashboard.confirm('Moechten Sie wirklich alle gecachten Thumbnails loeschen?', 'Cache leeren').then(() => {
        Dashboard.showLoadingMsg();
        fetch(ApiClient.getUrl('Xtream/ClearThumbnailCache'), {
          method: 'POST',
          headers: {
            'X-Emby-Token': ApiClient.accessToken()
          }
        })
        .then(response => response.json())
        .then(data => {
          Dashboard.hideLoadingMsg();
          Dashboard.alert({
            title: 'Erfolg',
            message: `${data.deletedFiles || 0} Dateien geloescht (${(data.freedSpaceMB || 0).toFixed(2)} MB).`
          });
          loadThumbnailCacheStats();
        })
        .catch(err => {
          Dashboard.hideLoadingMsg();
          Dashboard.alert({
            title: 'Fehler',
            message: 'Cache konnte nicht geleert werden: ' + err.message
          });
        });
      });
    });

    // Test connection button
    view.querySelector('#testConnectionBtn').addEventListener('click', () => {
      Dashboard.showLoadingMsg();

      fetch(ApiClient.getUrl('Xtream/LiveCategories'), {
        method: 'GET',
        headers: {
          'X-Emby-Token': ApiClient.accessToken()
        }
      })
      .then(response => response.json())
      .then(data => {
        Dashboard.hideLoadingMsg();
        Dashboard.alert({
          title: 'Test erfolgreich',
          message: `${data.length} Live-TV Kategorien geladen. Pruefe die Statistiken!`
        });
      })
      .catch(err => {
        Dashboard.hideLoadingMsg();
        Dashboard.alert({
          title: 'Fehler',
          message: 'Testverbindung fehlgeschlagen: ' + err.message
        });
      });
    });

    // Reset channel order button
    view.querySelector('#resetChannelOrderBtn').addEventListener('click', () => {
      Dashboard.confirm('Moechten Sie wirklich die benutzerdefinierte Kanalreihenfolge zuruecksetzen? Alle Kanaele werden neu vom Provider geladen.', 'Kanalliste zuruecksetzen').then(() => {
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
              message: data.message + ' Bitte warten Sie 1-2 Minuten, bis die Kanaele aktualisiert wurden.'
            });
          } else {
            Dashboard.alert({
              title: 'Fehler',
              message: 'Fehler beim Zuruecksetzen: ' + (data.error || 'Unbekannter Fehler')
            });
          }
        })
        .catch(err => {
          Dashboard.hideLoadingMsg();
          Dashboard.alert({
            title: 'Fehler',
            message: 'Fehler beim Zuruecksetzen: ' + err.message
          });
        });
      });
    });

    view.querySelector('#XtreamOptimizationsForm').addEventListener('submit', (e) => {
      Dashboard.showLoadingMsg();

      ApiClient.getPluginConfiguration(pluginId).then((config) => {
        config.EnableConnectionQueue = view.querySelector('#EnableConnectionQueue').checked;
        config.EnableExtendedCache = view.querySelector('#EnableExtendedCache').checked;
        config.MaintenanceStartHour = parseInt(view.querySelector('#MaintenanceStartHour').value);
        config.MaintenanceEndHour = parseInt(view.querySelector('#MaintenanceEndHour').value);
        config.EnableEpgPreload = view.querySelector('#EnableEpgPreload').checked;
        config.EnableMetadataUpdate = view.querySelector('#EnableMetadataUpdate').checked;
        config.EnableThumbnailCache = view.querySelector('#EnableThumbnailCache').checked;
        config.ThumbnailCacheRetentionDays = parseInt(view.querySelector('#ThumbnailCacheRetentionDays').value);

        ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
          Dashboard.processPluginConfigurationUpdateResult(result);
        });
      });

      e.preventDefault();
      return false;
    });
  }));
}