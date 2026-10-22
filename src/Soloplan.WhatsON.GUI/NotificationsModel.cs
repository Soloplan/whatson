// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class NotificationsModel : IDisposable
  {
    private readonly ObservationScheduler scheduler;

    private object lockObj = new object();

    public NotificationsModel(ObservationScheduler scheduler)
    {
      this.Connectors = new ObservableCollection<ConnectorViewModel>();
      this.scheduler = scheduler;
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    public event PropertyChangedEventHandler? StatusChanged;

    public ObservableCollection<ConnectorViewModel> Connectors { get; }

    public void Dispose()
    {
      this.scheduler.StatusQueried -= this.SchedulerStatusQueried;
      foreach (var connector in this.Connectors)
      {
        connector.PropertyChanged -= this.ForwardStatusChanged;
        connector.CurrentStatus.PropertyChanged -= this.ForwardStatusChanged;
      }
    }

    private void SchedulerStatusQueried(object? sender, Connector connector)
    {
      var modelToUpdate = this.GetModelToUpdate(connector);
      var previous = (modelToUpdate.CurrentStatus.State, modelToUpdate.CurrentStatus.BuildNumber, modelToUpdate.CurrentStatus.Time);
      modelToUpdate.CurrentStatus.PropertyChanged -= this.ForwardStatusChanged;
      modelToUpdate.Update(connector);
      modelToUpdate.CurrentStatus.PropertyChanged += this.ForwardStatusChanged;
      var current = (modelToUpdate.CurrentStatus.State, modelToUpdate.CurrentStatus.BuildNumber, modelToUpdate.CurrentStatus.Time);
      if (connector.CurrentStatus != null && previous != current)
      {
        this.ForwardStatusChanged(modelToUpdate.CurrentStatus, new PropertyChangedEventArgs(nameof(StatusViewModel.State)));
      }

      this.ForwardStatusChanged(modelToUpdate.CurrentStatus, new PropertyChangedEventArgs(nameof(ConnectorViewModel.CurrentStatus)));
    }

    private void ForwardStatusChanged(object? sender, PropertyChangedEventArgs eventArgs) => this.StatusChanged?.Invoke(sender, eventArgs);

    private ConnectorViewModel GetModelToUpdate(Connector connector)
    {
      lock (this.lockObj)
      {
        var modelToUpdate = this.Connectors.FirstOrDefault(sub => sub.Identifier == connector.Configuration.Identifier);
        if (modelToUpdate == null)
        {
          var presentationPlugIn = PluginManager.Instance.GetPresentationPlugin(connector.Configuration.Type);
          if (presentationPlugIn != null)
          {
            modelToUpdate = presentationPlugIn.CreateViewModel(connector);
          }
          else
          {
            modelToUpdate = new ConnectorViewModel(connector);
          }

          modelToUpdate.PropertyChanged += this.ForwardStatusChanged;
          this.Connectors.Add(modelToUpdate);
        }

        return modelToUpdate;
      }
    }
  }
}
