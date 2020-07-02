// <copyright file="ConnectorViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using NLog;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.Model;

  public class ConnectorViewModel : TreeItemViewModel
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private ObservableCollection<BuildStatusViewModel> connectorSnapshots;

    private string name;

    private string description;

    private BuildStatusViewModel currentStatus;

    private string url;

    public ConnectorViewModel()
    {
    }

    public ConnectorViewModel(Connector connector)
    {
      if (connector != null)
      {
        this.Identifier = connector.Configuration.Identifier;
        this.Name = connector.Configuration.Name;
        this.CurrentStatus = this.GetStatusViewModel();
      }
    }

    public string Name
    {
      get => this.name;
      set
      {
        if (this.name != value)
        {
          this.name = value;
          this.OnConfigurationChanged(this, EventArgs.Empty);
          this.OnPropertyChanged();
        }
      }
    }

    public string Description
    {
      get => this.description;
      protected set
      {
        if (this.description != value)
        {
          this.description = value;
          this.OnPropertyChanged();
        }
      }
    }

    public BuildStatusViewModel CurrentStatus
    {
      get => this.currentStatus;
      private set
      {
        if (!object.ReferenceEquals(this.currentStatus, value))
        {
          this.currentStatus = value;
          this.OnPropertyChanged();
        }
      }
    }

    public string Url
    {
      get => this.url;
      protected set
      {
        if (this.url != value)
        {
          this.url = value;
          this.OnPropertyChanged();
        }
      }
    }

    public Guid Identifier { get; private set; }

    public ObservableCollection<BuildStatusViewModel> ConnectorSnapshots => this.connectorSnapshots ?? (this.connectorSnapshots = new ObservableCollection<BuildStatusViewModel>());

    public Connector Connector { get; private set; }

    /// <summary>
    /// Gets command for opening builds webPage.
    /// </summary>
    public virtual OpenWebPageCommand OpenWebPage { get; } = new OpenWebPageCommand();

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      base.OnDoubleClick(sender, e);
      if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext == this && this.OpenWebPage.CanExecute(this.Url))
      {
        this.OpenWebPage.Execute(this.Url);
      }
    }

    /// <summary>
    /// Applies configuration (currently just name) without firing property changed.
    /// </summary>
    /// <param name="configuration">The configuration to apply.</param>
    public virtual void ApplyConfiguration(ConnectorConfiguration configuration)
    {
      this.name = configuration.Name;
    }

    public virtual void Update(Connector changedConnector)
    {
      Application.Current.Dispatcher.Invoke(new Action(() =>
      {
        log.Trace("Updating model {model}", new { this.Name, this.Identifier });

        if (this.Connector == null)
        {
          this.Connector = changedConnector;
        }

        if (changedConnector == null)
        {
          return;
        }

        int i = 0;
        bool clearList = false;
        foreach (var changedConnectorSnapshot in changedConnector.Snapshots)
        {
          if (i >= this.ConnectorSnapshots.Count || this.ConnectorSnapshots[i].Time.ToUniversalTime() != changedConnectorSnapshot.Status.Time)
          {
            log.Debug("Rebuilding list of history builds for model {type}, {instance}.", this.GetType(), new { this.Name, this.Identifier });
            clearList = true;
            break;
          }

          i++;
        }

        if (clearList)
        {
          this.ConnectorSnapshots.Clear();
          foreach (var snapshot in changedConnector.Snapshots)
          {
            var viewModel = this.GetStatusViewModel();
            viewModel.Update(snapshot.Status);
            if (changedConnector.Snapshots[0] == snapshot)
            {
              viewModel.First = true;
            }

            this.ConnectorSnapshots.Add(viewModel);
          }
        }

        this.Description = changedConnector.Description;
        this.CurrentStatus.Update(changedConnector.CurrentStatus);
      }));
    }

    /// <summary>
    /// Implements function that decides if a tooltip should be visible.
    /// </summary>
    /// <returns>Base returns always true.</returns>
    public virtual bool ShouldDisplayTooltip()
    {
      return true;
    }

    protected virtual BuildStatusViewModel GetStatusViewModel()
    {
      return new BuildStatusViewModel(this);
    }

    /// <summary>
    /// Creates command used for editing this tree item.
    /// </summary>
    /// <returns>Command used to edit tree item.</returns>
    protected override CustomCommand CreateEditCommand()
    {
      var command = base.CreateEditCommand();
      command.CanExecuteExternal += (s, e) => { e.Cancel = this.ConfigurationModifiedInTree || !this.IsOnlyOneSelected; };
      return command;
    }
  }
}
