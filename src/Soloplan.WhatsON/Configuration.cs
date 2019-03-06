// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItem.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Collections.Generic;

  /// <summary>
  /// The application configuration.
  /// </summary>
  public class Configuration
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Configuration"/> class.
    /// </summary>
    public Configuration()
    {
      this.Subjects = new List<Subject>();
    }

    /// <summary>
    /// Gets the subjects.
    /// </summary>
    public IList<Subject> Subjects { get; }

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// </summary>
    public bool DarkThemeEnabled { get; set; }
  }
}
