// <copyright file="ConnectorPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Composition
{
  using System;
  using System.Collections.Generic;
  using System.Reflection;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Base implementation for all plugins that provide connectors.
  /// </summary>
  public abstract class ConnectorPlugin : IPlugin, IProjectPlugin
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

    public abstract void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string serverAddress);

    public abstract Connector CreateNew(ConnectorConfiguration configuration);

    public abstract Task<IList<Project>> GetProjects(string address);

    public override string ToString()
    {
      return $"{this.GetType().Name} ({this.Name}: {this.Description}";
    }
  }
}
