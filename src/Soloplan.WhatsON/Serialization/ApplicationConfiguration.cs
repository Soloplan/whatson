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
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// </summary>
    public bool DarkThemeEnabled { get; set; }

    /// <summary>
    /// Gets the subjects configuration.
    /// </summary>
    public IList<SubjectConfiguration> SubjectsConfiguration { get; } = new List<SubjectConfiguration>();
  }
}