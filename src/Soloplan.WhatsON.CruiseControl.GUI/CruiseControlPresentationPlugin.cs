// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlPresentationPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class CruiseControlPresentationPlugin : PresentationPlugin
  {
    public CruiseControlPresentationPlugin()
      : base(CruiseControlConnector.ConnectorName, Properties.Resources.CcProjectDataTemplate)
    {
    }

    public override ConnectorViewModel CreateViewModel(Connector connector)
    {
      return new CruiseControlProjectViewModel(connector as CruiseControlConnector);
    }
  }
}