// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopyBuildLabelCommand.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows;

  public class CopyBuildLabelCommand : ExternalEnabledStateCommand
  {
    public override void Execute(object parameter)
    {
      if (parameter is BuildStatusViewModel buildStatus && !string.IsNullOrEmpty(buildStatus.Label))
      {
        Clipboard.SetText(buildStatus.Label);
      }
    }
  }
}