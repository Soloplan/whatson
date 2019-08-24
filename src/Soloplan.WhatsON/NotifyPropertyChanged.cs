// <copyright file="NotifyPropertyChanged.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON
{
  using System.ComponentModel;
  using System.Runtime.CompilerServices;

  public class NotifyPropertyChanged : INotifyPropertyChanged
  {
    /// <summary>
    /// Occurs when property is changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Called when property is changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
      this.PropertyChanged?.Invoke(sender, eventArgs);
    }
  }
}