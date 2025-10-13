export default function (view) {
  view.addEventListener("viewshow", () => import(
    window.ApiClient.getUrl("web/ConfigurationPage", {
      name: "Xtream.js",
    })
  ).then((Xtream) => Xtream.default
  ).then((Xtream) => {
    const pluginId = Xtream.pluginConfig.UniqueId;
    Xtream.setTabs(0);

    Dashboard.showLoadingMsg();
    ApiClient.getPluginConfiguration(pluginId).then(function (config) {
      view.querySelector('#BaseUrl').value = config.BaseUrl;
      view.querySelector('#Username').value = config.Username;
      view.querySelector('#Password').value = config.Password;
      Dashboard.hideLoadingMsg();

      // Load provider info if credentials are configured
      if (config.BaseUrl && config.Username && config.Password) {
        loadProviderInfo();
      }
    });

    function loadProviderInfo() {
      console.log('Loading provider info...');
      fetch(ApiClient.getUrl('Xtream/UserInfo'), {
        method: 'GET',
        headers: {
          'X-Emby-Token': ApiClient.accessToken()
        }
      })
      .then(response => {
        console.log('Provider info response status:', response.status);
        return response.json();
      })
      .then(data => {
        console.log('Provider info data:', data);
        if (data.error) {
          console.error('Provider info error:', data.error);
          // Hide provider info section on error
          view.querySelector('#providerInfoSection').style.display = 'none';
          return;
        }

        // Show provider info section
        view.querySelector('#providerInfoSection').style.display = 'block';

        // Fill in the data
        view.querySelector('#infoUsername').textContent = data.username || '-';
        view.querySelector('#infoStatus').textContent = data.status || '-';

        // Format expiration date
        if (data.expDate) {
          const expDate = new Date(data.expDate);
          view.querySelector('#infoExpDate').textContent = expDate.toLocaleDateString('de-DE');

          // Calculate remaining days
          const now = new Date();
          const diffTime = expDate - now;
          const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

          const remainingEl = view.querySelector('#infoRemainingDays');
          if (diffDays < 0) {
            remainingEl.textContent = 'Abgelaufen';
            remainingEl.style.color = '#f44336';
          } else if (diffDays < 7) {
            remainingEl.textContent = diffDays + ' Tage';
            remainingEl.style.color = '#f44336';
          } else if (diffDays < 30) {
            remainingEl.textContent = diffDays + ' Tage';
            remainingEl.style.color = '#ff9800';
          } else {
            remainingEl.textContent = diffDays + ' Tage';
            remainingEl.style.color = '#4caf50';
          }
        }

        // Format created date
        if (data.createdAt) {
          const createdDate = new Date(data.createdAt);
          view.querySelector('#infoCreatedAt').textContent = createdDate.toLocaleDateString('de-DE');
        } else {
          view.querySelector('#infoCreatedAt').textContent = '-';
        }

        view.querySelector('#infoActiveCons').textContent = data.activeCons !== undefined ? data.activeCons : '0';
        view.querySelector('#infoMaxCons').textContent = data.maxConnections !== undefined ? data.maxConnections : '-';
        view.querySelector('#infoIsTrial').textContent = data.isTrial ? 'Ja' : 'Nein';

        // Set status color
        const statusEl = view.querySelector('#infoStatus');
        if (data.status === 'Active') {
          statusEl.style.color = '#4caf50';
        } else {
          statusEl.style.color = '#f44336';
        }
      })
      .catch(err => {
        console.error('Failed to load provider info:', err);
        view.querySelector('#providerInfoSection').style.display = 'none';
      });
    }

    view.querySelector('#XtreamCredentialsForm').addEventListener('submit', (e) => {
      Dashboard.showLoadingMsg();

      ApiClient.getPluginConfiguration(pluginId).then((config) => {
        config.BaseUrl = view.querySelector('#BaseUrl').value;
        config.Username = view.querySelector('#Username').value;
        config.Password = view.querySelector('#Password').value;
        ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
          Dashboard.processPluginConfigurationUpdateResult(result);
          // Reload provider info after save
          setTimeout(() => loadProviderInfo(), 1000);
        });
      });

      e.preventDefault();
      return false;
    });
  }));
}