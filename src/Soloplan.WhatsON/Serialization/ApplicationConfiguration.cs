// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Serialization
{
  using System.Collections.Generic;

  /// <summary>
  /// Holds the serializable configuration.
  /// </summary>
  public class ApplicationConfiguration
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationConfiguration"/> class.
    /// </summary>
    public ApplicationConfiguration()
    {
      this.OpenMinimized = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// </summary>
    public bool DarkThemeEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether icon on taskbar should be shown.
    /// </summary>
    public bool ShowInTaskbar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether App is always on top.
    /// </summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether App is starts up minimized.
    /// </summary>
    public bool OpenMinimized { get; set; }

    /// <summary>
    /// Gets the subjects configuration.
    /// </summary>
    public IList<SubjectConfiguration> SubjectsConfiguration { get; } = new List<SubjectConfiguration>();
  }
}