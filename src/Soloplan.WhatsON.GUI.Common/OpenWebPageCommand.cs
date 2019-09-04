// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpenWebPageCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using NLog;

  public class OpenWebPageCommand : ExternalEnabledStateCommand
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public override void Execute(object parameter)
    {
      if (parameter is string url)
      {
        log.Debug("Opening web page {@address}.", url);
        System.Diagnostics.Process.Start(url);
        return;
      }

      log.Warn("Can't open web page because the pram is of incorrect type {type}", new { Type = parameter?.GetType() });
    }

    protected override bool CanExecuteInternal(object parameter)
    {
      if (parameter is string url && !string.IsNullOrEmpty(url))
      {
        log.Debug("Checking if command should be opened for {@param}", url);
        return base.CanExecuteInternal(parameter);
      }

      log.Warn("Webpage opening parameters are invalid {parameter}", new { Type = parameter?.GetType() });
      return false;
    }
  }
}