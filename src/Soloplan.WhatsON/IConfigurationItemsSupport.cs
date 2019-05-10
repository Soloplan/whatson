// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigurationItemsSupport.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  /// <summary>
  /// Gives support to access <see cref="IConfigurationItem"/>s.
  /// </summary>
  public interface IConfigurationItemsSupport
  {
    /// <summary>
    /// Gets (and might also create if it does not exists) the configuration item by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration item.</returns>
    IConfigurationItem GetConfigurationByKey(string key);
  }
}