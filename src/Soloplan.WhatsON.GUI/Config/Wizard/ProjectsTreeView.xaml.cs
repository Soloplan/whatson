// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectsTreeView.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.Wizard
{
  using System.Windows;
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for ProjectsTreeView.xaml
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
  }
}
