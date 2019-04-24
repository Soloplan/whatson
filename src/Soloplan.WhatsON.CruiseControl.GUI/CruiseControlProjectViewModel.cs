// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;

  public class CruiseControlProjectViewModel : BuildServerProjectStatusViewModel
  {
    public override ICommand OpenWebPage => null;

    public override object OpenWebPageParam
    {
      get => null;
      set { }
    }

    protected override StatusViewModel GetViewModelForStatus()
    {
      var ccStatusModel = new CruiseControlStatusViewModel(this);
      return ccStatusModel;
    }
  }
}