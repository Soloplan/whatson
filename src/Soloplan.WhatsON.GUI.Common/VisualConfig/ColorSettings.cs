// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainColorSettings.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.VisualConfig
{
  using System.Windows.Media;

  public class ColorSettings
  {
    public ColorSettings()
    {
      this.Primary = new ColorSetting();
      this.Secondary = new ColorSetting();
    }

    public ColorSetting Primary { get; set; }

    public ColorSetting Secondary { get; set; }
  }

  public class ColorSetting
  {
    public byte Red { get; set; }

    public byte Green { get; set; }

    public byte Blue { get; set; }

    public Color GetColor()
    {
      return Color.FromRgb(this.Red, this.Green, this.Blue);
    }

    public void Apply(Color color)
    {
      this.Red = color.R;
      this.Green = color.G;
      this.Blue = color.B;
    }
  }
}
