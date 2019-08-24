// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigurationItem.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  /// <summary>
  /// The configuration item.
  /// </summary>
  public interface IConfigurationItem
  {
    /// <summary>
    /// Gets the key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    string Value { get; set; }
  }
}