// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsCulpritViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.Jenkins.Model;

  public class JenkinsCulpritViewModel : CulpritViewModel
  {
    private string absoluteUrl;

    /// <summary>
    /// Gets or sets URL to users page.
    /// </summary>
    public string AbsoluteUrl
    {
      get => this.absoluteUrl;
      set
      {
        this.absoluteUrl = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Initializes this object based on <paramref name="culprit"/>.
    /// </summary>
    /// <param name="culprit">Information about developer.</param>
    public void Init(Culprit culprit)
    {
      this.FullName = culprit.FullName;
      this.AbsoluteUrl = culprit.AbsoluteUrl;
    }
  }
}