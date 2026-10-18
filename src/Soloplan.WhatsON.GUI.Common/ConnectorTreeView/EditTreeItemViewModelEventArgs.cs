// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EditTreeItemViewModelEventArgs.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;

  public enum EditType
  {
    Undefined = 0,

    Edit = 1,

    Rename = 2,
  }

  public class EditTreeItemViewModelEventArgs : EventArgs
  {
    public EditType EditType { get; set; }

    public required TreeItemViewModel Model { get; set; }
  }
}
