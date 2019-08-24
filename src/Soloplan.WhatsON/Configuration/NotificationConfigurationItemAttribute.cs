// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationConfigurationItemAttribute.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;

  /// <summary>
  /// Used for marking notification configuration items for connectors.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class NotificationConfigurationItemAttribute : ConfigurationItemAttribute
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfigurationItemAttribute"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    public NotificationConfigurationItemAttribute(string key, Type type)
      : base(key, type)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether connector supports failure notifications.
    /// </summary>
    public bool SupportsFailureNotify { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether connector supports unstable notifications.
    /// </summary>
    public bool SupportsUnstableNotify { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether connector supports success notifications.
    /// </summary>
    public bool SupportsSuccessNotify { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether connector supports running notifications.
    /// </summary>
    public bool SupportsRunningNotify { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether connector supports unknown notifications.
    /// </summary>
    public bool SupportsUnknownNotify { get; set; } = true;
  }
}