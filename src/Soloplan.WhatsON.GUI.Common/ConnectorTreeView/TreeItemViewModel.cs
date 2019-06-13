// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeItemViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using System.Windows.Input;

  public abstract class TreeItemViewModel : NotifyPropertyChanged, IHandleDoubleClick
  {
    private bool isNodeExpanded;

    /// <summary>
    /// Backing field for <see cref="EditCommand"/>.
    /// </summary>
    private CustomCommand editCommand;

    private CustomCommand deleteCommand;

    /// <summary>
    /// Event fired when user requested editing of tree view item in context menu.
    /// </summary>
    public event EventHandler<ValueEventArgs<TreeItemViewModel>> EditItem;

    /// <summary>
    /// Event fired when item should be deleted.
    /// </summary>
    public event EventHandler<DeleteTreeItemEventArgs> DeleteItem;

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

    public virtual CustomCommand DeleteCommand => this.deleteCommand ?? (this.deleteCommand = this.CreateDeleteCommand());

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
    /// Called when item is deleted.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="eventArgs">Arguments containing data about deleted item.</param>
    protected virtual void OnDeleteItem(object sender, DeleteTreeItemEventArgs eventArgs)
    {
      this.DeleteItem?.Invoke(sender, eventArgs);
    }

    /// <summary>
    /// Creates command used for editing this tree item.
    /// </summary>
    /// <returns>Command used to edit tree item.</returns>
    protected virtual CustomCommand CreateEditCommand()
    {
      var command = new CustomCommand();
      command.OnExecute += (s, e) => this.OnEditItem(this, new ValueEventArgs<TreeItemViewModel>(this));
      return command;
    }

    /// <summary>
    /// Creates command used for deleting this tree item.
    /// </summary>
    /// <returns>Command used to delete tree item.</returns>
    protected virtual CustomCommand CreateDeleteCommand()
    {
      var command = new CustomCommand();
      command.OnExecute += this.DeleteCommandOnExecute;
      return command;
    }

    /// <summary>
    /// Handles delete command.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void DeleteCommandOnExecute(object sender, ValueEventArgs<object> e)
    {
      var data = new DeleteTreeItemEventArgs(this);
      this.OnDeleteItem(this, data);
    }
  }

  /// <summary>
  /// Data related to deleting <see cref="TreeItemViewModel"/>.
  /// </summary>
  public class DeleteTreeItemEventArgs : EventArgs
  {
    /// <summary>
    /// Prevents re-using class for multiple event calls.
    /// </summary>
    private bool used;

    /// <summary>
    /// List of event handlers.
    /// </summary>
    private readonly IList<Func<Task<bool>>> canceledCalculationFunctions = new List<Func<Task<bool>>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTreeItemEventArgs"/> class.
    /// </summary>
    /// <param name="deleteItem">The item about to be deleted.</param>
    public DeleteTreeItemEventArgs(TreeItemViewModel deleteItem)
    {
      this.DeleteItem = deleteItem;
    }

    /// <summary>
    /// Gets the item about to be deleted.
    /// </summary>
    public TreeItemViewModel DeleteItem { get; }

    /// <summary>
    /// Adds function checking for canceled state.
    /// </summary>
    /// <param name="cancel">Function checking canceled state.</param>
    public void AddAsyncCancelCheckAction(Func<Task<bool>> cancel)
    {
      if (this.used)
      {
        throw new InvalidOperationException("Canceled state was already calculated. Can't change list of functions now");
      }

      this.canceledCalculationFunctions.Add(cancel);
    }

    public async Task<bool> CheckCanceled()
    {
      this.used = true;
      foreach (var task in this.canceledCalculationFunctions)
      {
        if (await task())
        {
          return true;
        }
      }

      return false;
    }
  }
}