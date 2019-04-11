// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Culprit.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  /// <summary>
  /// Represents developer who committed changes which triggered build.
  /// </summary>
  public class Culprit
  {
    /// <summary>
    /// Gets or sets name of user how made modifications in this build.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets URL to users page.
    /// </summary>
    public string AbsoluteUrl { get; set; }
  }
}