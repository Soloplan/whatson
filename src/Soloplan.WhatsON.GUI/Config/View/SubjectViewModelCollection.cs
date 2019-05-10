// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectViewModelCollection.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// The <see cref="ObservableCollection{T}"/> implementation for subjects with some additional events.
  /// </summary>
  /// <seealso cref="SubjectViewModel" />
  public class SubjectViewModelCollection : ObservableCollection<SubjectViewModel>
  {
    /// <summary>
    /// Occurs when subjects were loaded.
    /// </summary>
    public event EventHandler<EventArgs> Loaded;

    /// <summary>
    /// Occurs when collection item property changed.
    /// </summary>
    public event EventHandler<PropertyChangedEventArgs> CollectionItemPropertyChanged;

    /// <summary>
    /// Should be called when subjects were loaded by external code.
    /// </summary>
    public void LoadCompleted()
    {
      this.Loaded?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Loads the subjects from the source configuration.
    /// </summary>
    /// <param name="configurationSource">The source configuration.</param>
    public void Load(ApplicationConfiguration configurationSource)
    {
      try
      {
        var subjectsToRemove = this.Where(svm => configurationSource.SubjectsConfiguration.All(s => s.Identifier != svm.Identifier)).ToList();
        foreach (var subjectToRemove in subjectsToRemove)
        {
          this.Remove(subjectToRemove);
        }

        foreach (var subjectConfiguration in configurationSource.SubjectsConfiguration)
        {
          var subjectViewModel = this.FirstOrDefault(x => x.Identifier == subjectConfiguration.Identifier);
          if (subjectViewModel != null)
          {
            subjectViewModel.Load(subjectConfiguration);
            continue;
          }

          subjectViewModel = new SubjectViewModel();
          subjectViewModel.Load(subjectConfiguration);
          this.Add(subjectViewModel);
          subjectViewModel.PropertyChanged += (s, e) =>
          {
            if (subjectViewModel.IsLoaded)
            {
              this.OnCollectionItemPropertyChanged(e);
            }
          };
        }
      }
      finally
      {
        this.Loaded?.Invoke(this, new EventArgs());
      }
    }

    /// <summary>
    /// Applies subjects collection to configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void ApplyToConfiguration(ApplicationConfiguration configuration)
    {
      IList<SubjectConfiguration> subjectsToRemove = new List<SubjectConfiguration>();
      foreach (var sourceSubject in configuration.SubjectsConfiguration)
      {
        if (this.All(s => s.SourceSubjectConfiguration != sourceSubject))
        {
          subjectsToRemove.Add(sourceSubject);
        }
      }

      foreach (var subjectToRemove in subjectsToRemove)
      {
        configuration.SubjectsConfiguration.Remove(subjectToRemove);
      }

      foreach (var subject in this)
      {
        var subjectConfiguration = subject.ApplyToSourceSubjectConfiguration(out bool newSubjectConfigurationCreated);
        if (newSubjectConfigurationCreated)
        {
          configuration.SubjectsConfiguration.Add(subjectConfiguration);
        }
      }
    }

    /// <summary>
    /// Raises the <see cref="E:CollectionItemPropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyChangedEventArgs">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void OnCollectionItemPropertyChanged(PropertyChangedEventArgs propertyChangedEventArgs)
    {
      this.CollectionItemPropertyChanged?.Invoke(this, propertyChangedEventArgs);
    }
  }
}