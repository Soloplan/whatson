// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;
  using Soloplan.WhatsON.ServerBase;

  public class JenkinsProjectViewModel : BuildServerProjectStatusViewModel
  {
    private OpenWebPageCommandData openWebPageParam;

    /// <summary>
    /// Gets command for opening builds webPage.
    /// </summary>
    public override ICommand OpenWebPage { get; } = new OpenWebPageCommand();

    public override object OpenWebPageParam
    {
      get => this.openWebPageParam;
      set
      {
        this.openWebPageParam = value as OpenWebPageCommandData;
        this.OnPropertyChanged(nameof(this.OpenWebPageParam));
      }
    }

    public override void Init(SubjectConfiguration configuration)
    {
      base.Init(configuration);

      if (this.CurrentStatus is JenkinsStatusViewModel status)
      {
        (status.OpenBuildPage as OpenWebPageCommand).CanExecuteExternal += (s, e) => e.Cancel = this.CurrentStatus is JenkinsStatusViewModel model && !model.Building;
      }

      OpenWebPageCommandData param = new OpenWebPageCommandData();
      if (bool.TryParse(configuration.GetConfigurationByKey(JenkinsProject.RedirectPlugin)?.Value, out var redirect) && redirect)
      {
        param.Redirect = true;
      }
      else
      {
        param.Redirect = false;
      }

      param.Address = configuration.GetConfigurationByKey(ServerSubject.ServerAddress).Value + "/job/" + configuration.GetConfigurationByKey(JenkinsProject.ProjectName).Value;

      this.OpenWebPageParam = param;

      this.SetAddressForState(this.CurrentStatus);

      foreach (var subjectSnapshot in this.SubjectSnapshots)
      {
        this.SetAddressForState(subjectSnapshot);
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
