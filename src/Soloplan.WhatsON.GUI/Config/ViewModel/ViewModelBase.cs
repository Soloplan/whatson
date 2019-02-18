// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System;

  public abstract class ViewModelBase
  {
    /// <summary>
    /// Gets or sets a value indicating whether the loaded flag is set.
    /// </summary>
    protected bool IsLoaded { get; set; }

    protected T CheckIsLoadedAndGetValue<T>(Func<T> getValue)
    {
      if (!this.IsLoaded)
      {
        throw new InvalidOperationException("The ConfigViewModel was not correctly initialized.");
      }

      return getValue();
    }
  }
}