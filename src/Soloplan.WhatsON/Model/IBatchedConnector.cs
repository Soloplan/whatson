// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBatchedConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Model
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;

  /// <summary>
  /// Polls connectors on one server together and returns the connectors whose status was updated.
  /// </summary>
  public interface IBatchedConnector
  {
    Uri? BatchAddress { get; }

    Task<IReadOnlyCollection<Connector>> QueryStatusBatchAsync(IReadOnlyCollection<Connector> connectors, CancellationToken token);
  }
}
