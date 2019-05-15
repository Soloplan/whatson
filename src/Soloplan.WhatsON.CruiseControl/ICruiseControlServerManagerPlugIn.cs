// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICruiseControlServerManagerPlugIn.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  public interface ICruiseControlServerManagerPlugIn : IPlugIn
  {
    CruiseControlServer GetServer(string address, bool addToCache = true);
  }
}