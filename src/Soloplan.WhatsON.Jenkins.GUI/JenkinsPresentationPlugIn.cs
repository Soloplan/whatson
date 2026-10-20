// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPresentationPlugIn.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System.Windows;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class JenkinsPresentationPlugin : PresentationPlugin
  {
    public JenkinsPresentationPlugin()
      : base(JenkinsConnector.ConnectorName, Properties.Resources.JenkinsProjectDataTemplate)
    {
    }

    public override ConnectorViewModel CreateViewModel(Connector connector) => new JenkinsProjectViewModel(connector);

    public override FrameworkElement? CreateConfigurationAction(string key, IConfigurationItemProvider configuration) =>
      key == JenkinsConnector.ApiToken ? new JenkinsCredentialsTestControl(configuration) : null;
  }
}
