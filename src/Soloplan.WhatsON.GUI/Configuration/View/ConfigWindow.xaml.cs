// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigWindow.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
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

    /// <summary>
    /// The settings About item tag.
    /// </summary>
    public const string AboutListItemTag = "About";

    /// <summary>
    /// The configuration view model.
    /// </summary>
    private readonly ConfigViewModel configurationViewModel = new ConfigViewModel();

    /// <summary>
    /// The connector which should be focused during initialization.
    /// </summary>
    private readonly Connector initialFocusedConnector;

    /// <summary>
    /// The plugin which should be used for immediate creation of new connector after initialization.
    /// </summary>
    private readonly ConnectorPlugin newConnectorPlugin;

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
    private ConnectorsPage connectorPage;

    /// <summary>
    /// The main page.
    /// </summary>
    private Page mainPage;

    /// <summary>
    /// The about page.
    /// </summary>
    private AboutPage aboutPage;

    /// <summary>
    /// The window shown flag.
    /// </summary>
    private bool windowShown;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, WindowSettings wizardDialogSettings)
    {
      this.configurationSource = configuration;
      this.wizardDialogSettings = wizardDialogSettings;
      this.configurationViewModel.Load(configuration);
      this.configurationViewModel.SingleConnectorMode = false;
      this.DataContext = this.configurationViewModel;
      GlobalConfigDataViewModel.Instance.UseConfiguration(this.configurationViewModel);
      this.InitializeComponent();
      this.ConfigTopicsListBox.SelectedIndex = 0;
      this.configurationViewModel.ConfigurationApplied += (s, e) =>
      {
        this.configurationSource = this.configurationViewModel.Configuration;
        this.ConfigurationApplied?.Invoke(s, e);
      };
      this.configurationViewModel.ConfigurationApplying += (s, e) => this.ConfigurationApplying?.Invoke(s, e);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="initialFocusedConnector">The connector focused on initialization.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, Connector initialFocusedConnector, WindowSettings wizardDialogSettings)
      : this(configuration, wizardDialogSettings)
    {
      this.initialFocusedConnector = initialFocusedConnector;
      this.configurationViewModel.SingleConnectorMode = true;

      this.FocusConnectorsPage();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="newConnectorPlugin">The new connector plugin.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public ConfigWindow(ApplicationConfiguration configuration, ConnectorPlugin newConnectorPlugin, WindowSettings wizardDialogSettings)
      : this(configuration, wizardDialogSettings)
    {
      this.newConnectorPlugin = newConnectorPlugin;
      this.configurationViewModel.SingleConnectorMode = true;
      this.configurationViewModel.SingleNewConnectorMode = true;

      this.FocusConnectorsPage();
    }

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    /// <summary>
    /// Occurs when configuration is about to be applied.
    /// </summary>
    public event EventHandler<EventArgs> ConfigurationApplying;

    public ConfigViewModel ConfigurationViewModel
    {
      get { return this.configurationViewModel; }
    }

    /// <summary>
    /// Raises the <see cref="E:ContentRendered" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      this.Owner.Closing += this.OwnerClosing;
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
      var connectorsConfigurationListBoxItem = this.ConfigTopicsListBox.Items.Cast<ListBoxItem>().FirstOrDefault(i => i.Tag.ToString() == ConnectorsListItemTag);
      this.configurationViewModel.InitialFocusedConfigurationListBoxItem = connectorsConfigurationListBoxItem;
    }

    /// <summary>
    /// Handles the SelectionChanged event of the ListBox control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
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

      var selectedItemTag = (string)((ListBoxItem)e.AddedItems[0]).Tag;
      switch (selectedItemTag)
      {
        case MainListItemTag:
          this.mainPage = this.mainPage ?? new MainConfigPage(this.configurationViewModel);
          this.ConfigFrame.Content = this.mainPage;
          return;
        case ConnectorsListItemTag:
          if (this.connectorPage == null)
          {
            if (!this.configurationViewModel.SingleConnectorMode)
            {
              this.connectorPage = new ConnectorsPage(this.configurationViewModel.Connectors, this, this.configurationSource, this.wizardDialogSettings);
            }
            else if (this.initialFocusedConnector != null)
            {
              this.connectorPage = new ConnectorsPage(this.configurationViewModel.Connectors, this, this.initialFocusedConnector, this.configurationSource, this.wizardDialogSettings);
            }
            else
            {
              this.connectorPage = new ConnectorsPage(this.configurationViewModel.Connectors, this, this.newConnectorPlugin, this.configurationSource, this.wizardDialogSettings);
            }
          }

          this.configurationViewModel.PropertyChanged -= this.ConfigurationViewModelPropertyChanged;
          this.configurationViewModel.PropertyChanged += this.ConfigurationViewModelPropertyChanged;
          this.connectorPage.SingleConnectorMode = this.configurationViewModel.SingleConnectorMode;
          this.ConfigFrame.Content = this.connectorPage;
          return;
        case AboutListItemTag:
          this.aboutPage = this.aboutPage ?? new AboutPage(this.configurationViewModel);
          this.ConfigFrame.Content = this.aboutPage;
          return;
      }
    }

    /// <summary>
    /// Handles the PropertyChanged event of the ConfigurationViewModel control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void ConfigurationViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(this.configurationViewModel.SingleConnectorMode))
      {
        this.connectorPage.SingleConnectorMode = this.configurationViewModel.SingleConnectorMode;
      }

      if (e.PropertyName == nameof(this.ConfigurationViewModel.ConfigurationIsModified))
      {
        this.connectorPage.OnConfigurationIsModified();
      }
    }

    /// <summary>
    /// Handles the Closing event of the Window control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      this.Owner.Closing -= this.OwnerClosing;
    }

    /// <summary>
    /// Handles the Reset Click on the Snackbar.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void SnackbarResetClick(object sender, RoutedEventArgs e)
    {
      this.configurationViewModel.Load(this.configurationSource);
      if (this.configurationViewModel.SingleConnectorMode && this.newConnectorPlugin != null)
      {
        this.Close();
      }
    }

    /// <summary>
    /// Prevents parent from closing when configuration is modified.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void OwnerClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      e.Cancel = this.configurationViewModel.ConfigurationIsModified;
    }

    /// <summary>
    /// Handles the Save Click on the Snackbar.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void SnackbarSaveClick(object sender, RoutedEventArgs e)
    {
      if (Validation.IsValid(this))
      {
        this.configurationViewModel.ApplyToSourceAndSave();
      }
    }
  }
}
