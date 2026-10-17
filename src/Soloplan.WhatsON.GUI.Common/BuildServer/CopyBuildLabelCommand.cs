// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopyBuildLabelCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Runtime.InteropServices;
  using System.Windows;
  using NLog;

  public class CopyBuildLabelCommand : ExternalEnabledStateCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public override void Execute(object? parameter)
    {
      if (parameter is BuildStatusViewModel buildStatus && !string.IsNullOrEmpty(buildStatus.Label))
      {
        try
        {
          Clipboard.SetText(buildStatus.Label);
        }
        catch (COMException exception)
        {
          log.Warn(exception, "Could not copy the build label to the clipboard.");
        }
      }
    }
  }
}
