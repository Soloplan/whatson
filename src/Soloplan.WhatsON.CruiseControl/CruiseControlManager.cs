// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlManager.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;

  public static class CruiseControlManager
  {
    private static readonly IDictionary<Uri, CruiseControlServer> servers = new Dictionary<Uri, CruiseControlServer>();

    public static CruiseControlServer GetServer(string address, bool addToCache = true)
    {
      var uri = new Uri(address);
      if (servers.TryGetValue(uri, out CruiseControlServer server))
      {
        return server;
      }

      server = new CruiseControlServer(uri.AbsoluteUri);
      if (addToCache)
      {
        if (server != null)
        {
          servers[uri] = server;
        }
      }

      return server;
    }
  }
}