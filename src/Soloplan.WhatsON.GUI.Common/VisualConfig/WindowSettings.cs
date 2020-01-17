// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowSettings.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.VisualConfig
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows;

  public class WindowSettings
  {
    /// <summary>
    /// The delegate used to get additional settings values to apply them to window.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The setting value.</returns>
    public delegate string GetAdditionalSettingValueDelegate(string key, string defaultValue);

    /// <summary>
    /// The delegate used to set additional settings values based on window.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public delegate void SetAdditionalSettingValueDelegate(string key, string value);

    public double? Width { get; set; }

    public double? Height { get; set; }

    public IList<KeyValuePair<string, string>> AdditionalSettingsStore { get; set; }

    public void Apply(Window window)
    {
      if (this.Width.HasValue)
      {
        window.Width = this.Width.Value;
      }

      if (this.Height.HasValue)
      {
        window.Height = this.Height.Value;
      }

      if (window is IAdditionalWindowSettingsSupport additionalSettingsWindow)
      {
        additionalSettingsWindow.Apply(this.GetAdditionalSettingValue);
      }
    }

    public string GetAdditionalSettingValue(string key, string defaultValue)
    {
      if (this.AdditionalSettingsStore == null)
      {
        return defaultValue;
      }

      var records = this.AdditionalSettingsStore.Where(a => a.Key == key).ToList();
      return records.Count == 0 ? defaultValue : records.First().Value;
    }

    public void SetAdditionalSettingValue(string key, string value)
    {
      if (this.AdditionalSettingsStore == null)
      {
        this.AdditionalSettingsStore = new List<KeyValuePair<string, string>>();
      }

      var records = this.AdditionalSettingsStore.Where(a => a.Key == key).ToList();
      if (records.Count == 1)
      {
        this.AdditionalSettingsStore.Remove(records.First());
      }

      this.AdditionalSettingsStore.Add(new KeyValuePair<string, string>(key, value));
    }

    public WindowSettings Parse(Window window)
    {
      this.Width = window.Width;
      this.Height = window.Height;
      if (window is IAdditionalWindowSettingsSupport additionalSettingsWindow)
      {
        additionalSettingsWindow.Parse(this.SetAdditionalSettingValue);
      }

      return this;
    }
  }
}