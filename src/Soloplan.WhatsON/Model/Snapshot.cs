// <copyright file="Snapshot.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Model
{
  using System;

  public class Snapshot
  {
    public Snapshot(Status status)
    {
      this.Status = status;
    }

    public string Name { get; set; }

    public Status Status { get; }

    public long Age
    {
      get
      {
        if (this.Status == null)
        {
          return 0;
        }

        return (long)(DateTime.Now - this.Status.Time).TotalSeconds;
      }
    }
  }
}
