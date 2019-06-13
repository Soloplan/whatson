// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeItemViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Windows.Input;

  public abstract class TreeItemViewModel : NotifyPropertyChanged, IHandleDoubleClick
  {
    private bool isNodeExpanded;

    /// <summary>
    /// Backing field for <see cref="EditCommand"/>.
    /// </summary>
    private CustomCommand editCommand;

    /// <summary>
    /// Event fired when user requested editing of tree view item in context menu.
    /// </summary>
    public event EventHandler<ValueEventArgs<TreeItemViewModel>> EditItem;

    public bool IsNodeExpanded
    {
      get => this.isNodeExpanded;
      set
      {
        if (this.isNodeExpanded != value)
        {
          this.isNodeExpanded = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Gets command for editing tree item.
    /// </summary>
    public virtual CustomCommand EditCommand => this.editCommand ?? (this.editCommand = this.CreateEditCommand());

    /// <summary>
    /// Handles double click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    public virtual void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// Called when item is edited.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="eventArgs">Arguments containing data about edited item.</param>
    protected virtual void OnEditItem(object sender, ValueEventArgs<TreeItemViewModel> eventArgs)
    {
      this.EditItem?.Invoke(sender, eventArgs);
    }

    /// <summary>
    /// Creates command used for editing this tree item.
    /// </summary>
    /// <returns>Command used to edit tree itme.</returns>
    protected virtual CustomCommand CreateEditCommand()
    {
      var command = new CustomCommand();
      command.OnExecute += (s, e) => this.OnEditItem(this, new ValueEventArgs<TreeItemViewModel>(this));
      return command;
    }
  }
}