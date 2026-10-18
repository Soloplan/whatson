// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserColumn.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using PropertyChanged;

  public partial class ProjectBrowserColumn : INotifyPropertyChanged
  {
    private readonly Action<ProjectBrowserColumn> selectionChanged;

    public ProjectBrowserColumn(ProjectViewModel? folder, IReadOnlyList<ProjectViewModel> projects, Action<ProjectBrowserColumn> selectionChanged)
    {
      this.Folder = folder;
      this.Projects = projects;
      this.selectionChanged = selectionChanged;
    }

    public ProjectViewModel? Folder { get; }

    public IReadOnlyList<ProjectViewModel> Projects { get; }

    public bool IsLast { get; set; } = true;

    [OnChangedMethod(nameof(OnSelectedProjectChanged))]
    public ProjectViewModel? SelectedProject { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public IReadOnlyList<ProjectViewModel> VisibleProjects => this.Projects
      .Where(project => project.Name.Contains(this.SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

    public bool HasVisibleProjects => this.VisibleProjects.Count > 0;

    private void OnSelectedProjectChanged() => this.selectionChanged(this);
  }
}
