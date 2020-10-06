// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeHelper.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Windows.Media;
  using MaterialDesignThemes.Wpf;
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
      var baseTheme = isDark ? Theme.Dark : Theme.Light;
      ITheme theme = isDark ? Theme.Create(baseTheme, PrimaryColor, Color.FromRgb(195, 0, 107)) : Theme.Create(baseTheme, SecondaryColor, Color.FromRgb(195, 0, 107));
      var paletteHelper = new PaletteHelper();
      paletteHelper.SetTheme(theme);
    }
  }
}