// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EditTreeItemViewModelEventArgs.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
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
    private object commandParameters;

    public EditType EditType { get; set; }
    public TreeItemViewModel Model { get; set; }
  }
}