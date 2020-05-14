// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using System;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  public class CruiseControlProjectViewModel : ConnectorViewModel
  {
    public CruiseControlProjectViewModel(CruiseControlConnector connector)
      : base(connector)
    {
      this.Url = CruiseControlServer.UrlHelper.GetReportUrl(connector.Address);
    }

    protected override BuildStatusViewModel GetStatusViewModel()
    {
      return new CruiseControlStatusViewModel(this);
    }
  }
}