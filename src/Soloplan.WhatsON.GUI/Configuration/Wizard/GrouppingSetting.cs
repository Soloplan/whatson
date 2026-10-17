// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GrouppingSetting.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.ComponentModel;
  using Soloplan.WhatsON.GUI.Common.Localization;

  /// <summary>
  /// The grouping settings item.
  /// </summary>
  public partial class GrouppingSetting : INotifyPropertyChanged
  {
    private readonly Func<string> caption;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrouppingSetting" /> class.
    /// </summary>
    /// <param name="caption">The caption.</param>
    /// <param name="id">The identifier.</param>
    public GrouppingSetting(Func<string> caption, string id)
    {
      this.caption = caption;
      this.Id = id;
      PropertyChangedEventManager.AddHandler(LanguageSource.Instance, this.LanguageChanged, nameof(LanguageSource.Culture));
    }

    /// <summary>
    /// Gets the caption.
    /// </summary>
    public string Caption => this.caption();

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public string Id { get; }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args) => this.OnPropertyChanged(nameof(this.Caption));
  }
}
