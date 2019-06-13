// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalEnabledStateCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using System;
  using System.ComponentModel;
  using System.Windows.Input;
  using NLog;

  public abstract class ExternalEnabledStateCommand :ICommand
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

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
    public virtual bool CanExecute(object parameter)
    {
      var cancelEventArgs = new CancelEventArgs();
      this.CanExecuteExternal?.Invoke(this, cancelEventArgs);
      log.Debug("Command active = {value}", !cancelEventArgs.Cancel);
      return !cancelEventArgs.Cancel;
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
  }
}