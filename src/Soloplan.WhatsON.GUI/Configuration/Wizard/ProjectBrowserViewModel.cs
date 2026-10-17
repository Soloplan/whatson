// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using PropertyChanged;

  public partial class ProjectBrowserViewModel : INotifyPropertyChanged
  {
    private readonly IReadOnlyList<ProjectViewModel> projects;

    public ProjectBrowserViewModel(ProjectViewModelList roots)
    {
      this.MultiSelectionMode = roots.MultiSelectionMode;
      var all = Descendants(roots).ToArray();
      this.projects = all.Where(project => !project.IsFolder).ToArray();
      this.Columns.Add(new ProjectBrowserColumn(null, roots, this.HandleColumnSelection));
      if (!roots.Any(project => !project.IsFolder))
      {
        this.Columns[0].SelectedProject = roots.FirstOrDefault();
      }

      foreach (var project in this.projects)
      {
        PropertyChangedEventManager.AddHandler(project, this.ProjectSelectionChanged, nameof(ProjectViewModel.IsChecked));
      }
    }

    public bool MultiSelectionMode { get; }

    public ObservableCollection<ProjectBrowserColumn> Columns { get; } = new ObservableCollection<ProjectBrowserColumn>();

    [OnChangedMethod(nameof(OnSearchTextChanged))]
    public string SearchText { get; set; } = string.Empty;

    public bool HasSelectableProjects => this.projects.Any(project => project.IsCheckable);

    public int SelectedCount => this.projects.Count(project => project.IsChecked);

    public bool? AllSelectionState
    {
      get
      {
        var count = this.projects.Count(project => project.IsCheckable);
        return this.SelectedCount == 0 ? false : this.SelectedCount == count ? true : null;
      }

      set
      {
        if (this.MultiSelectionMode)
        {
          // Bulk selection includes jobs outside the current folder and search results.
          foreach (var project in this.projects.Where(project => project.IsCheckable))
          {
            project.IsChecked = value == true;
          }
        }
      }
    }

    private static IEnumerable<ProjectViewModel> Descendants(IEnumerable<ProjectViewModel> projects)
    {
      foreach (var project in projects)
      {
        yield return project;
        foreach (var child in Descendants(project.Projects))
        {
          yield return child;
        }
      }
    }

    private void HandleColumnSelection(ProjectBrowserColumn column)
    {
      var index = this.Columns.IndexOf(column);
      if (index < 0)
      {
        return;
      }

      if (index == this.Columns.Count - 1 && column.SelectedProject?.IsFolder != true)
      {
        return;
      }

      while (this.Columns.Count > index + 1)
      {
        this.Columns.RemoveAt(this.Columns.Count - 1);
      }

      if (column.SelectedProject is { IsFolder: true } folder)
      {
        this.Columns.Add(new ProjectBrowserColumn(folder, folder.Projects, this.HandleColumnSelection));
      }

      foreach (var visibleColumn in this.Columns)
      {
        visibleColumn.SearchText = string.Empty;
        visibleColumn.IsLast = visibleColumn == this.Columns[this.Columns.Count - 1];
      }

      this.SearchText = string.Empty;
    }

    private void OnSearchTextChanged() => this.Columns[this.Columns.Count - 1].SearchText = this.SearchText;

    private void ProjectSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
      this.OnPropertyChanged(nameof(this.SelectedCount));
      this.OnPropertyChanged(nameof(this.AllSelectionState));
    }
  }
}
