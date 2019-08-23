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
      if (parameter is BuildStatusViewModel buildStatus && !string.IsNullOrEmpty(buildStatus.BuildLabel))
      {
        Clipboard.SetText(buildStatus.BuildLabel);
      }
    }
  }
}