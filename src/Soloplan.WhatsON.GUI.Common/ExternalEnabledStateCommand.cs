// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalEnabledStateCommand.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using System;
  using System.ComponentModel;
  using System.Windows.Input;
  using NLog;

  public abstract class ExternalEnabledStateCommand : ICommand
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private bool? canExecuteState;

    /// <summary>
    /// Called to check if command can be executed.
    /// </summary>
    public event CancelEventHandler CanExecuteExternal;

    /// <summary>
    /// Called when can execute state of the command has changed.
    /// </summary>
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Checks if command can be executed.
    /// </summary>
    /// <param name="parameter">Parameter.</param>
    /// <returns>True if it can be executd; false otherwise.</returns>
    public bool CanExecute(object parameter)
    {
      var result = this.CanExecuteInternal(parameter);
      if (this.canExecuteState.HasValue && this.canExecuteState != result)
      {
        this.canExecuteState = result;
        this.OnCanExecuteChanged(this, EventArgs.Empty);
      }

      this.canExecuteState = result;
      return result;
    }

    /// <summary>
    /// Handles command logic.
    /// </summary>
    /// <param name="parameter">Parameter.</param>
    public abstract void Execute(object parameter);

    /// <summary>
    /// Called when CanExecute has changed.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="args">Event args.</param>
    protected virtual void OnCanExecuteChanged(object sender, EventArgs args)
    {
      this.CanExecuteChanged?.Invoke(sender, args);
    }

    protected virtual bool CanExecuteInternal(object parameter)
    {
      var cancelEventArgs = new CancelEventArgs();
      this.CanExecuteExternal?.Invoke(this, cancelEventArgs);
      log.Debug("Command active = {value}", !cancelEventArgs.Cancel);
      return !cancelEventArgs.Cancel;
    }
  }
}