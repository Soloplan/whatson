// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectConfigurationExtensions.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Linq;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// The extensions for <see cref="SubjectConfiguration"/> class.
  /// </summary>
  public static class SubjectConfigurationExtensions
  {
    /// <summary>
    /// Gets the notification configuration from the per connector settings or the global settings..
    /// </summary>
    /// <param name="applicationConfiguration">The application configuration.</param>
    /// <param name="subjectConfiguration">The subject configuration.</param>
    /// <returns>The notification configuration.</returns>
    public static NotificationConfiguration GetNotificationConfiguration(this ApplicationConfiguration applicationConfiguration, SubjectConfiguration subjectConfiguration)
    {
      // firstly check if the config item exists to prevent creation it "on access".
      var notificationVisibilityConfigItem = subjectConfiguration.ConfigurationItems.FirstOrDefault(ci => ci.Key == Subject.NotificationsVisbility);
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