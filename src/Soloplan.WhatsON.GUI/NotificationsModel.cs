// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class NotificationsModel
  {
    private object lockObj = new object();

    public NotificationsModel(ObservationScheduler scheduler)
    {
      this.Connectors = new ObservableCollection<ConnectorViewModel>();
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    public event PropertyChangedEventHandler StatusChanged;

    public ObservableCollection<ConnectorViewModel> Connectors { get; }

    private void SchedulerStatusQueried(object sender, Connector connector)
    {
      var modelToUpdate = this.GetModelToUpdate(connector);
      modelToUpdate.Update(connector);
      modelToUpdate.CurrentStatus.PropertyChanged -= this.ForwardStatusChanged;
      modelToUpdate.CurrentStatus.PropertyChanged += this.ForwardStatusChanged;
      this.ForwardStatusChanged(modelToUpdate.CurrentStatus, new PropertyChangedEventArgs(nameof(ConnectorViewModel.CurrentStatus)));
    }

    private void ForwardStatusChanged(object sender, PropertyChangedEventArgs eventArgs) => this.StatusChanged?.Invoke(sender, eventArgs);

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
