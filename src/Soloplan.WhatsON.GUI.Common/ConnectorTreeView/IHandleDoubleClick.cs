// <copyright file="IHandleDoubleClick.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System.Windows.Input;

  public interface IHandleDoubleClick
  {
    void OnDoubleClick(object sender, MouseButtonEventArgs e);
  }
}