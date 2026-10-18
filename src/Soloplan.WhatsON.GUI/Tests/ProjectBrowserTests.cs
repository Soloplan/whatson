// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class ProjectBrowserTests
  {
    [Test]
    public void FolderSelectionAppendsColumnsAndSiblingSelectionReplacesTheFollowingPath()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      var rootJob = roots.AddProject(Project("Root job"));
      var folder = roots.AddProject(Project("Application"));
      var main = folder.AddProject(Project("Main"));
      var nested = folder.AddProject(Project("Release"));
      var installer = nested.AddProject(Project("Installer"));
      var platform = nested.AddProject(Project("Windows"));
      var branch = platform.AddProject(Project("Main"));
      var build = branch.AddProject(Project("Build"));
      var browser = new ProjectBrowserViewModel(roots);
      Assert.That(browser.Columns, Has.Count.EqualTo(1));
      Assert.That(browser.Columns[0].VisibleProjects, Is.EqualTo(new[] { rootJob, folder }));
      browser.Columns[0].SelectedProject = folder;
      Assert.That(browser.Columns, Has.Count.EqualTo(2));
      Assert.That(browser.Columns[1].VisibleProjects, Is.EqualTo(new[] { main, nested }));
      browser.Columns[1].SelectedProject = nested;
      var activeColumn = browser.Columns[2];
      var changes = Observe(activeColumn);
      Assert.That(activeColumn.VisibleProjects, Is.EqualTo(new[] { installer, platform }));
      browser.SearchText = " root JOB ";
      Assert.That(activeColumn.VisibleProjects, Is.Empty, "Filtering must not bring in jobs from other folders.");
      Assert.That(changes, Does.Contain(nameof(activeColumn.VisibleProjects)).And.Contain(nameof(activeColumn.HasVisibleProjects)));
      browser.SearchText = " INSTALLer ";
      Assert.That(activeColumn.VisibleProjects, Is.EqualTo(new[] { installer }));
      browser.SearchText = "missing";
      Assert.That(activeColumn.HasVisibleProjects, Is.False);
      activeColumn.SelectedProject = platform;
      browser.Columns[3].SelectedProject = branch;
      Assert.That(browser.SearchText, Is.Empty, "Do not carry an old folder's filter into a different folder.");
      Assert.That(browser.Columns, Has.Count.EqualTo(5));
      Assert.That(browser.Columns[4].VisibleProjects, Is.EqualTo(new[] { build }));
      build.IsChecked = true;
      browser.Columns[4].SelectedProject = build;
      Assert.That(browser.Columns, Has.Count.EqualTo(5), "Selecting a job must not create an empty column.");
      browser.Columns[1].SelectedProject = main;
      Assert.That(browser.Columns, Has.Count.EqualTo(2));
      Assert.That(browser.Columns[1].IsLast, Is.True);
      browser.Columns[0].SelectedProject = rootJob;
      Assert.That(browser.Columns, Has.Count.EqualTo(1));
      Assert.That(browser.SelectedCount, Is.EqualTo(1), "Closing columns must preserve checked projects.");
    }

    [Test]
    public void FolderOnlyServersStartWithTheFirstFolderOpen()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      var folder = roots.AddProject(Project("Application"));
      var main = folder.AddProject(Project("Main"));
      var browser = new ProjectBrowserViewModel(roots);
      Assert.That(browser.Columns, Has.Count.EqualTo(2));
      Assert.That(browser.Columns[0].SelectedProject, Is.SameAs(folder));
      Assert.That(browser.Columns[1].Folder, Is.SameAs(folder));
      Assert.That(browser.Columns[1].VisibleProjects, Is.EqualTo(new[] { main }));
    }

    [Test]
    public void FilteringASelectedJobDoesNotClearTheSearchOrItsCheckbox()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      var main = roots.AddProject(Project("Main"));
      roots.AddProject(Project("Release"));
      var browser = new ProjectBrowserViewModel(roots);
      browser.Columns[0].SelectedProject = main;
      main.IsChecked = true;
      browser.SearchText = "Release";
      browser.Columns[0].SelectedProject = null;
      Assert.That(browser.SearchText, Is.EqualTo("Release"));
      Assert.That(browser.Columns[0].VisibleProjects.Single().Name, Is.EqualTo("Release"));
      Assert.That(main.IsChecked, Is.True);
      Assert.That(browser.Columns, Has.Count.EqualTo(1));
    }

    [Test]
    public void SelectAllIgnoresFolderAndSearchFiltersAndSkipsAlreadyAddedProjects()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      var rootJob = roots.AddProject(Project("Root job"));
      var folder = roots.AddProject(Project("Application"));
      var main = folder.AddProject(Project("Main"));
      var release = folder.AddProject(Project("Release"));
      release.AlreadyAdded = true;
      var browser = new ProjectBrowserViewModel(roots);
      browser.Columns[0].SelectedProject = folder;
      browser.SearchText = "Main";
      var changes = Observe(browser);
      main.IsChecked = true;
      Assert.That(browser.AllSelectionState, Is.Null);
      browser.AllSelectionState = true;
      Assert.That(rootJob.IsChecked, Is.True);
      Assert.That(main.IsChecked, Is.True);
      Assert.That(release.IsChecked, Is.False);
      Assert.That(browser.SelectedCount, Is.EqualTo(2));
      Assert.That(browser.AllSelectionState, Is.True);
      Assert.That(changes, Does.Contain(nameof(browser.SelectedCount)).And.Contain(nameof(browser.AllSelectionState)));
      browser.AllSelectionState = false;
      Assert.That(browser.SelectedCount, Is.Zero);
      Assert.That(browser.AllSelectionState, Is.False);
    }

    [Test]
    public void FolderCheckboxShowsPartialSelectionAndSelectsAllNestedEligibleJobs()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      var folder = roots.AddProject(Project("Application"));
      var main = folder.AddProject(Project("Main"));
      var nested = folder.AddProject(Project("Release"));
      var installer = nested.AddProject(Project("Installer"));
      var added = nested.AddProject(Project("Added"));
      added.AlreadyAdded = true;
      installer.IsVisible = false;
      main.IsChecked = true;
      Assert.That(folder.SelectionState, Is.Null);
      Assert.That(nested.SelectionState, Is.False);
      folder.SelectionState = true;
      Assert.That(installer.IsChecked, Is.True);
      Assert.That(added.IsChecked, Is.False);
      Assert.That(nested.SelectionState, Is.True);
      Assert.That(folder.SelectionState, Is.True);
      folder.SelectionState = false;
      Assert.That(roots.GetChecked(), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyOrAlreadyAddedOnlyListsCannotBeSelected(bool addJob)
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = true };
      if (addJob)
      {
        var folder = roots.AddProject(Project("Application"));
        folder.AddProject(Project("Added")).AlreadyAdded = true;
        Assert.That(folder.IsCheckable, Is.False);
        folder.IsChecked = true;
        Assert.That(folder.SelectionState, Is.False);
      }

      var browser = new ProjectBrowserViewModel(roots);
      browser.AllSelectionState = true;
      Assert.That(browser.HasSelectableProjects, Is.False);
      Assert.That(browser.AllSelectionState, Is.False);
      Assert.That(browser.SelectedCount, Is.Zero);
    }

    [Test]
    public void SingleProjectEditingKeepsExclusiveSelectionAndAllowsTheExistingJob()
    {
      var roots = new ProjectViewModelList { MultiSelectionMode = false };
      var folder = roots.AddProject(Project("Application"));
      var first = folder.AddProject(Project("Main"));
      var second = folder.AddProject(Project("Release"));
      first.AlreadyAdded = true;
      var browser = new ProjectBrowserViewModel(roots);
      browser.AllSelectionState = true;
      Assert.That(browser.SelectedCount, Is.Zero);
      Assert.That(folder.IsCheckable, Is.False);
      first.IsChecked = true;
      Assert.That(first.IsChecked, Is.True);
      second.IsChecked = true;
      Assert.That(first.IsChecked, Is.False);
      Assert.That(second.IsChecked, Is.True);
      Assert.That(browser.SelectedCount, Is.EqualTo(1));
    }

    [Test]
    public void WizardExportsOnlyNewLeafJobsWithTheirOriginalHierarchy()
    {
      var controller = new WizardController(null, new ApplicationConfiguration(), null);
      controller.Projects.MultiSelectionMode = true;
      controller.Projects.PlugIn = new JenkinsPlugin();
      var folder = controller.Projects.AddProject(Project("Application"));
      var main = folder.AddProject(Project("Main"));
      main.FullName = "Application/Main";
      var added = folder.AddProject(Project("Added"));
      added.AlreadyAdded = true;
      folder.IsChecked = true;
      var selected = controller.GetSelectedProjects();
      Assert.That(selected, Has.Count.EqualTo(1));
      Assert.That(selected[0].Name, Is.EqualTo("Main"));
      Assert.That(selected[0].FullName, Is.EqualTo("Application/Main"));
      Assert.That(selected[0].Parent?.Name, Is.EqualTo("Application"));
      Assert.That(selected[0].Plugin, Is.SameAs(controller.Projects.PlugIn));
    }

    [Test]
    public void DuplicateDetectionIncludesRootJobsWhenTheConnectorWasDetectedAutomatically()
    {
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration(
        "Jenkins", "Saved root", Connector.ServerAddress, "https://ci.example.invalid/", Connector.ProjectName, "Root"));
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration(
        "Jenkins", "Saved nested", Connector.ServerAddress, "https://ci.example.invalid/", Connector.ProjectName, "Folder/Main"));
      var controller = new WizardController(null, configuration, null) { ProposedServerAddress = "https://ci.example.invalid/" };
      controller.Projects.PlugIn = new JenkinsPlugin();
      var folder = Project("Folder");
      folder.Children.Add(new Project("https://ci.example.invalid/", "Main", null, "Folder/Main"));
      var process = typeof(WizardController).GetMethod("ProcessServerSubProjects", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected project discovery preparation.");
      process.Invoke(controller, new object?[] { new List<Project> { Project("Root"), folder }, null });
      Assert.That(controller.Projects.Single(project => project.Name == "Root").AlreadyAdded, Is.True);
      Assert.That(controller.Projects.Single(project => project.Name == "Folder").Projects[0].AlreadyAdded, Is.True);
    }

    private static Project Project(string name) => new Project("https://ci.example.invalid/", name, null);

    private static List<string?> Observe(INotifyPropertyChanged model)
    {
      var changes = new List<string?>();
      model.PropertyChanged += (sender, args) => changes.Add(args.PropertyName);
      return changes;
    }
  }
}
