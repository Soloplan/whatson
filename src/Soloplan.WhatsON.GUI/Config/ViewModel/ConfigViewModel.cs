// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System;
  using System.Collections.Generic;

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
    public IList<SubjectViewModel> Subjects => this.CheckIsLoadedAndGetValue(() => this.subjects);

    /// <summary>
    /// Gets the original configuration.
    /// </summary>
    public Configuration Configuration { get; private set; }

    /// <summary>
    /// Loads the configuration view model from the source object.
    /// </summary>
    /// <param name="configurationSource">The configuration source.</param>
    public void Load(Configuration configurationSource)
    {
      this.Configuration = configurationSource;
      this.subjects = new List<SubjectViewModel>();
      foreach (var subject in configurationSource.Subjects)
      {
        var subjectViewModel = new SubjectViewModel();
        subjectViewModel.Load(subject);
        this.subjects.Add(subjectViewModel);
      }

      this.IsLoaded = true;
    }
  }
}