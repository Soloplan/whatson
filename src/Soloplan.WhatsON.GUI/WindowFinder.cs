// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowFinder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Drawing;
  using System.Runtime.InteropServices;
  using System.Windows.Interop;

  /// <summary>
  /// Class for checking whether the application window is topmost.
  /// </summary>
  /// <remarks>
  /// From https://stackoverflow.com/q/454792.
  /// </remarks>
  public static class WindowFinder
  {
    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(Point lpPoint);

    public static bool IsWindowVisible(System.Windows.Window window)
    {
      WindowInteropHelper win = new WindowInteropHelper(window);
      int x = (int)(window.Left + (window.Width / 2));
      int y = (int)(window.Top + (window.Height / 2));
      Point p = new Point(x, y);
      return win.Handle == WindowFromPoint(p);
    }
  }
}