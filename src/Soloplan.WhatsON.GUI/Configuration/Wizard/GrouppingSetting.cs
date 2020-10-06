// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GrouppingSetting.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
//  Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  /// <summary>
  /// The grouping settings item.
  /// </summary>
  public class GrouppingSetting
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="GrouppingSetting"/> class.
    /// </summary>
    /// <param name="caption">The caption.</param>
    /// <param name="id">The identifier.</param>
    public GrouppingSetting(string caption, string id)
    {
      this.Caption = caption;
      this.Id = id;
    }

    /// <summary>
    /// Gets the caption.
    /// </summary>
    public string Caption { get; }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public string Id { get; }
  }
}