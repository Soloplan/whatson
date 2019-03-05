// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System.ComponentModel;
  using System.Runtime.CompilerServices;

  public abstract class ViewModelBase : INotifyPropertyChanged
  {
    /// <summary>
    /// Occurs when property is changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the loaded flag is set.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Called when property is changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}