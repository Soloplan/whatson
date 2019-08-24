// <copyright file="ConnectorViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using NLog;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  public class ConnectorViewModel : TreeItemViewModel
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private ObservableCollection<StatusViewModel> connectorSnapshots;

    private string name;

    private string description;

    private StatusViewModel currentStatus;

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

    public StatusViewModel CurrentStatus
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

    public ObservableCollection<StatusViewModel> ConnectorSnapshots => this.connectorSnapshots ?? (this.connectorSnapshots = new ObservableCollection<StatusViewModel>());

    public Connector Connector { get; private set; }

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

    protected virtual StatusViewModel GetViewModelForStatus()
    {
      StatusViewModel result = new StatusViewModel(this);
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
