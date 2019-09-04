// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class JenkinsProjectViewModel : ConnectorViewModel
  {
    public JenkinsProjectViewModel(Connector connector)
      : base(connector)
    {
      this.CurrentStatus.OpenBuildPage.CanExecuteExternal += (s, e) => e.Cancel = !this.CurrentStatus.Building;

      var address = connector.Configuration.GetConfigurationByKey(Connector.ServerAddress).Value.Trim('/') + "/job/" + connector.Configuration.GetConfigurationByKey(JenkinsConnector.ProjectName).Value.Trim('/');
      if (bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect) && redirect)
      {
        address += JenkinsApi.UrlHelper.RedirectPluginUrlSuffix;
      }

      this.Url = address;
    }

    protected override BuildStatusViewModel GetStatusViewModel()
    {
      return new JenkinsStatusViewModel(this);
    }
  }
}
