// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorConfigurationExtensions.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Linq;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// The extensions for <see cref="ConnectorConfiguration"/> class.
  /// </summary>
  public static class ConnectorConfigurationExtensions
  {
    /// <summary>
    /// Gets the notification configuration from the per connector settings or the global settings..
    /// </summary>
    /// <param name="applicationConfiguration">The application configuration.</param>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The notification configuration.</returns>
    public static NotificationConfiguration GetNotificationConfiguration(this ApplicationConfiguration applicationConfiguration, ConnectorConfiguration connectorConfiguration)
    {
      // firstly check if the config item exists to prevent creation it "on access".
      var notificationVisibilityConfigItem = connectorConfiguration.ConfigurationItems.FirstOrDefault(ci => ci.Key == Connector.NotificationsVisbility);
      if (notificationVisibilityConfigItem == null)
      {
        return applicationConfiguration.NotificationConfiguration;
      }

      var connectorNotificationConfiguration = notificationVisibilityConfigItem.GetOrCreateConnectorNotificationConfiguration();
      return connectorNotificationConfiguration.UseGlobalNotificationSettings ?
        applicationConfiguration.NotificationConfiguration :
        connectorNotificationConfiguration;
    }
  }
}