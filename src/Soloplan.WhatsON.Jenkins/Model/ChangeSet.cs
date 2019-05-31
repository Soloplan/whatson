// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ChangeSet.cs" company="Soloplan GmbH">
// //   Copyright (c) Soloplan GmbH. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

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