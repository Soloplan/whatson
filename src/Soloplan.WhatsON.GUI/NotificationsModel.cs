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
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;

  public class NotificationsModel : NotifyPropertyChanged
  {
    private readonly ObservableCollection<SubjectViewModel> subjects;

    public NotificationsModel(ObservationScheduler scheduler)
    {
      this.subjects = new ObservableCollection<SubjectViewModel>();
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    public ObservableCollection<SubjectViewModel> Subjects => this.subjects;

    private void SchedulerStatusQueried(object sender, Subject subject)
    {
      var modelToUpdate = this.GetModelToUpdate(subject);
      modelToUpdate.Update(subject);
      modelToUpdate.CurrentStatus.PropertyChanged -= this.OnPropertyChanged;
      modelToUpdate.CurrentStatus.PropertyChanged += this.OnPropertyChanged;
    }

    private SubjectViewModel GetModelToUpdate(Subject subject)
    {
      var modelToUpdate = this.subjects.FirstOrDefault(sub => sub.Identifier == subject.SubjectConfiguration.Identifier);
      if (modelToUpdate == null)
      {
        var presentationPlugIn = PluginsManager.Instance.GetPresentationPlugIn(subject.GetType());
        if (presentationPlugIn != null)
        {
          modelToUpdate = presentationPlugIn.CreateViewModel();
          modelToUpdate.Init(subject.SubjectConfiguration);
          modelToUpdate.PropertyChanged += this.OnPropertyChanged;
          this.subjects.Add(modelToUpdate);
        }
      }

      return modelToUpdate;
    }
  }
}