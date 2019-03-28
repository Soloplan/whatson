// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  public abstract class ViewModelBase : NotifyPropertyChanged
  {
    /// <summary>
    /// Gets or sets a value indicating whether the loaded flag is set.
    /// </summary>
    public bool IsLoaded { get; set; }
  }
}