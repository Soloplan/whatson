// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using System.Windows;
  using System.Windows.Media;
  using MaterialDesignColors;
  using MaterialDesignThemes.Wpf;

  /// <summary>
  /// Helper class for managing themes and color adjustments to the UI.
  /// </summary>
  internal class ThemeHelper
  {
    /// <summary>
    /// Initializes this instance.
    /// </summary>
    public void Initialize()
    {
      this.ApplySoloplanThemeColors();
    }

    /// <summary>
    /// Applies the light or dark mode.
    /// </summary>
    /// <param name="isDark">if set to <c>true</c> dark mode will be applied.</param>
    public void ApplyLightDarkMode(bool isDark)
    {
      var resourceDictionary = Application.Current.Resources.MergedDictionaries.Where(rd => rd.Source != (Uri)null).SingleOrDefault(rd => Regex.Match(rd.Source.OriginalString, "(\\/MaterialDesignThemes.Wpf;component\\/Themes\\/MaterialDesignTheme\\.)((Light)|(Dark))").Success);
      if (resourceDictionary == null)
      {
        throw new ApplicationException("Unable to find Light/Dark base theme in Application resources.");
      }

      resourceDictionary["MaterialDesignPaper"] = new SolidColorBrush(isDark ? Color.FromRgb(45, 45, 48) : Color.FromRgb(249, 249, 249));
      var paletteHelper = new PaletteHelper();
      paletteHelper.SetLightDark(isDark);
    }

    /// <summary>
    /// Applies our favorite colors to current palette.
    /// </summary>
    private void ApplySoloplanThemeColors()
    {
      var paletteHelper = new PaletteHelper();

      var newPrimaryHues = new List<Hue>();
      newPrimaryHues.Add(new Hue("Primary50", Color.FromRgb(247, 224, 237), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary100", Color.FromRgb(236, 179, 211), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary200", Color.FromRgb(224, 128, 181), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary300", Color.FromRgb(211, 77, 151), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary400", Color.FromRgb(201, 38, 129), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary500", Color.FromRgb(192, 0, 107), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary600", Color.FromRgb(186, 0, 99), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary700", Color.FromRgb(178, 0, 88), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary800", Color.FromRgb(170, 0, 78), Color.FromRgb(255, 255, 255)));
      newPrimaryHues.Add(new Hue("Primary900", Color.FromRgb(156, 0, 60), Color.FromRgb(255, 255, 255)));

      var newAccentHues = new List<Hue>();
      newAccentHues.Add(new Hue("Accent100", Color.FromRgb(255, 199, 216), Color.FromRgb(255, 255, 255)));
      newAccentHues.Add(new Hue("Accent200", Color.FromRgb(255, 148, 181), Color.FromRgb(255, 255, 255)));
      newAccentHues.Add(new Hue("Accent400", Color.FromRgb(255, 97, 145), Color.FromRgb(255, 255, 255)));
      newAccentHues.Add(new Hue("Accent700", Color.FromRgb(255, 71, 127), Color.FromRgb(255, 255, 255)));

      var swatch = new Swatch("Soloplan WhatsOn", newPrimaryHues, newAccentHues);
      var palette = new Palette(swatch, swatch, 3, 5, 4, 2);
      paletteHelper.ReplacePalette(palette);
    }
  }
}