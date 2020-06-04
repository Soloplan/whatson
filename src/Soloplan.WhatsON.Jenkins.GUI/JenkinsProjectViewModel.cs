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

    public override void MakeToast()
    {
      ToastGenerator toastGenerator = new ToastGenerator();
      var toastContent = toastGenerator.GenerateToastContent(this);

      if (this.CurrentStatus.State == ObservationState.Running)
      {
        var progressBar = new AdaptiveProgressBar();
        progressBar.Title = "Build progress";
        progressBar.Value = new BindableProgressBarValue("progressValue");
        progressBar.ValueStringOverride = new BindableString("progressValueString");
        progressBar.Status = new BindableString("progressStatus");
        toastContent.Visual.BindingGeneric.Children.Add(progressBar);
      }

      var xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(toastContent.GetContent());

      var toast = new ToastNotification(xmlDoc);

      if (this.CurrentStatus.State == ObservationState.Running)
      {
        toast.Data = new NotificationData();

        toast.Data.Values["progressValue"] = ((float)this.CurrentStatus.Progress / 100f).ToString().Replace(',','.');
        toast.Data.Values["progressValueString"] = "ETA: " + this.CurrentStatus.EstimatedRemaining.ToString();
        toast.Data.Values["progressStatus"] = "Building...";
      }

      var toastNotifier = ToastNotificationManager.CreateToastNotifier();
      toastNotifier.Show(toast);
    }
  }
}
