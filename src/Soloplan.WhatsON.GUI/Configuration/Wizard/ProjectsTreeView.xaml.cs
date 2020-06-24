// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectsTreeView.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System.Diagnostics;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Navigation;

  /// <summary>
  /// Interaction logic for ProjectsTreeView.xaml.
  /// </summary>
  public partial class ProjectsTreeView : UserControl
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsTreeView"/> class.
    /// </summary>
    public ProjectsTreeView()
    {
      this.InitializeComponent();
    }

    /// <summary>
    /// Marks the <see cref="RoutedEventArgs"/> as handled.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void NullHandler(object sender, RoutedEventArgs e)
    {
      e.Handled = true;
    }

    /// <summary>
    /// Handles the PreviewKeyUp event of the TreeViewItem control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
    private void TreeViewItemPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == System.Windows.Input.Key.Space)
      {
        var item = (ProjectViewModel)this.mainTreeView.SelectedItem;
        if (item.IsCheckable)
        {
          item.IsChecked = !item.IsChecked;
          e.Handled = true;
        }
      }
    }

    /// <summary>
    /// Handles the hyperlink requiest event of elements in the TreeViewItem control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RequestNavigateEventArgs"/> instance containing the event data.</param>
    private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
      e.Handled = true;
    }

    /// <summary>
    /// Checks if all projects are checked.
    /// </summary>
    /// <returns>Bool = true if all projects are checked. </returns>
    private bool AreAllProjectsChecked()
    {
      foreach (var item in this.mainTreeView.Items)
      {
        var project = (ProjectViewModel)item;
        if (!project.IsChecked)
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Handles keydown in wizard projects tree view.
    /// </summary>
    /// <param name="sender">Sender item.</param>
    /// <param name="e">Event args.</param>
    private void ProjectsTreeViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.A))
      {
        if (!this.AreAllProjectsChecked())
        {
          foreach (var item in this.mainTreeView.Items)
          {
            var project = (ProjectViewModel)item;
            project.IsChecked = true;
          }
        }
        else
        {
          foreach (var item in this.mainTreeView.Items)
          {
            var project = (ProjectViewModel)item;
            project.IsChecked = false;
          }
        }
      }
    }
  }
}
