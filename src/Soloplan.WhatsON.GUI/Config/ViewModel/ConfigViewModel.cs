// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;

  /// <summary>
  /// The view model for see <see cref="Configuration"/>.
  /// </summary>
  public class ConfigViewModel : ViewModelBase
  {
    /// <summary>
    /// The subjects list.
    /// </summary>
    private IList<SubjectViewModel> subjects;

    /// <summary>
    /// Gets the subjects list.
    /// </summary>
    public IList<SubjectViewModel> Subjects => this.subjects;

    /// <summary>
    /// Gets the original configuration.
    /// </summary>
    public Configuration Configuration { get; private set; }

    /// <summary>
    /// Gets a value indicating whether configuration is modified.
    /// </summary>
    /// <value>
    ///   <c>true</c> if configuration is modified; otherwise, <c>false</c>.
    /// </value>
    public bool ConfigurationIsModified { get; private set; }

    /// <summary>
    /// Loads the configuration view model from the source object.
    /// </summary>
    /// <param name="configurationSource">The configuration source.</param>
    public void Load(Configuration configurationSource)
    {
      this.IsLoaded = false;
      var configurationIsModifiedOldValue = this.ConfigurationIsModified;
      try
      {
        this.Configuration = configurationSource;
        if (this.subjects == null)
        {
          this.subjects = new List<SubjectViewModel>();
        }

        var subjectsToRemove = this.subjects.Where(svm => this.Configuration.Subjects.All(s => s.Name != svm.Name));
        foreach (var subjectToRemove in subjectsToRemove)
        {
          this.subjects.Remove(subjectToRemove);
        }

        foreach (var subject in configurationSource.Subjects)
        {
          var subjectViewModel = this.subjects.FirstOrDefault(x => x.Name == subject.Name);
          if (subjectViewModel == null)
          {
            subjectViewModel = new SubjectViewModel();
            this.subjects.Add(subjectViewModel);
            subjectViewModel.PropertyChanged += (s, e) =>
            {
              if (subjectViewModel.IsLoaded)
              {
                this.OnPropertyChanged(nameof(this.Subjects));
              }
            };
          }

          subjectViewModel.Load(subject);
        }

        this.ConfigurationIsModified = false;
      }
      finally
      {
        this.IsLoaded = true;
      }

      if (configurationIsModifiedOldValue)
      {
        this.OnPropertyChanged(nameof(this.ConfigurationIsModified));
      }
    }

    /// <summary>
    /// Applies to source.
    /// </summary>
    public void ApplyToSource()
    {
      foreach (var subject in this.subjects)
      {
        if (subject.IsDeleted)
        {
          this.Configuration.Subjects.Remove(subject.SourceSubject);
          continue;
        }

        subject.ApplyToSource(out bool newSubjectCreated);
        if (newSubjectCreated)
        {
          this.Configuration.Subjects.Add(subject.SourceSubject);
        }
      }
    }

    /// <summary>
    /// Called when significant property of the view model was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (!this.ConfigurationIsModified && propertyName != nameof(this.ConfigurationIsModified) && this.IsLoaded)
      {
        this.ConfigurationIsModified = true;
        this.OnPropertyChanged(nameof(this.ConfigurationIsModified));
      }

      base.OnPropertyChanged(propertyName);
    }
  }
}