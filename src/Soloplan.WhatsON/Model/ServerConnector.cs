// <copyright file="ServerConnector.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Model
{
  using Soloplan.WhatsON.Configuration;

  [ConfigurationItem(ServerAddress, typeof(string), Optional = false, Priority = 100)]
  public abstract class ServerConnector : Connector
  {
    public const string ServerAddress = "Address";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerConnector"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    protected ServerConnector(ConnectorConfiguration configuration)
      : base(configuration)
    {
    }

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address
    {
      get => this.ConnectorConfiguration.GetConfigurationByKey(ServerAddress).Value;
      set => this.ConnectorConfiguration.GetConfigurationByKey(ServerAddress).Value = value;
    }
  }
}
