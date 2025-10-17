export default function (view) {
  view.addEventListener("viewshow", () => import(
    ApiClient.getUrl("web/ConfigurationPage", {
      name: "Xtream.js",
    })
  ).then((Xtream) => Xtream.default
  ).then((Xtream) => {
    const pluginId = Xtream.pluginConfig.UniqueId;
    Xtream.setTabs(6); // New tab index

    let currentConfig = null;
    let allChannels = [];
    let currentEditingCategory = null;

    // Get DOM elements
    const form = view.querySelector("#XtreamCategoriesForm");
    const useCustomCategories = view.querySelector("#UseCustomChannelCategories");
    const enableVodGenreCategories = view.querySelector("#EnableVodGenreCategories");
    const enableTmdbForSeries = view.querySelector("#EnableTmdbForSeries");
    const enableAutomaticTmdbUpdate = view.querySelector("#EnableAutomaticTmdbUpdate");
    const categoriesContent = view.querySelector("#CategoriesContent");
    const addCategoryBtn = view.querySelector("#AddCategoryBtn");
    const customCategoriesSection = view.querySelector("#CustomCategoriesSection");

    // Dialog elements
    const dialog = view.querySelector("#CategoryEditDialog");
    const dialogBackdrop = view.querySelector("#DialogBackdrop");
    const dialogTitle = view.querySelector("#DialogTitle");
    const categoryForm = view.querySelector("#CategoryEditForm");
    const categoryName = view.querySelector("#CategoryName");
    const categoryDescription = view.querySelector("#CategoryDescription");
    const categoryIconUrl = view.querySelector("#CategoryIconUrl");
    const categorySortOrder = view.querySelector("#CategorySortOrder");
    const categoryIsEnabled = view.querySelector("#CategoryIsEnabled");
    const channelSelectionContainer = view.querySelector("#ChannelSelectionContainer");
    const cancelCategoryBtn = view.querySelector("#CancelCategoryBtn");

    // Load configuration
    const loadConfig = () => {
      Dashboard.showLoadingMsg();
      return ApiClient.getPluginConfiguration(pluginId).then((config) => {
        currentConfig = config;

        // Set checkbox states
        useCustomCategories.checked = config.UseCustomChannelCategories || false;
        enableVodGenreCategories.checked = config.EnableVodGenreCategories !== false; // default true
        enableTmdbForSeries.checked = config.EnableTmdbForSeries !== false; // default true
        enableAutomaticTmdbUpdate.checked = config.EnableAutomaticTmdbUpdate !== false; // default true

        // Load channels for selection
        return Xtream.fetchJson('Xtream/LiveTv').then((channels) => {
          allChannels = channels;
          renderCategories();
          Dashboard.hideLoadingMsg();
        });
      });
    };

    // Render categories table
    const renderCategories = () => {
      categoriesContent.innerHTML = '';
      const categories = currentConfig.CustomChannelCategories || [];

      if (categories.length === 0) {
        const tr = document.createElement('tr');
        tr.innerHTML = '<td colspan="6" style="text-align:center; padding:2em; color:#999;">Keine benutzerdefinierten Kategorien vorhanden. Klicken Sie auf "Neue Kategorie", um eine zu erstellen.</td>';
        categoriesContent.appendChild(tr);
        return;
      }

      categories.sort((a, b) => (a.SortOrder || 0) - (b.SortOrder || 0));

      categories.forEach((category) => {
        const tr = document.createElement('tr');

        // Name
        const tdName = document.createElement('td');
        tdName.textContent = category.Name || '';
        tr.appendChild(tdName);

        // Description
        const tdDesc = document.createElement('td');
        tdDesc.textContent = category.Description || '';
        tdDesc.style.maxWidth = '200px';
        tdDesc.style.overflow = 'hidden';
        tdDesc.style.textOverflow = 'ellipsis';
        tr.appendChild(tdDesc);

        // Channel count
        const tdChannels = document.createElement('td');
        const channelCount = (category.ChannelIds && category.ChannelIds.length) || 0;
        tdChannels.textContent = channelCount + ' Sender';
        tr.appendChild(tdChannels);

        // Sort order
        const tdSort = document.createElement('td');
        tdSort.textContent = category.SortOrder || 0;
        tr.appendChild(tdSort);

        // Enabled
        const tdEnabled = document.createElement('td');
        const enabledIcon = document.createElement('span');
        enabledIcon.classList.add('material-icons');
        enabledIcon.textContent = category.IsEnabled !== false ? 'check_circle' : 'cancel';
        enabledIcon.style.color = category.IsEnabled !== false ? '#52b54b' : '#cc3333';
        tdEnabled.appendChild(enabledIcon);
        tr.appendChild(tdEnabled);

        // Actions
        const tdActions = document.createElement('td');

        const editBtn = document.createElement('button');
        editBtn.type = 'button';
        editBtn.classList.add('paper-icon-button-light');
        editBtn.innerHTML = '<span class="material-icons">edit</span>';
        editBtn.title = 'Bearbeiten';
        editBtn.onclick = () => editCategory(category);
        tdActions.appendChild(editBtn);

        const deleteBtn = document.createElement('button');
        deleteBtn.type = 'button';
        deleteBtn.classList.add('paper-icon-button-light');
        deleteBtn.innerHTML = '<span class="material-icons">delete</span>';
        deleteBtn.title = 'Löschen';
        deleteBtn.onclick = () => deleteCategory(category.Id);
        tdActions.appendChild(deleteBtn);

        tr.appendChild(tdActions);
        categoriesContent.appendChild(tr);
      });
    };

    // Open dialog for new category
    const openNewCategoryDialog = () => {
      currentEditingCategory = null;
      dialogTitle.textContent = 'Neue Kategorie erstellen';
      categoryName.value = '';
      categoryDescription.value = '';
      categoryIconUrl.value = '';
      categorySortOrder.value = '0';
      categoryIsEnabled.checked = true;

      renderChannelSelection([]);
      showDialog();
    };

    // Open dialog for editing category
    const editCategory = (category) => {
      currentEditingCategory = category;
      dialogTitle.textContent = 'Kategorie bearbeiten';
      categoryName.value = category.Name || '';
      categoryDescription.value = category.Description || '';
      categoryIconUrl.value = category.IconUrl || '';
      categorySortOrder.value = category.SortOrder || 0;
      categoryIsEnabled.checked = category.IsEnabled !== false;

      renderChannelSelection(category.ChannelIds || []);
      showDialog();
    };

    // Render channel selection checkboxes
    const renderChannelSelection = (selectedChannelIds) => {
      channelSelectionContainer.innerHTML = '';

      allChannels.forEach((channel) => {
        const label = document.createElement('label');
        label.style.display = 'block';
        label.style.marginBottom = '0.5em';

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.value = channel.Id;
        checkbox.checked = selectedChannelIds.includes(channel.Id);
        checkbox.dataset.channelId = channel.Id;

        label.appendChild(checkbox);
        label.appendChild(document.createTextNode(' ' + channel.Name));

        channelSelectionContainer.appendChild(label);
      });
    };

    // Show dialog
    const showDialog = () => {
      dialog.style.display = 'block';
      dialogBackdrop.style.display = 'block';
    };

    // Hide dialog
    const hideDialog = () => {
      dialog.style.display = 'none';
      dialogBackdrop.style.display = 'none';
    };

    // Delete category
    const deleteCategory = (categoryId) => {
      if (!confirm('Möchten Sie diese Kategorie wirklich löschen?')) {
        return;
      }

      Dashboard.showLoadingMsg();

      ApiClient.fetch({
        type: 'DELETE',
        url: ApiClient.getUrl(`Xtream/CustomChannelCategories/${categoryId}`),
      }).then(() => {
        currentConfig.CustomChannelCategories = currentConfig.CustomChannelCategories.filter(c => c.Id !== categoryId);
        renderCategories();
        Dashboard.hideLoadingMsg();
        Dashboard.processServerConfigurationUpdateResult({ UpdateType: 'None' });
      }).catch((error) => {
        Dashboard.hideLoadingMsg();
        Dashboard.alert('Fehler beim Löschen der Kategorie: ' + error);
      });
    };

    // Save category (create or update)
    categoryForm.addEventListener('submit', (e) => {
      e.preventDefault();

      // Get selected channel IDs
      const selectedChannelIds = Array.from(channelSelectionContainer.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => parseInt(cb.value));

      const categoryData = {
        Id: currentEditingCategory ? currentEditingCategory.Id : null,
        Name: categoryName.value.trim(),
        Description: categoryDescription.value.trim(),
        IconUrl: categoryIconUrl.value.trim(),
        SortOrder: parseInt(categorySortOrder.value) || 0,
        IsEnabled: categoryIsEnabled.checked,
        ChannelIds: selectedChannelIds
      };

      if (!categoryData.Name) {
        Dashboard.alert('Bitte geben Sie einen Namen ein.');
        return;
      }

      Dashboard.showLoadingMsg();

      const method = currentEditingCategory ? 'PUT' : 'POST';
      const url = currentEditingCategory
        ? `Xtream/CustomChannelCategories/${currentEditingCategory.Id}`
        : 'Xtream/CustomChannelCategories';

      ApiClient.fetch({
        type: method,
        url: ApiClient.getUrl(url),
        data: JSON.stringify(categoryData),
        contentType: 'application/json',
        dataType: 'json'
      }).then((result) => {
        // Update local config
        if (currentEditingCategory) {
          const index = currentConfig.CustomChannelCategories.findIndex(c => c.Id === result.Id);
          if (index >= 0) {
            currentConfig.CustomChannelCategories[index] = result;
          }
        } else {
          if (!currentConfig.CustomChannelCategories) {
            currentConfig.CustomChannelCategories = [];
          }
          currentConfig.CustomChannelCategories.push(result);
        }

        hideDialog();
        renderCategories();
        Dashboard.hideLoadingMsg();
        Dashboard.processServerConfigurationUpdateResult({ UpdateType: 'None' });
      }).catch((error) => {
        Dashboard.hideLoadingMsg();
        Dashboard.alert('Fehler beim Speichern der Kategorie: ' + error);
      });
    });

    // Event listeners
    addCategoryBtn.addEventListener('click', openNewCategoryDialog);
    cancelCategoryBtn.addEventListener('click', hideDialog);
    dialogBackdrop.addEventListener('click', hideDialog);

    // Toggle custom categories section visibility
    useCustomCategories.addEventListener('change', () => {
      customCategoriesSection.style.display = useCustomCategories.checked ? 'flex' : 'none';
      view.querySelector('#CustomCategoriesTable').parentElement.style.display = useCustomCategories.checked ? 'block' : 'none';
    });

    // Main form submit
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      Dashboard.showLoadingMsg();

      ApiClient.getPluginConfiguration(pluginId).then((config) => {
        config.UseCustomChannelCategories = useCustomCategories.checked;
        config.EnableVodGenreCategories = enableVodGenreCategories.checked;
        config.EnableTmdbForSeries = enableTmdbForSeries.checked;
        config.EnableAutomaticTmdbUpdate = enableAutomaticTmdbUpdate.checked;

        ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
          Dashboard.processPluginConfigurationUpdateResult(result);
        });
      });
    });

    // Initial load
    loadConfig().then(() => {
      // Set initial visibility
      customCategoriesSection.style.display = useCustomCategories.checked ? 'flex' : 'none';
      view.querySelector('#CustomCategoriesTable').parentElement.style.display = useCustomCategories.checked ? 'block' : 'none';
    });
  }));
}
