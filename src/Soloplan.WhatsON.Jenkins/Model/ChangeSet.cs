// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeSet.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public class ChangeSet
  {
    [JsonProperty("items")]
    public IList<ChangeSetItem> ChangeSetItems { get; set; }
  }
}