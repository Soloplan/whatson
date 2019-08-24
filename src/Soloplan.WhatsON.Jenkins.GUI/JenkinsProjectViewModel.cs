// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsProjectViewModel : BuildServerProjectStatusViewModel
  {
    private OpenJenkinsWebPageCommandData openWebPageParam;

    public override OpenWebPageCommandData OpenWebPageParam
    {
      get => this.openWebPageParam;
      set
      {
        this.openWebPageParam = value as OpenJenkinsWebPageCommandData;
        this.OnPropertyChanged(nameof(this.OpenWebPageParam));
      }
    }

    public override void Init(ConnectorConfiguration configuration)
    {
      base.Init(configuration);

      if (this.CurrentStatus is JenkinsStatusViewModel status)
      {
        (status.OpenBuildPage as OpenWebPageCommand).CanExecuteExternal += (s, e) => e.Cancel = this.CurrentStatus is JenkinsStatusViewModel model && !model.Building;
      }

      OpenJenkinsWebPageCommandData param = new OpenJenkinsWebPageCommandData();
      if (bool.TryParse(configuration.GetConfigurationByKey(JenkinsProject.RedirectPlugin)?.Value, out var redirect) && redirect)
      {
        param.Redirect = true;
      }
      else
      {
        param.Redirect = false;
      }

      param.Address = configuration.GetConfigurationByKey(ServerConnector.ServerAddress).Value.Trim('/') + "/job/" + configuration.GetConfigurationByKey(JenkinsProject.ProjectName).Value.Trim('/');

      this.OpenWebPageParam = param;

      this.SetAddressForState(this.CurrentStatus);

      foreach (var connectorSnapshot in this.ConnectorSnapshots)
      {
        this.SetAddressForState(connectorSnapshot);
      }
    }

    protected override StatusViewModel GetViewModelForStatus()
    {
      var jenkinsModel = new JenkinsStatusViewModel(this);
      this.SetAddressForState(jenkinsModel);
      this.SetAddressForState(jenkinsModel);
      return jenkinsModel;
    }

    private void SetAddressForState(StatusViewModel model)
    {
      if (model is JenkinsStatusViewModel jenkinsModel)
      {
        jenkinsModel.SetJobAddress(this.openWebPageParam);
      }
    }
  }
}
