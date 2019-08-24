// <copyright file="IConnectorPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Composition
{
  using System;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  public interface IConnectorPlugin : IPlugIn
  {
    Type ConnectorType { get; }

    ConnectorTypeAttribute ConnectorTypeAttribute { get; }

    Connector CreateNew(ConnectorConfiguration configuration);
  }
}