// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.GUI.Config.Wizard;

  /// <summary>
  /// Interaction logic for ConnectorsPage.xaml
  /// </summary>
  public partial class ConnectorsPage : Page, INotifyPropertyChanged
  {
    /// <summary>
    /// The owner window.
    /// </summary>
    private readonly Window ownerWindow;

    /// <summary>
    /// The current connector.
    /// </summary>
    private ConnectorViewModel currentConnector;

    /// <summary>
    /// The active connector supports wizard.
    /// </summary>
    private bool activeConnectorSupportsWizard;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorsPage"/> class.
    /// </summary>
    /// <param name="connectors">The connectors.</param>
    /// <param name="ownerWindow">The owner <see cref="Window"/>.</param>
    public ConnectorsPage(ConnectorViewModelCollection connectors, Window ownerWindow)
    {
      this.ownerWindow = ownerWindow;
      this.Connectors = connectors;
      this.DataContext = this;
      this.InitializeComponent();
      this.Connectors.Loaded -= this.ConnectorsLoaded;
      this.Connectors.Loaded += this.ConnectorsLoaded;
    }

    /// <summary>
    /// Occurs when a property was changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the connectors.
    /// </summary>
    public ConnectorViewModelCollection Connectors { get; private set; }

    /// <summary>
    /// Gets the current connector.
    /// </summary>
    public ConnectorViewModel CurrentConnector => this.currentConnector ?? (ConnectorViewModel)this.uxConnectors.SelectedItem;

    /// <summary>
    /// Gets or sets a value indicating whether active connector supports wizard.
    /// </summary>
    public bool ActiveConnectorSupportsWizard
    {
      get => this.activeConnectorSupportsWizard;
      set
      {
        this.activeConnectorSupportsWizard = value;
        this.OnPropertyChanged(nameof(this.ActiveConnectorSupportsWizard));
      }
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Handles the Loaded event of the Connectors control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
    private void ConnectorsLoaded(object sender, System.EventArgs e)
    {
      if (this.uxConnectors.Items.Count > 0 && this.uxConnectors.SelectedItem == null)
      {
        this.uxConnectors.SelectedItem = this.uxConnectors.Items[0];
      }
    }

    /// <summary>
    /// Handles the SelectionChanged event of the uxConnectors control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void ConnectorsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!Validation.IsValid(this) && e.RemovedItems.Count > 0)
      {
        e.Handled = true;
        try
        {
          this.uxConnectors.SelectionChanged -= this.ConnectorsSelectionChanged;
          this.uxConnectors.SelectedItem = e.RemovedItems[0];
          return;
        }
        finally
        {
          this.uxConnectors.SelectionChanged += this.ConnectorsSelectionChanged;
          this.currentConnector = null; // just mark that the backing field  should be initialized.
        }
      }

      if (!this.IsInitialized)
      {
        return;
      }

      this.SetConnectorFrameContent(e.AddedItems.Count != 0 ? (ConnectorViewModel)e.AddedItems[0] : null);
    }

    /// <summary>
    /// Handles the Initialized event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void PageInitialized(object sender, System.EventArgs e)
    {
      this.SetConnectorFrameContent((ConnectorViewModel)this.uxConnectors.SelectedItem);
    }

    /// <summary>
    /// Sets the content of the connector frame.
    /// </summary>
    /// <param name="connector">The connector.</param>
    private void SetConnectorFrameContent(ConnectorViewModel connector)
    {
      this.uxConnectorFrame.Content = new ConnectorConfigPage(connector);
      this.ActiveConnectorSupportsWizard = this.CurrentConnector?.SourceConnectorPlugin?.SupportsWizard ?? false;
    }

    /// <summary>
    /// Handles the request for connector deletion.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void DeleteConnectorClick(object sender, System.Windows.RoutedEventArgs e)
    {
      var result = (bool)await DialogHost.Show(new OkCancelDialog("Are you sure you want to delete this connector?"), "ConnectorsConfigPageHost"); // TODO translate
      if (result)
      {
        this.Connectors.Remove(this.CurrentConnector);
      }
    }

    /// <summary>
    /// Handles the request for connector rename.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void RenameConnectorClick(object sender, System.Windows.RoutedEventArgs e)
    {
      if (this.uxConnectors.SelectedItem == null)
      {
        return;
      }

      await DialogHost.Show(new CreateEditConnectorDialog((ConnectorViewModel)this.uxConnectors.SelectedItem, false), "ConnectorsConfigPageHost");
    }

    /// <summary>
    /// Handles the request for connector rename.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void AddConnectorClick(object sender, System.Windows.RoutedEventArgs e)
    {
      if (!Validation.IsValid(this))
      {
        return;
      }

      this.currentConnector = new ConnectorViewModel();
      var createEditDialod = new CreateEditConnectorDialog(this.currentConnector, true);
      var result = (bool)await DialogHost.Show(createEditDialod, "ConnectorsConfigPageHost");
      if (result)
      {
        // TODO move to connector view model/model
        this.currentConnector.SourceConnectorPlugin = (IConnectorPlugin)createEditDialod.uxPluginType.SelectedItem;
        this.currentConnector.Load(null);
        this.Connectors.Add(this.currentConnector);
        this.uxConnectors.SelectedItem = this.currentConnector;
      }
    }

    /// <summary>
    /// Handles the start wizard button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private void StartWizardClick(object sender, System.Windows.RoutedEventArgs e)
    {
      var wizardController = new WizardController(this.ownerWindow);
      if (wizardController.Start(this.CurrentConnector.SourceConnectorPlugin))
      {
        var selectedProjects = wizardController.GetSelectedProjects();
        if (selectedProjects.Count != 1)
        {
          throw new InvalidOperationException("One selected project is required.");
        }

        var currentConnectorAsAssignable = this.CurrentConnector.SourceConnectorPlugin as IAssignServerProject;
        if (currentConnectorAsAssignable == null)
        {
          throw new InvalidOperationException("Connector does not support assign from server project.");
        }

        currentConnectorAsAssignable.AssignServerProject(selectedProjects[0], this.CurrentConnector, wizardController.ProposedServerAddress);
      }
    }
  }
}
