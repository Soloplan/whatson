// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpenJenkinsWebPageCommandData.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using Soloplan.WhatsON.GUI.Common;

  public class OpenJenkinsWebPageCommandData : OpenWebPageCommandData
  {
    public override string FullAddress => this.Address.TrimEnd('/') + (this.Redirect ? "/display/redirect" : string.Empty);

    public bool Redirect { get; set; }
  }
}