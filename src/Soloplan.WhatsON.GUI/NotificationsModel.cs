// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  public class NotificationsModel : NotifyPropertyChanged
  {
    private readonly ObservableCollection<ConnectorViewModel> connectors;

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
      var modelToUpdate = this.connectors.FirstOrDefault(sub => sub.Identifier == connector.ConnectorConfiguration.Identifier);
      if (modelToUpdate == null)
      {
        var presentationPlugIn = PluginsManager.Instance.GetPresentationPlugIn(connector.GetType());
        if (presentationPlugIn != null)
        {
          modelToUpdate = presentationPlugIn.CreateViewModel();
          modelToUpdate.Init(connector.ConnectorConfiguration);
          modelToUpdate.PropertyChanged += this.OnPropertyChanged;
          this.connectors.Add(modelToUpdate);
        }
      }

      return modelToUpdate;
    }
  }
}