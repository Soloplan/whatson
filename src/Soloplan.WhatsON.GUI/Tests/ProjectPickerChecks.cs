// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectPickerChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Runtime.ExceptionServices;
  using System.Text;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;
  using TheArtOfDev.HtmlRenderer.WPF;

  internal static class ProjectPickerChecks
  {
    internal static void Verify(App app)
    {
      var previousLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? ApplicationLanguage.German : ApplicationLanguage.English;
      try
      {
        foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
        {
          Localizer.ApplyLanguage(ApplicationLanguage.English);
          app.ApplyTheme(theme);
          VerifyPicker(theme);
        }
      }
      finally
      {
        Localizer.ApplyLanguage(previousLanguage);
      }
    }

    internal static ListBox GetProjectList(ProjectsTreeView picker, int column)
    {
      var browser = (ProjectBrowserViewModel)picker.DataContext;
      return Descendants<ListBox>(picker).Single(list => ReferenceEquals(list.DataContext, browser.Columns[column]));
    }

    private static void VerifyPicker(ApplicationTheme theme)
    {
      var controller = new WizardController(null, new ApplicationConfiguration(), null) { ProposedServerAddress = "https://ci.example.invalid/" };
      controller.Projects.MultiSelectionMode = true;
      foreach (var name in new[] { "Application", "Services", "Release", "Tools", "Operations" })
      {
        var folder = controller.Projects.AddProject(new Project(null, name, null));
        for (var index = 1; index <= 20; index++)
        {
          var job = folder.AddProject(new Project("https://ci.example.invalid/", $"Build {index:00}", null));
          job.AlreadyAdded = index == 20;
          job.AddedProject = job.AlreadyAdded ? "Production" : null;
        }
      }

      var nested = controller.Projects[0].AddProject(new Project(null, "Components", null));
      var platform = nested.AddProject(new Project(null, "Windows", null));
      var branch = platform.AddProject(new Project(null, "Main", null));
      branch.AddProject(new Project("https://ci.example.invalid/", "Compile", null));

      var page = new ProjectSelectionWizardPage(controller) { DataContext = controller };
      var currentPage = typeof(WizardController).GetProperty("CurrentPage", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected wizard navigation state.");
      currentPage.SetValue(controller, page);
      var attach = typeof(WizardController).GetMethod("AttachToProjectsPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected selection observation.");
      attach.Invoke(controller, null);
      var wizard = new WizardWindow(controller) { Left = -20000, Top = -20000, ShowActivated = false };
      try
      {
        wizard.Frame.Content = page;
        wizard.Show();
        Flush(wizard);
        var picker = (ProjectsTreeView)page.FindName("projectsTreeView");
        var browser = (ProjectBrowserViewModel)picker.DataContext;
        var folders = GetProjectList(picker, 0);
        var jobs = GetProjectList(picker, 1);
        var all = (CheckBox)picker.FindName("SelectAllProjects");
        Flush(wizard);
        Assert.That(browser.Columns, Has.Count.EqualTo(2));
        Assert.That(browser.Columns[1].Folder, Is.SameAs(controller.Projects[0]));
        Assert.That(jobs.Items.Count, Is.EqualTo(21));
        var folderRow = (ListBoxItem)folders.ItemContainerGenerator.ContainerFromIndex(0);
        Assert.That(folderRow.IsSelected, Is.True, "Keep the selected folder highlighted in its column.");
        var folderCheckbox = Descendants<CheckBox>(folderRow).First();
        folderCheckbox.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        Assert.That(browser.SelectedCount, Is.EqualTo(20));
        Assert.That(all.IsChecked, Is.Null, "The global checkbox must show selection in only one folder.");
        all.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        Assert.That(browser.SelectedCount, Is.EqualTo(96), "Select all must include nested folders and skip the five existing jobs.");
        all.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
        Assert.That(browser.SelectedCount, Is.Zero);
        var firstJob = (ListBoxItem)jobs.ItemContainerGenerator.ContainerFromIndex(0);
        Descendants<CheckBox>(firstJob).Single().SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        Assert.That(browser.SelectedCount, Is.EqualTo(1));
        Assert.That(((Button)wizard.FindName("FinishButton")).IsEnabled, Is.True);
        Assert.That(folderCheckbox.IsChecked, Is.Null);
        VerifyDescriptionImages(firstJob, wizard);

        var search = (TextBox)page.FindName("searchTextBox");
        search.Text = "Build 01";
        search.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Flush(wizard);
        Assert.That(jobs.Items.Count, Is.EqualTo(1), "Filtering must stay within the highlighted folder.");
        search.Text = "Compile";
        search.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Flush(wizard);
        Assert.That(jobs.Items.Count, Is.Zero, "A deep descendant must not appear in its ancestor's list.");
        search.Text = "Build 01";
        search.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Flush(wizard);
        var hiddenScrollbar = Descendants<ScrollBar>(jobs).First(bar => bar.Orientation == Orientation.Vertical);
        Assert.That(hiddenScrollbar.IsVisible, Is.False);
        firstJob = (ListBoxItem)jobs.ItemContainerGenerator.ContainerFromIndex(0);
        var unusedSpace = jobs.ActualWidth - firstJob.TranslatePoint(new Point(firstJob.ActualWidth, 0), jobs).X;
        Assert.That(unusedSpace, Is.InRange(7, 20), "Do not reserve a scrollbar gutter when there is no scrollbar.");
        search.Text = string.Empty;
        search.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

        foreach (var size in new[] { new Size(600, 600), new Size(760, 680) })
        {
          wizard.Width = size.Width;
          wizard.Height = size.Height;
          Flush(wizard);
          Assert.That(jobs.ActualWidth, Is.GreaterThan(200));
          Assert.That(jobs.ActualHeight, Is.GreaterThan(60));
          var scrollbar = Descendants<ScrollBar>(jobs).First(bar => bar.Orientation == Orientation.Vertical);
          Assert.That(scrollbar.IsVisible, Is.True);
          firstJob = (ListBoxItem)jobs.ItemContainerGenerator.ContainerFromIndex(0);
          var rowRight = firstJob.TranslatePoint(new Point(firstJob.ActualWidth, 0), jobs).X;
          var scrollbarLeft = scrollbar.TranslatePoint(new Point(0, 0), jobs).X;
          Assert.That(scrollbarLeft - rowRight, Is.GreaterThanOrEqualTo(7), "Keep a gutter between rows and the scrollbar.");
        }

        Localizer.ApplyLanguage(ApplicationLanguage.German);
        Flush(wizard);
        Assert.That(all.Content, Is.EqualTo(Strings.SelectAllProjects));
        Assert.That(Descendants<TextBlock>(picker).Any(text => text.Text == Strings.ProjectServerRoot), Is.True);
        Assert.That(browser.SelectedCount, Is.EqualTo(1), "Changing language must preserve selections.");
        Localizer.ApplyLanguage(ApplicationLanguage.English);
        Assert.That(folders.Items.Count, Is.EqualTo(5), "The first column shows root folders, not their flattened jobs.");
        jobs.ScrollIntoView(nested);
        Flush(wizard);
        var nestedRow = (ListBoxItem)jobs.ItemContainerGenerator.ContainerFromItem(nested);
        Descendants<Button>(nestedRow).Single(button => button.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Flush(wizard);
        Assert.That(browser.Columns, Has.Count.EqualTo(3));
        var platformList = GetProjectList(picker, 2);
        Assert.That(platformList.Items.Cast<ProjectViewModel>(), Is.EqualTo(new[] { platform }));
        platformList.SelectedItem = platform;
        Flush(wizard);
        GetProjectList(picker, 3).SelectedItem = branch;
        Flush(wizard);
        Assert.That(browser.Columns, Has.Count.EqualTo(5));
        var buildList = GetProjectList(picker, 4);
        Assert.That(buildList.Items.Cast<ProjectViewModel>().Single().Name, Is.EqualTo("Compile"));
        Assert.That(browser.SelectedCount, Is.EqualTo(1), "Navigation must preserve selections in other folders.");
        var scroller = (ScrollViewer)picker.FindName("ColumnsScrollViewer");
        Assert.That(scroller.HorizontalOffset, Is.GreaterThan(0), "Scroll new columns into view when the path is wider than the window.");
        Assert.That(buildList.TranslatePoint(new Point(buildList.ActualWidth, 0), scroller).X,
          Is.LessThanOrEqualTo(scroller.ActualWidth), "The newly opened column must be fully visible.");
        folders.SelectedItem = controller.Projects[1];
        Flush(wizard);
        Assert.That(browser.Columns, Has.Count.EqualTo(2), "A sibling folder replaces the following path.");
        Assert.That(GetProjectList(picker, 1).Items.Count, Is.EqualTo(20));
        Assert.That(scroller.HorizontalOffset, Is.Zero);
        folders.SelectedItem = controller.Projects[0];
        Flush(wizard);
        SavePreview((FrameworkElement)wizard.Content, theme);
      }
      finally
      {
        wizard.Close();
      }
    }

    private static void VerifyDescriptionImages(ListBoxItem row, Window wizard)
    {
      var tooltip = Descendants<Grid>(row).Select(grid => grid.ToolTip).OfType<ToolTip>().Single();
      var html = (HtmlPanel)tooltip.Content;
      var imageErrors = new List<string>();
      var decodingErrors = new ConcurrentBag<Exception>();
      var handledImages = new List<bool>();
      html.RenderError += (sender, args) => imageErrors.Add(args.Data.Message);
      html.ImageLoad += (sender, args) => handledImages.Add(args.Data.Handled);
      EventHandler<FirstChanceExceptionEventArgs> observe = (sender, args) =>
      {
        if (args.Exception is NotSupportedException && args.Exception.StackTrace?.Contains("BitmapDecoder", StringComparison.Ordinal) == true)
        {
          decodingErrors.Add(args.Exception);
        }
      };
      AppDomain.CurrentDomain.FirstChanceException += observe;
      try
      {
        tooltip.DataContext = row.DataContext;
        tooltip.PlacementTarget = row;
        tooltip.IsOpen = true;
        var svg = Convert.ToBase64String(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10'/></svg>"));
        ((ProjectViewModel)row.DataContext).Description = $"<p>Build description</p><img src='data:image/svg+xml;base64,{svg}' />"
          + "<img src='data:image/png;base64,bm90IGFuIGltYWdl' /><span style='background-image:url(https://images.example.invalid/badge.svg)'>Status</span>";
        Flush(wizard);
        Assert.That(html.Text, Does.Contain("Build description"));
        Assert.That(handledImages, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(decodingErrors, Is.Empty, "Unsupported images must not raise first-chance BitmapDecoder exceptions.");
        Assert.That(handledImages.All(handled => handled), Is.True, "Intercept images before the decoder or downloader sees them.");
        Assert.That(imageErrors, Is.Empty);
      }
      finally
      {
        tooltip.IsOpen = false;
        AppDomain.CurrentDomain.FirstChanceException -= observe;
      }
    }

    private static void SavePreview(FrameworkElement content, ApplicationTheme theme)
    {
      var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(content);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"ProjectPicker-{theme}.png");
      using (var stream = File.Create(path))
      {
        encoder.Save(stream);
      }

      TestContext.AddTestAttachment(path);
    }

    private static void Flush(Window window)
    {
      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
      window.UpdateLayout();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent)
      where T : DependencyObject
    {
      for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
      {
        var child = VisualTreeHelper.GetChild(parent, index);
        if (child is T match)
        {
          yield return match;
        }

        foreach (var descendant in Descendants<T>(child))
        {
          yield return descendant;
        }
      }
    }
  }
}
