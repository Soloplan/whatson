// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorNotificationConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  /// <summary>
  /// The notification configuration used for connectors.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.NotificationConfiguration" />
  public class ConnectorNotificationConfiguration : NotificationConfiguration
  {
    /// <summary>
    /// Gets or sets a value indicating whether global settings should be used.
    /// </summary>
    public bool UseGlobalNotificationSettings { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether notify only if status changed.
    /// </summary>
    public bool OnlyIfStatusChanged { get; set; } = false;
  }
}