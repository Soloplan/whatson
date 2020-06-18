// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Microsoft.Toolkit.Uwp.Notifications;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using System;
  using Soloplan.WhatsON.Model;
  using Windows.Data.Xml.Dom;
  using Windows.UI.Notifications;

  public class JenkinsProjectViewModel : ConnectorViewModel
  {
    public JenkinsProjectViewModel(Connector connector)
      : base(connector)
    {
      this.CurrentStatus.OpenBuildPage.CanExecuteExternal += (s, e) => e.Cancel = !this.CurrentStatus.Building;
      this.Url = JenkinsApi.UrlHelper.ProjectUrl(connector);
    }

    protected override BuildStatusViewModel GetStatusViewModel()
    {
      return new JenkinsStatusViewModel(this);
    }

    /// <summary>
    /// Creates a toast content for a win10 notification.
    /// </summary>
    /// <param name="connectorGroupViewModel"> Group in which the connector is located.</param>
    /// <returns>Toast notification to be shown.</returns>
    public override ToastNotification MakeToast(ConnectorGroupViewModel connectorGroupViewModel=null)
    {
      ToastGenerator toastGenerator = new ToastGenerator();
      var toastContent = toastGenerator.GenerateToastContent(this,connectorGroupViewModel);

      if (this.CurrentStatus.State == ObservationState.Running)
      {
        var progressBar = new AdaptiveProgressBar();
        progressBar.Title = "Build progress";
        progressBar.Value = new BindableProgressBarValue("progressValue");
        progressBar.ValueStringOverride = new BindableString("progressValueString");
        progressBar.Status = new BindableString("progressStatus");
        toastContent.Visual.BindingGeneric.Children.Insert(toastContent.Visual.BindingGeneric.Children.Count,progressBar);
      }

      var xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(toastContent.GetContent());

      var toast = new ToastNotification(xmlDoc);

      if (this.CurrentStatus.State == ObservationState.Running)
      {
        toast.Data = new NotificationData();

        if (this.CurrentStatus.RawProgress < 100)
        {
          toast.Data.Values["progressValue"] = ((float)this.CurrentStatus.Progress / 100f).ToString().Replace(',', '.');
          toast.Data.Values["progressValueString"] = "Progress:" + this.CurrentStatus.Progress + " ETA: " + this.CurrentStatus.EstimatedRemaining.Hours
          + ":" + this.CurrentStatus.EstimatedRemaining.Minutes + ":" + this.CurrentStatus.EstimatedRemaining.Seconds;
        }
        else
        {
          toast.Data.Values["progressValue"] = 100.0f.ToString().Replace(',', '.');
          toast.Data.Values["progressValueString"] = "Taking longer than expected.";
        }

        toast.Data.Values["progressStatus"] = "Building...";
      }

      toast.ExpirationTime = DateTimeOffset.Now + TimeSpan.FromDays(1);
      return toast;
    }

    /// <summary>
    /// Implements update function for notifications of Jenkins projects.
    /// </summary>
    /// <param name="tag">Toast tag</param>
    /// <param name="sequence">Sequence of update. </param>
    /// <param name="group">Group of notification. </param>
    /// <returns>Notification data to be sent. </returns>
    public override NotificationData CreateNotificationsDataUpdate(uint tag, uint sequence, string group)
    {
      var data = new NotificationData();

      data.SequenceNumber = 0;
      data = new NotificationData();

      if (this.CurrentStatus.RawProgress < 100)
      {
        data.Values["progressValue"] = ((float)this.CurrentStatus.Progress / 100f).ToString().Replace(',', '.');
        data.Values["progressValueString"] = "Progress:" + this.CurrentStatus.Progress + " ETA: " + this.CurrentStatus.EstimatedRemaining.Hours
        + ":" + this.CurrentStatus.EstimatedRemaining.Minutes + ":" + this.CurrentStatus.EstimatedRemaining.Seconds;
      }
      else
      {
        data.Values["progressValue"] = 100.0f.ToString().Replace(',', '.');
        data.Values["progressValueString"] = "Taking longer than expected.";
      }

      data.Values["progressStatus"] = "Building...";

      return data;
    }
  }
}
