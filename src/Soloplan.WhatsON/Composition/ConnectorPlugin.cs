// <copyright file="ConnectorPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Composition
{
  using System;
  using System.Reflection;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  public abstract class ConnectorPlugin : IConnectorPlugin
  {
    protected ConnectorPlugin(Type connectorType)
    {
      this.ConnectorType = connectorType;
      this.ConnectorTypeAttribute = this.ConnectorType.GetCustomAttribute<ConnectorTypeAttribute>();
    }

    public Type ConnectorType { get; }

    public ConnectorTypeAttribute ConnectorTypeAttribute { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin supports wizards.
    /// </summary>
    public virtual bool SupportsWizard { get; }

    public abstract Connector CreateNew(ConnectorConfiguration configuration);
  }
}
