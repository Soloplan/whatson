﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Windows;
  using Soloplan.WhatsON.Configuration;

  /// <summary>
  /// The interface for every configuration control builder.
  /// </summary>
  public interface IConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    string SupportedConfigurationItemsKey { get; }

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the connector.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    FrameworkElement GetControl(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute);
  }
}