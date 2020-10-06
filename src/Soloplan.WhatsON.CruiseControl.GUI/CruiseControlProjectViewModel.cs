// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class CruiseControlProjectViewModel : ConnectorViewModel
  {
    public CruiseControlProjectViewModel(CruiseControlConnector connector)
      : base(connector)
    {
      this.Url = CruiseControlServer.UrlHelper.GetReportUrl(connector.directAddress, this.Connector?.Project);
    }

    /// <summary>
    /// Implements function that decides if a tooltip should be visible.
    /// </summary>
    /// <returns>True when should be visible, false when should not be visible.</returns>
    public override bool ShouldDisplayTooltip()
    {
      if (this.CurrentStatus is CruiseControlStatusViewModel status)
      {
        return (status.Culprits.Count == 0 && status.State != ObservationState.Running && status.State != ObservationState.Unknown) ? false : true;
      }

      return true;
    }

    public override void ApplyConfiguration(ConnectorConfiguration configuration)
    {
      base.ApplyConfiguration(configuration);
      this.Url = CruiseControlServer.UrlHelper.GetReportUrl(this.Connector.directAddress, this.Connector?.Project);
    }

    protected override BuildStatusViewModel GetStatusViewModel()
    {
      return new CruiseControlStatusViewModel(this);
    }
  }
}
