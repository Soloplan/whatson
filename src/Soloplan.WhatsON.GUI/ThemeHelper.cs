// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.IO;
  using System.Security;
  using System.Windows;
  using System.Windows.Media;
  using MaterialDesignThemes.Wpf;
  using Microsoft.Win32;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;

  /// <summary>
  /// Helper class for managing themes and color adjustments to the UI.
  /// </summary>
  internal class ThemeHelper
  {
    static ThemeHelper()
    {
      PrimaryColor = Color.FromRgb(79, 103, 121);
      SecondaryColor = Color.FromRgb(79, 103, 121);
    }

    public static Color PrimaryColor { get; private set; }

    public static Color SecondaryColor { get; private set; }

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    public void Initialize(ColorSettings settings)
    {
      if (settings != null)
      {
        PrimaryColor = settings.Primary.GetColor();
        SecondaryColor = settings.Secondary.GetColor();
      }
    }

    /// <summary>
    /// Applies the light or dark mode.
    /// </summary>
    /// <param name="isDark">if set to <c>true</c> dark mode will be applied.</param>
    public void ApplyLightDarkMode(bool isDark)
    {
      System.Windows.Forms.Application.SetColorMode(
        isDark ? System.Windows.Forms.SystemColorMode.Dark : System.Windows.Forms.SystemColorMode.Classic);
      var baseTheme = isDark ? Theme.Dark : Theme.Light;
      ITheme theme = Theme.Create(baseTheme, isDark ? PrimaryColor : SecondaryColor, Color.FromRgb(49, 95, 200));
      var paletteHelper = new PaletteHelper();
      paletteHelper.SetTheme(theme);
      var resources = Application.Current.Resources;
      resources["SettingsBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#111827" : "#F4F6FA"));
      resources["SettingsSurface"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#1B2536" : "#FFFFFF"));
      resources["SettingsBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#344155" : "#DFE5EF"));
      resources["SettingsMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#B4C1D2" : "#5C6B7E"));
      resources["SettingsAccent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#A3C1FF" : "#315FC8"));
      resources["SettingsSelection"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#233A61" : "#EAF0FE"));
    }

    internal static bool IsDarkMode(ApplicationTheme mode, bool systemIsDark) => mode == ApplicationTheme.Dark || (mode == ApplicationTheme.System && systemIsDark);

    internal static bool SystemUsesDarkMode()
    {
      try
      {
        return Registry.GetValue(
          @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
          "AppsUseLightTheme",
          1) is int value && value == 0;
      }
      catch (Exception exception) when (exception is SecurityException || exception is UnauthorizedAccessException || exception is IOException)
      {
        return false;
      }
    }
  }
}
