// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EditTreeItemCommand.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;

  /// <summary>
  /// Command which fires event when it is executed.
  /// </summary>
  public class CustomCommand : ExternalEnabledStateCommand
  {
    /// <summary>
    /// Subscribe for whatever logic is required.
    /// </summary>
    public event EventHandler<ValueEventArgs<object>> OnExecute;

    /// <summary>
    /// Handles command execution. Calls <see cref="OnExecute"/>.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    public override void Execute(object parameter)
    {
      if (this.CanExecute(parameter))
      {
        this.OnExecute?.Invoke(this, new ValueEventArgs<object>(parameter));
      }
    }
  }
}