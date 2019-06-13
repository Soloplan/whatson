// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpenWebPageCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using NLog;
  using Soloplan.WhatsON.GUI.Common;

  public class OpenWebPageCommand : ExternalEnabledStateCommand
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public override bool CanExecute(object parameter)
    {
      if (parameter is OpenWebPageCommandData webPageParam && !string.IsNullOrEmpty(webPageParam.Address))
      {
        log.Debug("Checking if command should be opened for {@param}", webPageParam);
        return base.CanExecute(parameter);
      }

      log.Warn("Webpage opening parameters are invalid {parameter}", new { Type = parameter?.GetType() });
      return false;
    }

    public override void Execute(object parameter)
    {
      if (parameter is OpenWebPageCommandData webPageParam)
      {
        log.Debug("Opening web page {@address}.", webPageParam);
        System.Diagnostics.Process.Start(webPageParam.FullAddress);
        return;
      }

      log.Warn("Can't open web page because the pram is of incorrect type {type}", new { Type = parameter?.GetType() });
    }
  }

  public class OpenWebPageCommandData
  {
    public virtual string FullAddress => this.Address;

    public string Address { get; set; }
  }
}