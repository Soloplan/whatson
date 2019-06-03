// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildServerProjectStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows.Controls;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;
  using Soloplan.WhatsON.Jenkins.GUI;

  public abstract class BuildServerProjectStatusViewModel : ConnectorViewModel
  {
    /// <summary>
    /// Gets command for opening builds webPage.
    /// </summary>
    public virtual OpenWebPageCommand OpenWebPage { get; } = new OpenWebPageCommand();

    public abstract OpenWebPageCommandData OpenWebPageParam { get; set; }

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      base.OnDoubleClick(sender, e);
      var treeViewItem = sender as TreeViewItem;
      if (treeViewItem != null && treeViewItem.DataContext == this && this.OpenWebPage.CanExecute(this.OpenWebPageParam))
      {
        this.OpenWebPage.Execute(this.OpenWebPageParam);
      }
    }
  }
}