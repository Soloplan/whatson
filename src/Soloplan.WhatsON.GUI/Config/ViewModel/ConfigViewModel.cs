// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// The view model for see <see cref="Configuration"/>.
  /// </summary>
  public class ConfigViewModel : ViewModelBase
  {
    /// <summary>
    /// The dark theme enabled.
    /// </summary>
    private bool darkThemeEnabled;

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    /// <summary>
    /// Occurs when configuration is about to be applied.
    /// </summary>
    public event EventHandler<EventArgs> ConfigurationApplying;

    /// <summary>
    /// Gets the subjects list.
    /// </summary>
    public SubjectViewModelCollection Subjects { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// </summary>
    public bool DarkThemeEnabled
    {
      get => this.darkThemeEnabled;
      set
      {
        this.darkThemeEnabled = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the original configuration.
    /// </summary>
    public ApplicationConfiguration Configuration { get; private set; }

    /// <summary>
    /// Gets a value indicating whether configuration is modified.
    /// </summary>
    /// <value>
    ///   <c>true</c> if configuration is modified; otherwise, <c>false</c>.
    /// </value>
    public bool ConfigurationIsModified { get; private set; }

    /// <summary>
    /// Gets a value indicating whether configuration is not modified.
    /// </summary>
    /// <value>
    ///   <c>true</c> if configuration is not modified; otherwise, <c>false</c>.
    /// </value>
    public bool ConfigurationIsNotModified => !this.ConfigurationIsModified;

    /// <summary>
    /// Loads the configuration view model from the source object.
    /// </summary>
    /// <param name="configurationSource">The configuration source.</param>
    public void Load(ApplicationConfiguration configurationSource)
    {
      this.IsLoaded = false;
      var configurationIsModifiedOldValue = this.ConfigurationIsModified;
      try
      {
        this.Configuration = configurationSource;
        if (this.Subjects == null)
        {
          this.Subjects = new SubjectViewModelCollection();
        }

        this.DarkThemeEnabled = configurationSource.DarkThemeEnabled;

        this.ConfigurationIsModified = false;

        this.Subjects.CollectionChanged -= this.SubjectsCollectionChanged;
        this.Subjects.CollectionItemPropertyChanged -= this.SubjectsCollectionItemPropertyChanged;

        this.Subjects.Load(configurationSource);
      }
      finally
      {
        this.IsLoaded = true;
        if (this.Subjects != null)
        {
          this.Subjects.CollectionChanged += this.SubjectsCollectionChanged;
          this.Subjects.CollectionItemPropertyChanged += this.SubjectsCollectionItemPropertyChanged;
        }
      }

      if (configurationIsModifiedOldValue)
      {
        this.OnPropertyChanged(nameof(this.ConfigurationIsModified));
        this.OnPropertyChanged(nameof(this.ConfigurationIsNotModified));
      }
    }

    /// <summary>
    /// Applies to source and saves changes.
    /// </summary>
    public void ApplyToSourceAndSave()
    {
      this.ConfigurationApplying?.Invoke(this, new EventArgs());
      try
      {
        if (!this.ConfigurationIsModified)
        {
          return;
        }

        IList<SubjectConfiguration> subjectsToRemove = new List<SubjectConfiguration>();
        foreach (var sourceSubject in this.Configuration.SubjectsConfiguration)
        {
          if (this.Subjects.All(s => s.SourceSubjectConfiguration != sourceSubject))
          {
            subjectsToRemove.Add(sourceSubject);
          }
        }

        foreach (var subjectToRemove in subjectsToRemove)
        {
          this.Configuration.SubjectsConfiguration.Remove(subjectToRemove);
        }

        foreach (var subject in this.Subjects)
        {
          subject.ApplyToSource(out bool newSubjectConfigurationCreated);
          if (newSubjectConfigurationCreated)
          {
            this.Configuration.SubjectsConfiguration.Add(subject.SourceSubjectConfiguration);
          }
        }

        this.Configuration.DarkThemeEnabled = this.DarkThemeEnabled;
        SerializationHelper.SaveConfiguration(this.Configuration);
      }
      finally
      {
        this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.Configuration));
      }
    }

    /// <summary>
    /// Exports the configuration to specified file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public void Export(string filePath)
    {
      SerializationHelper.Save(this.Configuration, filePath);
    }

    /// <summary>
    /// Imports the configuration from specified file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public void Import(string filePath)
    {
      this.ConfigurationApplying?.Invoke(this, new EventArgs());
      try
      {
        var newConfiguration = SerializationHelper.Load<ApplicationConfiguration>(filePath);
        this.Load(newConfiguration);
      }
      finally
      {
        this.ConfigurationIsModified = true;
        this.ApplyToSourceAndSave();
        this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.Configuration));
      }
    }

    /// <summary>
    /// Called when significant property of the view model was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (!this.ConfigurationIsModified && propertyName != nameof(this.ConfigurationIsModified) && propertyName != nameof(this.ConfigurationIsNotModified) && this.IsLoaded)
      {
        this.ConfigurationIsModified = true;
        this.OnPropertyChanged(nameof(this.ConfigurationIsModified));
        this.OnPropertyChanged(nameof(this.ConfigurationIsNotModified));
      }

      base.OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Handles the CollectionItemPropertyChanged event of the Subjects object.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void SubjectsCollectionItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      this.OnPropertyChanged(nameof(this.Subjects));
    }

    /// <summary>
    /// Handles the changes of <see cref="Subjects"/> collection.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
    private void SubjectsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      this.ConfigurationIsModified = true;
      this.OnPropertyChanged(nameof(this.ConfigurationIsModified));
      this.OnPropertyChanged(nameof(this.ConfigurationIsNotModified));
    }
  }
}