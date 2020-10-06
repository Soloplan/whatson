// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class NotificationsModel : NotifyPropertyChanged
  {
    private readonly ObservableCollection<ConnectorViewModel> connectors;
    private object lockObj = new object();

    public NotificationsModel(ObservationScheduler scheduler)
    {
      this.connectors = new ObservableCollection<ConnectorViewModel>();
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    public ObservableCollection<ConnectorViewModel> Connectors => this.connectors;

    private void SchedulerStatusQueried(object sender, Connector connector)
    {
      var modelToUpdate = this.GetModelToUpdate(connector);
      modelToUpdate.Update(connector);
      modelToUpdate.CurrentStatus.PropertyChanged -= this.OnPropertyChanged;
      modelToUpdate.CurrentStatus.PropertyChanged += this.OnPropertyChanged;
    }

    private ConnectorViewModel GetModelToUpdate(Connector connector)
    {
      lock (this.lockObj)
      {
        var modelToUpdate = this.connectors.FirstOrDefault(sub => sub.Identifier == connector.Configuration.Identifier);
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

          modelToUpdate.PropertyChanged += this.OnPropertyChanged;
          this.connectors.Add(modelToUpdate);
        }

        return modelToUpdate;
      }
    }
  }
}