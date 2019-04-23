// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CulpritViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  public class CulpritViewModel : NotifyPropertyChanged
  {
    private string fullName;

    /// <summary>
    /// Gets or sets name of user how made modifications in this build.
    /// </summary>
    public string FullName
    {
      get => this.fullName;
      set
      {
        this.fullName = value;
        this.OnPropertyChanged();
      }
    }
  }
}