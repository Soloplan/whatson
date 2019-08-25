// <copyright file="ConnectorViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.ObjectModel;
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

    public string Name
    {
      get => this.name;
      protected set
      {
        if (this.name != value)
        {
          this.name = value;
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

    public Guid Identifier { get; private set; }

    public ObservableCollection<BuildStatusViewModel> ConnectorSnapshots => this.connectorSnapshots ?? (this.connectorSnapshots = new ObservableCollection<BuildStatusViewModel>());

    public Connector Connector { get; private set; }

    /// <summary>
    /// Gets command for opening builds webPage.
    /// </summary>
    public virtual OpenWebPageCommand OpenWebPage { get; } = new OpenWebPageCommand();

    public virtual OpenWebPageCommandData OpenWebPageParam { get; set; }

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      base.OnDoubleClick(sender, e);
      var treeViewItem = sender as TreeViewItem;
      if (treeViewItem != null && treeViewItem.DataContext == this && this.OpenWebPage.CanExecute(this.OpenWebPageParam))
      {
        this.OpenWebPage.Execute(this.OpenWebPageParam);
      }
    }

    public virtual void Init(ConnectorConfiguration configuration)
    {
      this.Identifier = configuration.Identifier;
      this.Name = configuration.Name;
      this.CurrentStatus = this.GetViewModelForStatus();
      log.Debug("Initializing {type}, {instance}.", this.GetType(), new { Name = this.Name, Identifier = this.Identifier });
    }

    public virtual void Update(Connector changedConnector)
    {
      log.Trace("Updating model {model}", new { Name = this.Name, Identifier = this.Identifier });

      if (this.Connector == null)
      {
        this.Connector = changedConnector;
      }

      int i = 0;
      bool clearList = false;
      foreach (var changedConnectorSnapshot in changedConnector.Snapshots)
      {
        if (i >= this.ConnectorSnapshots.Count || this.ConnectorSnapshots[i].Time.ToUniversalTime() != changedConnectorSnapshot.Status.Time)
        {
          log.Debug("Rebuilding list of history builds for model {type}, {instance}.", this.GetType(), new { Name = this.Name, Identifier = this.Identifier });
          clearList = true;
          break;
        }

        i++;
      }

      if (clearList)
      {
        this.ConnectorSnapshots.Clear();
        foreach (var connectorSnapshot in changedConnector.Snapshots)
        {
          var connectorSnapshotViewModel = this.GetViewModelForStatus();
          connectorSnapshotViewModel.Update(connectorSnapshot.Status);
          connectorSnapshotViewModel.Age = connectorSnapshot.Age;
          this.ConnectorSnapshots.Add(connectorSnapshotViewModel);
        }
      }

      this.Description = changedConnector.Description;
      this.CurrentStatus.Update(changedConnector.CurrentStatus);
    }

    protected virtual BuildStatusViewModel GetViewModelForStatus()
    {
      BuildStatusViewModel result = new BuildStatusViewModel(this);
      return result;
    }

    /// <summary>
    /// Creates command used for editing this tree item.
    /// </summary>
    /// <returns>Command used to edit tree item.</returns>
    protected override CustomCommand CreateEditCommand()
    {
      var command = base.CreateEditCommand();
      command.CanExecuteExternal += (s, e) => e.Cancel = this.ConfigurationModifiedInTree;
      return command;
    }
  }
}
