// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlManager.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Concurrent;

  public static class CruiseControlManager
  {
    private static readonly ConcurrentDictionary<Uri, CruiseControlServer> servers = new ConcurrentDictionary<Uri, CruiseControlServer>();

    public static CruiseControlServer GetServer(string address, bool addToCache = true)
    {
      var uri = new Uri(address);
      if (servers.TryGetValue(uri, out CruiseControlServer server))
      {
        return server;
      }

      return addToCache
        ? servers.GetOrAdd(uri, key => new CruiseControlServer(key.AbsoluteUri))
        : new CruiseControlServer(uri.AbsoluteUri);
    }
  }
}
