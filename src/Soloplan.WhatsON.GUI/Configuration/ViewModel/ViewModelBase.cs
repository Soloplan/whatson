// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.ViewModel
{
  using System.ComponentModel;
  using PropertyChanged;

  public abstract partial class ViewModelBase : INotifyPropertyChanged
  {
    /// <summary>
    /// Gets or sets a value indicating whether the loaded flag is set.
    /// </summary>
    [DoNotNotify]
    public bool IsLoaded { get; set; }
  }
}
