// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpenWebPageCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.ComponentModel;
  using System.Windows.Input;
  using NLog;

  public class OpenWebPageCommand : ICommand
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public event EventHandler CanExecuteChanged;

    public event CancelEventHandler CanExecuteExternal;

    public bool CanExecute(object parameter)
    {
      if (parameter is OpenWebPageCommandData webPageParam && !string.IsNullOrEmpty(webPageParam.Address))
      {
        log.Debug("Checking if command should be opened for {@param}", webPageParam);
        var cancelEventArgs = new CancelEventArgs();
        this.CanExecuteExternal?.Invoke(this, cancelEventArgs);
        log.Debug("Command active = {value}", !cancelEventArgs.Cancel);
        return !cancelEventArgs.Cancel;
      }

      log.Warn("Webpage opening parameters are invalid {parameter}", new { Type = parameter?.GetType() });
      return false;
    }

    public void Execute(object parameter)
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