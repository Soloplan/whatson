// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectSelectionWizardPage.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Data;
  using System.Windows.Threading;

  /// <summary>
  /// Interaction logic for ProjectSelectionWizardPage.xaml.
  /// </summary>
  public partial class ProjectSelectionWizardPage : Page
  {
    /// <summary>
    /// The wizard controllers.
    /// </summary>
    private readonly WizardController wizardController;

    /// <summary>
    /// The search typing timer.
    /// </summary>
    private DispatcherTimer searchTypingTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectSelectionWizardPage" /> class.
    /// </summary>
    /// <param name="wizardController">The wizard controller.</param>
    public ProjectSelectionWizardPage(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.InitializeComponent();
      this.projectsTreeView.Loaded += (s, e) =>
      {
        if (this.projectsTreeView.mainTreeView.Items.Count == 0)
        {
          return;
        }

        var treeViewItem = (TreeViewItem)this.projectsTreeView.mainTreeView.ItemContainerGenerator.ContainerFromItem(this.projectsTreeView.mainTreeView.Items[0]);
        treeViewItem?.Focus();
      };
    }

    /// <summary>
    /// Handles the PreviewKeyUp event of the MainTreeView control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
    private void MainTreeViewPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == System.Windows.Input.Key.Enter)
      {
        this.wizardController.GoToNextPage();
        e.Handled = true;
      }
    }

    /// <summary>
    /// Handles the Loaded event of the uxGrouppingSettings control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void UxGrouppingSettingsLoaded(object sender, RoutedEventArgs e)
    {
      var bindingExpressionbase = BindingOperations.GetBindingExpressionBase((ComboBox)sender, Selector.SelectedItemProperty);
      bindingExpressionbase?.UpdateTarget();
    }

    /// <summary>
    /// Shoulds the be visibile due to the fact that parents contains the searched text.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="searchTextLowercase">The search text lowercase.</param>
    /// <returns>True if the project should be visible.</returns>
    private bool ShouldBeVisibileDueToParents(ProjectViewModel project, string searchTextLowercase)
    {
      if (project == null)
      {
        return false;
      }

      var newVisibility = string.IsNullOrWhiteSpace(searchTextLowercase) || project.Name.ToLower().Contains(searchTextLowercase);
      if (newVisibility)
      {
        return true;
      }

      return this.ShouldBeVisibileDueToParents(project.Parent, searchTextLowercase);
    }

    /// <summary>
    /// Checks if the project and sub projects should be visible.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="searchTextLowercase">The search text lowercase.</param>
    private void CheckProjectAndSubProjectsShouldBeVisible(ProjectViewModel project, string searchTextLowercase)
    {
      var newVisibility = string.IsNullOrWhiteSpace(searchTextLowercase) || project.Name.ToLower().Contains(searchTextLowercase);
      if (this.ShouldBeVisibileDueToParents(project.Parent, searchTextLowercase))
      {
        newVisibility = true;
      }

      if (project.IsChecked)
      {
        newVisibility = true;
      }

      if (project.IsVisible != newVisibility)
      {
        project.IsVisible = newVisibility;
      }

      this.UpdateParentProjectsVisibility(project);

      if (project.Projects == null || project.Projects.Count == 0)
      {
        return;
      }

      foreach (var subProject in project.Projects)
      {
        this.CheckProjectAndSubProjectsShouldBeVisible(subProject, searchTextLowercase);
      }
    }

    /// <summary>
    /// Checks if the projects should be visible.
    /// </summary>
    /// <param name="projects">The projects.</param>
    /// <param name="searchTextLowercase">The search text lowercase.</param>
    private void CheckProjectsShouldBeVisible(IReadOnlyList<ProjectViewModel> projects, string searchTextLowercase)
    {
      foreach (var project in projects)
      {
        this.CheckProjectAndSubProjectsShouldBeVisible(project, searchTextLowercase);
      }
    }

    /// <summary>
    /// Updates the parent projects visibility.
    /// </summary>
    /// <param name="project">The project.</param>
    private void UpdateParentProjectsVisibility(ProjectViewModel project)
    {
      if (project.Parent == null)
      {
        return;
      }

      var newVisibility = project.IsVisible;

      // do not hide project groups where not all items are hidden
      if (!project.IsVisible && project.Parent.Projects.Any(p => p.IsVisible))
      {
        newVisibility = true;
      }

      if (project.IsVisible != project.Parent.IsVisible)
      {
        project.Parent.IsVisible = newVisibility;
      }

      this.UpdateParentProjectsVisibility(project.Parent);
    }

    /// <summary>
    /// Handles the TextChanged event of the TextBox control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
    private void SearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
      if (this.searchTypingTimer == null)
      {
        this.searchTypingTimer = new DispatcherTimer();
        this.searchTypingTimer.Interval = TimeSpan.FromMilliseconds(250);
        this.searchTypingTimer.Tick += this.SearchTypingTimerTick;
      }

      this.searchTypingTimer.Stop();
      this.searchTypingTimer.Start();
    }

    /// <summary>
    /// Handles the Tick event of the SearchTypingTimer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void SearchTypingTimerTick(object sender, EventArgs e)
    {
      this.searchTypingTimer.Stop();

      if (this.wizardController.Projects == null)
      {
        return;
      }

      this.CheckProjectsShouldBeVisible(this.wizardController.Projects, this.searchTextBox.Text.ToLower().Trim());
    }
  }
}
