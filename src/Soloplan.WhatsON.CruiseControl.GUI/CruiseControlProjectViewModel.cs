// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  public class CruiseControlProjectViewModel : ConnectorViewModel
  {
    public override OpenWebPageCommandData OpenWebPageParam
    {
      get
      {
        if (this.CurrentStatus is CruiseControlStatusViewModel ccModel && !string.IsNullOrEmpty(ccModel.OpenBuildPageCommandData?.Address))
        {
          return new OpenWebPageCommandData { Address = ccModel.OpenBuildPageCommandData.Address.Replace("ViewLatestBuildReport.aspx", "ViewProjectReport.aspx") };
        }

        return null;
      }

      set
      {
      }
    }

    protected override StatusViewModel GetViewModelForStatus()
    {
      var ccStatusModel = new CruiseControlStatusViewModel(this);
      return ccStatusModel;
    }
  }
}