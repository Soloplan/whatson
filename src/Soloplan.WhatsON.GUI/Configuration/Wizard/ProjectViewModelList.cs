// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectViewModelList.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System.Collections;
  using System.Collections.Generic;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// The list which is able to keep the <see cref="ProjectViewModel"/>s and serves additional functionalities.
  /// </summary>
  /// <seealso cref="System.Collections.Generic.IReadOnlyList{ProjectViewModel}" />
  public class ProjectViewModelList : IReadOnlyList<ProjectViewModel>
  {
    /// <summary>
    /// The projects inner list.
    /// </summary>
    private readonly List<ProjectViewModel> projects;

    /// <summary>
    /// The uncheck all is running flag.
    /// </summary>
    private bool uncheckAllRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectViewModelList"/> class.
    /// </summary>
    public ProjectViewModelList()
    {
      this.projects = new List<ProjectViewModel>();
    }

    /// <summary>
    /// Gets or sets the plug in used to retrieve the projects list.
    /// </summary>
    public ConnectorPlugin PlugIn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether multi selection mode is active.
    /// </summary>
    public bool MultiSelectionMode { get; set; }

    /// <summary>
    /// Gets the count.
    /// </summary>
    public int Count => this.projects.Count;

    /// <summary>
    /// Gets the <see cref="ProjectViewModel"/> at the specified index.
    /// </summary>
    /// <value>
    /// The <see cref="ProjectViewModel"/>.
    /// </value>
    /// <param name="index">The index.</param>
    /// <returns>The <see cref="ProjectViewModel"/> at specific index.</returns>
    public ProjectViewModel this[int index] => this.projects[index];

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<ProjectViewModel> GetEnumerator()
    {
      return this.projects.GetEnumerator();
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.projects.GetEnumerator();
    }

    /// <summary>
    /// Adds the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The added project.</returns>
    public ProjectViewModel AddProject(Project project)
    {
      var newProject = new ProjectViewModel(project.Name, this) { FullName = project.FullName, Description = project.Description };
      this.projects.Add(newProject);
      return newProject;
    }

    /// <summary>
    /// Gets the list of checked projects, including sub projects.
    /// All checked projects are on first level.
    /// </summary>
    /// <returns>The checked projects.</returns>
    public IList<ProjectViewModel> GetChecked()
    {
      var checkedList = new List<ProjectViewModel>();
      this.GetChecked(this, checkedList);
      return checkedList;
    }

    /// <summary>
    /// Unchecks all.
    /// </summary>
    /// <param name="excludeProject">The project to exclude, for example new checked project which should not be unchecked.</param>
    public void UncheckAll(ProjectViewModel excludeProject)
    {
      if (this.uncheckAllRunning)
      {
        return;
      }

      this.uncheckAllRunning = true;
      try
      {
        foreach (var project in this.projects)
        {
          this.UncheckProject(project, excludeProject);
        }
      }
      finally
      {
        this.uncheckAllRunning = false;
      }
    }

    /// <summary>
    /// Unchecks the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="excludeProject">The exclude project.</param>
    private void UncheckProject(ProjectViewModel project, ProjectViewModel excludeProject)
    {
      if (project != excludeProject)
      {
        project.IsChecked = false;
      }

      foreach (var subProject in project.Projects)
      {
        this.UncheckProject(subProject, excludeProject);
      }
    }

    /// <summary>
    /// Recursively determines checked projects and adds them to given list.
    /// </summary>
    /// <param name="sourceList">The source list to check, including sub lists.</param>
    /// <param name="checkedList">The checked list - the result.</param>
    private void GetChecked(IReadOnlyList<ProjectViewModel> sourceList, IList<ProjectViewModel> checkedList)
    {
      foreach (var project in sourceList)
      {
        if (project.IsChecked)
        {
          checkedList.Add(project);
        }

        this.GetChecked(project.Projects, checkedList);
      }
    }
  }
}