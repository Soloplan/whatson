// <copyright file="ObservationState.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Model
{
  public enum ObservationState
  {
    Unknown = 0,
    Unstable = 1,
    Failure = 2,
    Success = 3,
    Running = 4,
  }
}
