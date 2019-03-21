// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectGroup.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Collections.ObjectModel;

  public class SubjectGroup
  {
    private ObservableCollection<Subject> subjects;

    public string Category { get; set; }

    public ObservableCollection<Subject> Subjects => this.subjects ?? (this.subjects = new ObservableCollection<Subject>());
  }
}