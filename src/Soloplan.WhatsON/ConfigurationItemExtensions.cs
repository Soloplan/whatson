// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItemExtensions.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using Newtonsoft.Json;

  /// <summary>
  /// The extensions to <see cref="ConfigurationItem"/> and <see cref="IConfigurationItem"/>.
  /// </summary>
  public static class ConfigurationItemExtensions
  {
    /// <summary>
    /// Gets (or creates) <see cref="ConnectorNotificationConfiguration"/> based on <see cref="IConfigurationItem"/> using deserialization.
    /// </summary>
    /// <param name="configItem">The configuration item.</param>
    /// <returns>The deserialized instance of <see cref="ConnectorNotificationConfiguration"/> or a new instance if it does not exists.</returns>
    public static ConnectorNotificationConfiguration GetOrCreateConnectorNotificationConfiguration(this IConfigurationItem configItem)
    {
      if (string.IsNullOrWhiteSpace(configItem.Value))
      {
        return new ConnectorNotificationConfiguration();
      }

      try
      {
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        return JsonConvert.DeserializeObject<ConnectorNotificationConfiguration>(configItem.Value, settings);
      }
      catch
      {
        return new ConnectorNotificationConfiguration();
      }
    }
  }
}