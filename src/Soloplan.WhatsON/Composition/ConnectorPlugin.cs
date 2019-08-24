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
      var connectorTypeAttribute = this.ConnectorType.GetCustomAttribute<ConnectorTypeAttribute>();
      if (connectorTypeAttribute == null)
      {
        throw new ArgumentException($"The connector type {this.ConnectorType} doesn't provide meta information. Make sure a ConnectorTypeAttribute is defined on the type.");
      }

      this.Name = connectorTypeAttribute.Name;
      this.Description = connectorTypeAttribute.Description;
    }

    public Type ConnectorType { get; }

    public string Name { get; }

    public string Description { get; }

    public abstract Connector CreateNew(ConnectorConfiguration configuration);

    public override string ToString()
    {
      return $"{this.GetType().Name} ({this.Name}: {this.Description}";
    }
  }
}
