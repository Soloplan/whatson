// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationConfiguration.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Holds the serializable configuration.
  /// </summary>
  public class ApplicationConfiguration
  {
    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// </summary>
    public bool DarkThemeEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether icon on taskbar should be shown.
    /// </summary>
    public bool ShowInTaskbar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether App is always on top.
    /// </summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether App is starts up minimized.
    /// </summary>
    public bool OpenMinimized { get; set; }

    /// <summary>
    /// Gets or sets a view style, for now normal/compact are available and they only control the spacing of items.
    /// </summary>
    public ViewStyle ViewStyle { get; set; }

    /// <summary>
    /// Gets the connectors configuration.
    /// </summary>
    public IList<ConnectorConfiguration> ConnectorsConfiguration { get; } = new List<ConnectorConfiguration>();

    /// <summary>
    /// Gets the notification configuration.
    /// </summary>
    public NotificationConfiguration NotificationConfiguration { get; } = new NotificationConfiguration();

    /// <summary>
    /// Gets the notification configuration from the per connector settings or the global settings..
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The notification configuration.</returns>
    public NotificationConfiguration GetNotificationConfiguration(ConnectorConfiguration connectorConfiguration)
    {
      // firstly check if the config item exists to prevent creation it "on access".
      var notificationVisibilityConfigItem = connectorConfiguration.ConfigurationItems.FirstOrDefault(ci => ci.Key == Connector.NotificationsVisbility);
      if (notificationVisibilityConfigItem == null)
      {
        return this.NotificationConfiguration;
      }

      var connectorNotificationConfiguration = notificationVisibilityConfigItem.GetOrCreateConnectorNotificationConfiguration();
      return connectorNotificationConfiguration.UseGlobalNotificationSettings ?
        this.NotificationConfiguration :
        connectorNotificationConfiguration;
    }
  }
}