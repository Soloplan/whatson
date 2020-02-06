// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// The view model for the selectable projects on the wizard.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public class ProjectViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// The root list of the projects.
    /// </summary>
    private readonly ProjectViewModelList rootList;

    /// <summary>
    /// The projects.
    /// </summary>
    private List<ProjectViewModel> projects;

    /// <summary>
    /// Is checked flag.
    /// </summary>
    private bool isChecked;

    /// <summary>
    /// The is visible flag.
    /// </summary>
    private bool isVisible = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectViewModel" /> class which will be a root element.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="rootList">The root list of projcts.</param>
    public ProjectViewModel(string name, ProjectViewModelList rootList)
    {
      this.rootList = rootList;
      this.Name = name;
    }

    /// <summary>
    /// Occurs when project property changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the projects.
    /// </summary>
    public IReadOnlyList<ProjectViewModel> Projects => this.projects ?? (this.projects = new List<ProjectViewModel>());

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the address of the project.
    /// </summary>
    public string Address { get; set; }

    public bool AlreadyAdded { get; set; }

    public string AddedProject { get; set; }

    public string FullName { get; set; }

    public string Description { get; set; }

    public bool IsVisible
    {
      get => this.isVisible;
      set
      {
        this.isVisible = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this project is checked.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this project is checked; otherwise, <c>false</c>.
    /// </value>
    public bool IsChecked
    {
      get => this.isChecked;
      set
      {
        this.isChecked = value;
        this.OnPropertyChanged();
        if (this.rootList.MultiSelectionMode)
        {
          foreach (var project in this.Projects)
          {
            project.IsChecked = value;
          }
        }
        else
        {
          this.rootList.UncheckAll(this);
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether this project is checkable - supports checking (some items might be only some grouping items).
    /// </summary>
    /// <value>
    ///   <c>true</c> if this project is checkable; otherwise, <c>false</c>.
    /// </value>
    public bool IsCheckable
    {
      get
      {
        if (this.rootList.MultiSelectionMode)
        {
          return true;
        }

        if (this.Projects.Count == 0)
        {
          return true;
        }

        return false;
      }
    }

    /// <summary>
    /// Gets or sets the parent.
    /// </summary>
    public ProjectViewModel Parent { get; set; }

    /// <summary>
    /// Determines whether is any project checked.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if any project is checked; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAnyChecked()
    {
      if (this.IsChecked)
      {
        return true;
      }

      foreach (var project in this.Projects)
      {
        if (project.IsAnyChecked())
        {
          return true;
        }
      }

      return false;
    }

    public ProjectViewModel AddProject(Project project)
    {
      if (this.projects == null)
      {
        this.projects = new List<ProjectViewModel>();
      }

      var newProject = new ProjectViewModel(project.Name, this.rootList) { FullName = project.FullName, Description = project.Description };
      this.projects.Add(newProject);
      return newProject;
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}