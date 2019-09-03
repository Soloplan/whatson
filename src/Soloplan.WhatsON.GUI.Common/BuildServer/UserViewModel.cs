// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  public class UserViewModel : NotifyPropertyChanged
  {
    private string fullName;
    private string url;

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

    /// <summary>
    /// Gets or sets URL to user.
    /// </summary>
    public string Url
    {
      get => this.url;
      set
      {
        this.url = value;
        this.OnPropertyChanged();
      }
    }
  }
}