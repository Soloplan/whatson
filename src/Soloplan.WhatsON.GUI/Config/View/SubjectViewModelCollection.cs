// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectViewModelCollection.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

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
    public void Load(Configuration configurationSource)
    {
      try
      {
        var subjectsToRemove = this.Where(svm => configurationSource.Subjects.All(s => s.Identifier != svm.Identifier)).ToList();
        foreach (var subjectToRemove in subjectsToRemove)
        {
          this.Remove(subjectToRemove);
        }

        foreach (var subject in configurationSource.Subjects)
        {
          var subjectViewModel = this.FirstOrDefault(x => x.Identifier == subject.Identifier);
          if (subjectViewModel != null)
          {
            subjectViewModel.Load(subject);
            continue;
          }

          subjectViewModel = new SubjectViewModel();
          subjectViewModel.Load(subject);
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
    /// Raises the <see cref="E:CollectionItemPropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyChangedEventArgs">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void OnCollectionItemPropertyChanged(PropertyChangedEventArgs propertyChangedEventArgs)
    {
      this.CollectionItemPropertyChanged?.Invoke(this, propertyChangedEventArgs);
    }
  }
}