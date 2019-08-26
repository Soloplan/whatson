﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System;

  public class PluginViewModel
  {
    public string Name { get; set; }

    public Version Version { get; set; }

    public string Description { get; set; }
  }
}