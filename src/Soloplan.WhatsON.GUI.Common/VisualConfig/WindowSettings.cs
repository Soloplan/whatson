// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowSettings.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.VisualConfig
{
  using System.Windows;

  public class WindowSettings
  {
    public double Top { get; set; }

    public double Left { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public void Apply(Window window)
    {
      window.Top = this.Top;
      window.Left = this.Left;
      window.Width = this.Width;
      window.Height = this.Height;
    }

    public WindowSettings Parse(Window window)
    {
      this.Top = window.Top;
      this.Left = window.Left;
      this.Width = window.Width;
      this.Height = window.Height;
      return this;
    }
  }
}