// ----------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPresentationPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class JenkinsPresentationPlugin : PresentationPlugin
  {
    public JenkinsPresentationPlugin()
      : base(JenkinsConnector.ConnectorName, Properties.Resources.JenkinsProjectDataTemplate)
    {
    }

    public override ConnectorViewModel CreateViewModel(Connector connector)
    {
      return new JenkinsProjectViewModel(connector);
    }
  }
}
