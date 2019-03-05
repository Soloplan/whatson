// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Windows.Controls;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

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
    /// <param name="configItem">The configuration item of the subject.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="Control" /> for the <see cref="configItem" />.
    /// </returns>
    Control GetControl(ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute);
  }
}