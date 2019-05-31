// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeItemViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System.Windows.Input;

  public abstract class TreeItemViewModel : NotifyPropertyChanged, IHandleDoubleClick
  {
    private bool isNodeExpanded;

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

    public virtual void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
    }
  }
}