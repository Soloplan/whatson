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

  public class OpenWebPageCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public event CancelEventHandler CanExecuteExternal;

    public bool CanExecute(object parameter)
    {
      if (parameter is OpenWebPageCommandData webPageParam && !string.IsNullOrEmpty(webPageParam.Address))
      {
        var cancelEventArgs = new CancelEventArgs();
        this.CanExecuteExternal?.Invoke(this, cancelEventArgs);
        return !cancelEventArgs.Cancel;
      }

      return false;
    }

    public void Execute(object parameter)
    {
      if (parameter is OpenWebPageCommandData webPageParam)
      {
        System.Diagnostics.Process.Start(webPageParam.FullAddress);
      }
    }
  }

  public class OpenWebPageCommandData
  {
    public string FullAddress => this.Address.TrimEnd('/') + (this.Redirect ? "/display/redirect" : string.Empty);

    public string Address { get; set; }

    public bool Redirect { get; set; }
  }
}