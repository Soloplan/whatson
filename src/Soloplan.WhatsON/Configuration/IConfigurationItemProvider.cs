// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigurationItemsSupport.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  /// <summary>
  /// Gives support to access <see cref="IConfigurationItem"/>s.
  /// </summary>
  public interface IConfigurationItemProvider
  {
    /// <summary>
    /// Gets (and might also create if it does not exists) the configuration item by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration item.</returns>
    IConfigurationItem GetConfigurationByKey(string key);
  }
}