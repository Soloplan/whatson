// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopyBuildLabelCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows;

  public class CopyBuildLabelCommand : ExternalEnabledStateCommand
  {
    public override void Execute(object parameter)
    {
      if (parameter is BuildStatusViewModel buildStatus)
      {
        if (!string.IsNullOrEmpty(buildStatus.DisplayName))
        {
          Clipboard.SetText(buildStatus.DisplayName);
        }
        else if (buildStatus.BuildNumber.HasValue)
        {
          Clipboard.SetText($"#{buildStatus.BuildNumber}");
        }
      }
    }
  }
}