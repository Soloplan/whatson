// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowSettings.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.VisualConfig
{
  public class MainWindowSettings
  {
    public TreeListSettings TreeListSettings { get; set; }

    public WindowSettings MainWindowDimensions { get; set; }

    /// <summary>
    /// Gets or sets the window location.
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Gets or sets the window location.
    /// </summary>
    public double Top { get; set; }

    public bool Maximized { get; set; }

    public WindowSettings ConfigDialogSettings { get; set; }

    public WindowSettings WizardDialogSettings { get; set; } = new WindowSettings();

    public ColorSettings ColorSettings { get; set; }
  }
}