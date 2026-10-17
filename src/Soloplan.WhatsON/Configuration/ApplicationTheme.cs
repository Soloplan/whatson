// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationTheme.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  [JsonConverter(typeof(StringEnumConverter))]
  public enum ApplicationTheme
  {
    System,
    Light,
    Dark,
  }
}
