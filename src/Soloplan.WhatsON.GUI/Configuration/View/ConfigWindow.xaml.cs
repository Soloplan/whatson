// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigWindow.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Interaction logic for ConfigWindow.xaml.
  /// </summary>
  public partial class ConfigWindow : Window
  {
    /// <summary>
    /// The settings Main item tag.
    /// </summary>
    public const string MainListItemTag = "Main";

    /// <summary>
    /// The settings Connectors item tag.
    /// </summary>
    public const string ConnectorsListItemTag = "Connectors";

    public const string ServersListItemTag = "Servers";

    /// <summary>
    /// The settings About item tag.
    /// </summary>
    public const string AboutListItemTag = "About";

    /// <summary>
    /// The connector which should be focused during initialization.
    /// </summary>
    private readonly Connector? initialFocusedConnector;

    /// <summary>
    /// The plugin which should be used for immediate creation of new connector after initialization.
    /// </summary>
    private readonly ConnectorPlugin? newConnectorPlugin;

    /// <summary>
    /// The wizard dialog settings.
    /// </summary>
    private readonly WindowSettings wizardDialogSettings;

    /// <summary>
    /// The configuration source.
    /// </summary>
    private ApplicationConfiguration configurationSource;

    /// <summary>
    /// The connector page.
    /// </summary>
    private ConnectorsPage? connectorPage;

    /// <summary>
    /// The main page.
    /// </summary>
    private Page? mainPage;

    /// <summary>
    /// The about page.
    /// </summary>
    private AboutPage? aboutPage;

    /// <summary>
    /// The window shown flag.
    /// </summary>
    private bool windowShown;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, WindowSettings? wizardDialogSettings)
    {
      this.configurationSource = configuration;
      this.wizardDialogSettings = wizardDialogSettings ?? new WindowSettings();
      this.ConfigurationViewModel.Load(configuration);
      this.ConfigurationViewModel.SingleConnectorMode = false;
      this.DataContext = this.ConfigurationViewModel;
      GlobalConfigDataViewModel.Instance.UseConfiguration(this.ConfigurationViewModel);
      this.InitializeComponent();
      this.ConfigTopicsListBox.SelectedIndex = 0;
      this.ConfigurationViewModel.ConfigurationApplied += (s, e) =>
      {
        this.configurationSource = this.ConfigurationViewModel.Configuration;
        this.ConfigurationApplied?.Invoke(s, e);
      };
      this.ConfigurationViewModel.ConfigurationApplying += (s, e) => this.ConfigurationApplying?.Invoke(s, e);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="initialFocusedConnector">The connector focused on initialization.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, Connector initialFocusedConnector, WindowSettings? wizardDialogSettings)
      : this(configuration, wizardDialogSettings)
    {
      this.initialFocusedConnector = initialFocusedConnector;
      this.ConfigurationViewModel.SingleConnectorMode = true;

      this.FocusConnectorsPage();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="newConnectorPlugin">The new connector plugin.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, ConnectorPlugin newConnectorPlugin, WindowSettings? wizardDialogSettings)
      : this(configuration, wizardDialogSettings)
    {
      this.newConnectorPlugin = newConnectorPlugin;
      this.ConfigurationViewModel.SingleConnectorMode = true;
      this.ConfigurationViewModel.SingleNewConnectorMode = true;

      this.FocusConnectorsPage();
    }

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>>? ConfigurationApplied;

    /// <summary>
    /// Occurs when configuration is about to be applied.
    /// </summary>
    public event EventHandler<EventArgs>? ConfigurationApplying;

    /// <summary>
    /// The configuration view model.
    /// </summary>
    public ConfigViewModel ConfigurationViewModel { get; } = new ConfigViewModel();

    /// <summary>
    /// Raises the <see cref="E:ContentRendered" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      if (this.Owner != null)
      {
        this.Owner.Closing += this.OwnerClosing;
      }

      if (this.windowShown)
      {
        return;
      }

      this.windowShown = true;
    }

    /// <summary>
    /// Focuses the connectors page.
    /// </summary>
    private void FocusConnectorsPage()
    {
      var connectorsConfigurationListBoxItem =
        this.ConfigTopicsListBox.Items.Cast<ListBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == ConnectorsListItemTag);
      this.ConfigurationViewModel.InitialFocusedConfigurationListBoxItem = connectorsConfigurationListBoxItem;
    }

    /// <summary>
    /// Handles the SelectionChanged event of the ListBox control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
    private void ListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count != 1)
      {
        return;
      }

      if (!Validation.IsValid(this))
      {
        e.Handled = true;
        try
        {
          this.ConfigTopicsListBox.SelectionChanged -= this.ListBoxSelectionChanged;
          this.ConfigTopicsListBox.SelectedItem = e.RemovedItems[0];
          return;
        }
        finally
        {
          this.ConfigTopicsListBox.SelectionChanged += this.ListBoxSelectionChanged;
        }
      }

      var selectedItemTag = (e.AddedItems[0] as ListBoxItem)?.Tag as string;
      switch (selectedItemTag)
      {
        case MainListItemTag:
          this.mainPage = this.mainPage ?? new MainConfigPage(this.ConfigurationViewModel);
          this.ConfigFrame.Content = this.mainPage;
          return;
        case ConnectorsListItemTag:
          if (this.connectorPage == null)
          {
            if (!this.ConfigurationViewModel.SingleConnectorMode)
            {
              this.connectorPage = new ConnectorsPage(
                this.ConfigurationViewModel.Connectors,
                this,
                this.configurationSource,
                this.wizardDialogSettings);
            }
            else if (this.initialFocusedConnector != null)
            {
              this.connectorPage = new ConnectorsPage(
                this.ConfigurationViewModel.Connectors,
                this,
                this.initialFocusedConnector,
                this.configurationSource,
                this.wizardDialogSettings);
            }
            else
            {
              this.connectorPage = new ConnectorsPage(
                this.ConfigurationViewModel.Connectors,
                this,
                this.newConnectorPlugin ?? throw new InvalidOperationException("No connector plugin was selected."),
                this.configurationSource,
                this.wizardDialogSettings);
            }
          }

          this.ConfigurationViewModel.PropertyChanged -= this.ConfigurationViewModelPropertyChanged;
          this.ConfigurationViewModel.PropertyChanged += this.ConfigurationViewModelPropertyChanged;
          this.connectorPage.SingleConnectorMode = this.ConfigurationViewModel.SingleConnectorMode;
          this.ConfigFrame.Content = this.connectorPage;
          return;
        case AboutListItemTag:
          this.aboutPage = this.aboutPage ?? new AboutPage(this.ConfigurationViewModel);
          this.ConfigFrame.Content = this.aboutPage;
          return;
        case ServersListItemTag:
          this.ConfigFrame.Content = new ServerConfigPage(this.ConfigurationViewModel);
          return;
      }
    }

    /// <summary>
    /// Handles the PropertyChanged event of the ConfigurationViewModel control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
    private void ConfigurationViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(this.ConfigurationViewModel.SingleConnectorMode))
      {
        if (this.connectorPage != null)
        {
          this.connectorPage.SingleConnectorMode = this.ConfigurationViewModel.SingleConnectorMode;
        }
      }

      if (e.PropertyName == nameof(this.ConfigurationViewModel.ConfigurationIsModified))
      {
        this.connectorPage?.OnConfigurationIsModified();
      }
    }

    /// <summary>
    /// Handles the Closing event of the Window control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the event data.</param>
    private void WindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!this.IsEnabled)
      {
        e.Cancel = true;
        return;
      }

      if (this.Owner != null)
      {
        this.Owner.Closing -= this.OwnerClosing;
      }

      ((App)Application.Current).ApplyTheme(this.configurationSource.ThemeMode);
      Localizer.ApplyLanguage(this.configurationSource.Language);
    }

    /// <summary>
    /// Handles the Reset Click on the Snackbar.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private void SnackbarResetClick(object? sender, RoutedEventArgs e)
    {
      this.ConfigurationViewModel.Load(this.configurationSource);
      ((App)Application.Current).ApplyTheme(this.configurationSource.ThemeMode);
      Localizer.ApplyLanguage(this.configurationSource.Language);
      if (this.ConfigurationViewModel.SingleConnectorMode && this.newConnectorPlugin != null)
      {
        this.Close();
      }
    }

    /// <summary>
    /// Prevents parent from closing when configuration is modified.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void OwnerClosing(object? sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = !this.IsEnabled || this.ConfigurationViewModel.ConfigurationIsModified;

    /// <summary>
    /// Handles the Save Click on the Snackbar.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private async void SnackbarSaveClick(object? sender, RoutedEventArgs e)
    {
      this.IsEnabled = false;
      try
      {
        if (Validation.IsValid(this))
        {
          await this.ConfigurationViewModel.ApplyToSourceAndSaveAsync();
        }
      }
      finally
      {
        this.IsEnabled = true;
      }
    }
  }
}
