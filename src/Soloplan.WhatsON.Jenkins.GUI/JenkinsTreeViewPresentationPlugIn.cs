// <copyright file="JenkinsTreeViewPresentationPlugIn.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.IO;
  using System.Text;
  using System.Xml;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins.Model;

  public class JenkinsTreeViewPresentationPlugIn : IPresentationPlugin
  {
    public Type ConnectorType => typeof(JenkinsProject);

    public ConnectorViewModel CreateViewModel()
    {
      return new JenkinsProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      return XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.JenkinsProjectDataTemplate)));
    }
  }
}
