// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAdditionalWindowSettingsSupport.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using System.Windows;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;

  /// <summary>
  /// Adds support for storing additional window settings values.
  /// </summary>
  public interface IAdditionalWindowSettingsSupport
  {
    /// <summary>
    /// Applies the additional window settings to the <see cref="Window"/> instance.
    /// </summary>
    /// <param name="getAdditionalSettingValue">The get additional setting value.</param>
    void Apply(WindowSettings.GetAdditionalSettingValueDelegate getAdditionalSettingValue);

    /// <summary>
    /// Parses the specified additional setting values from <see cref="Window"/> instance on which the interface is implemented.
    /// </summary>
    /// <param name="setAdditionalSettingValue">The set additional setting value.</param>
    void Parse(WindowSettings.SetAdditionalSettingValueDelegate setAdditionalSettingValue);
  }
}