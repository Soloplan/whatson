// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  internal class TestConnector : Connector
  {
    public TestConnector()
      : base(new ConnectorConfiguration("Test", "Test project"))
    {
    }

    public Func<CancellationToken, Task<Status?>> Query { get; set; } = token => Task.FromResult<Status?>(null);

    public List<Status?> History { get; } = new List<Status?>();

    public int HistoryRequests { get; private set; }

    protected override Task<Status?> GetCurrentStatus(CancellationToken cancellationToken) => this.Query(cancellationToken);

    protected override Task<List<Status?>> GetHistory(CancellationToken cancellationToken)
    {
      this.HistoryRequests++;
      return Task.FromResult(this.History);
    }
  }
}
