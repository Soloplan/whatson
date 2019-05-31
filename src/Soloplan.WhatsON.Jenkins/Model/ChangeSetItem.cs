// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeSetItem.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//    Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using Newtonsoft.Json;

  public class ChangeSetItem
  {
    [JsonProperty("author")]
    public Culprit Author { get; set; }
  }
}