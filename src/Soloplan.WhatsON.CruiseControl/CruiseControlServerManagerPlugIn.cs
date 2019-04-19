// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlServerManagerPlugIn.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;

  public class CruiseControlServerManagerPlugIn : ICruiseControlServerManagerPlugIn
  {
    private IDictionary<Uri, CruiseControlServer> servers = new Dictionary<Uri, CruiseControlServer>();

    public CruiseControlServer GetServer(string address)
    {
      var uri = new Uri(address);
      CruiseControlServer server;
      if (this.servers.TryGetValue(uri, out server))
      {
        return server;
      }

      server = new CruiseControlServer(uri.AbsoluteUri);
      this.servers[uri] = server;
      return server;
    }
  }
}