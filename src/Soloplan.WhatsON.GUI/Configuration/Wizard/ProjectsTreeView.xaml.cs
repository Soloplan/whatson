// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectsTreeView.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Diagnostics;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;
  using TheArtOfDev.HtmlRenderer.Core.Entities;
  using TheArtOfDev.HtmlRenderer.WPF;

  public partial class ProjectsTreeView : UserControl
  {
    public ProjectsTreeView()
    {
      this.InitializeComponent();
    }

    private static ListBox? FindProjectList(DependencyObject? element)
    {
      while (element != null)
      {
        if (element is ListBox list)
        {
          return list;
        }

        element = VisualTreeHelper.GetParent(element);
      }

      return null;
    }

    private void OpenFolderClick(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement { DataContext: ProjectViewModel { IsFolder: true } folder } &&
          FindProjectList(sender as DependencyObject)?.DataContext is ProjectBrowserColumn column)
      {
        column.SelectedProject = folder;
      }

      e.Handled = true;
    }

    private void DescriptionImageLoad(object sender, RoutedEventArgs<HtmlImageLoadEventArgs> e)
    {
      // Server descriptions can contain SVG badges or invalid image data that WPF cannot decode.
      e.Data.Callback();
    }

    private void ColumnLoaded(object sender, RoutedEventArgs e) => ((FrameworkElement)sender).BringIntoView();

    private void ColumnsSizeChanged(object sender, SizeChangedEventArgs e)
    {
      var width = Math.Max(230, this.ColumnsScrollViewer.ViewportWidth / Math.Min(Math.Max(1, this.ColumnsPanel.Items.Count), 2));
      if (double.IsFinite(width) && Math.Abs(width - (double)this.Resources["ProjectColumnWidth"]) > 0.5)
      {
        this.Resources["ProjectColumnWidth"] = width;
      }
    }

    private void OpenProjectClick(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement { DataContext: ProjectViewModel project } &&
          Uri.TryCreate(project.DirectAddress, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
      {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
      }

      e.Handled = true;
    }

    private void ProjectsPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter && e.OriginalSource is ListBoxItem { DataContext: ProjectViewModel { IsFolder: true } folder } row &&
          FindProjectList(row)?.DataContext is ProjectBrowserColumn column)
      {
        column.SelectedProject = folder;
        e.Handled = true;
      }
      else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control &&
          this.DataContext is ProjectBrowserViewModel { MultiSelectionMode: true } browser)
      {
        browser.AllSelectionState = browser.AllSelectionState != true;
        e.Handled = true;
      }
      else if (e.Key == Key.Space && e.OriginalSource is ListBoxItem { DataContext: ProjectViewModel item })
      {
        item.IsChecked = !item.IsChecked;
        e.Handled = true;
      }
    }
  }
}
