// <copyright file="ConnectorTypeAttribute.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Composition
{
  using System;

  [AttributeUsage(AttributeTargets.Class)]
  public class ConnectorTypeAttribute : Attribute
  {
    public ConnectorTypeAttribute(string name)
    {
      this.Name = name;
    }

    public ConnectorTypeAttribute(string name, string displayName)
    {
      this.Name = name;
      this.DisplayName = displayName;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string Description { get; set; }
  }
}
