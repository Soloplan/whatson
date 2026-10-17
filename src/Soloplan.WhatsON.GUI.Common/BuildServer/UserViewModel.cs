// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.ComponentModel;

  public partial class UserViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// Gets or sets name of user how made modifications in this build.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets URL to user.
    /// </summary>
    public string? Url { get; set; }
  }
}
