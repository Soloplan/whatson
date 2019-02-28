// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  /// The view model for see <see cref="Subject"/>.
  /// </summary>
  public class SubjectViewModel : ViewModelBase
  {
    /// <summary>
    /// The name of the subject.
    /// </summary>
    private string name;

    /// <summary>
    /// The source subject.
    /// </summary>
    private Subject sourceSubject;

    /// <summary>
    /// The source subject Plugin.
    /// </summary>
    private ISubjectPlugin sourceSubjectPlugin;

    /// <summary>
    /// Gets the subject source configuration.
    /// </summary>
    public Subject SourceSubject
    {
      get => this.sourceSubject;
      private set
      {
        this.sourceSubject = value;
        this.OnPropertyChanged();
        this.OnPropertyChanged(nameof(this.SourceSubjectPlugin));
      }
    }

    /// <summary>
    /// Gets or sets the source subject plugin.
    /// </summary>
    public ISubjectPlugin SourceSubjectPlugin
    {
      get
      {
        if (this.sourceSubjectPlugin == null && this.SourceSubject != null)
        {
          this.sourceSubjectPlugin = PluginsManager.Instance.GetPlugin(this.SourceSubject);
        }

        return this.sourceSubjectPlugin;
      }

      set => this.sourceSubjectPlugin = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Identifier { get; private set; }

    /// <summary>
    /// Gets or sets the name of the subject.
    /// </summary>
    public string Name
    {
      get => this.name;
      set
      {
        this.name = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public List<ConfigurationItemViewModel> ConfigurationItems { get; private set; }

    /// <summary>
    /// Loads the subject view model from the source object.
    /// </summary>
    /// <param name="subjectSource">The subject source.</param>
    public void Load(Subject subjectSource)
    {
      this.IsLoaded = false;
      try
      {
        this.SourceSubject = subjectSource;
        if (this.ConfigurationItems == null)
        {
          this.ConfigurationItems = new List<ConfigurationItemViewModel>();
        }

        if (subjectSource != null)
        {
          this.Name = subjectSource.Name;
          this.Identifier = subjectSource.Identifier;
          var configurationItemsToNull = this.ConfigurationItems.Where(cvm => this.SourceSubject.Configuration.All(c => c.Key != cvm.Key));
          foreach (var configurationItemToNull in configurationItemsToNull)
          {
            configurationItemToNull.Value = null;
          }

          foreach (var configItem in subjectSource.Configuration)
          {
            var configItemViewModel = this.GetConfigurationItemViewModel(configItem.Key);
            configItemViewModel.Load(configItem);
          }
        }
      }
      finally
      {
        this.IsLoaded = true;
      }
    }

    /// <summary>
    /// Gets the subject configuration attributes.
    /// </summary>
    /// <returns>The subject attributes.</returns>
    public IList<ConfigurationItemAttribute> GetSubjectConfigAttributes()
    {
      return this.SourceSubjectPlugin.SubjectType.GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
    }

    /// <summary>
    /// Gets (and might also create if it does not exists) the configuration view model by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration view model.</returns>
    public ConfigurationItemViewModel GetConfigurationByKey(string key)
    {
      var configItem = this.GetConfigurationItemViewModel(key);
      configItem.Load(key);
      return configItem;
    }

    /// <summary>
    /// Applies modifications to source.
    /// </summary>
    /// <param name="newSubjectCreated">if set to <c>true</c> new subject was created.</param>
    public void ApplyToSource(out bool newSubjectCreated)
    {
      newSubjectCreated = false;
      IList<ConfigurationItem> sourceConfiguration;
      if (this.SourceSubject == null)
      {
        newSubjectCreated = true;
        sourceConfiguration = new List<ConfigurationItem>();
      }
      else
      {
        sourceConfiguration = this.sourceSubject.Configuration;
        this.SourceSubject.Name = this.name;
      }

      foreach (var configurationItem in this.ConfigurationItems)
      {
        configurationItem.ApplyToSource(out bool newItemCreated);
        if (newItemCreated)
        {
          sourceConfiguration.Add(configurationItem.ConfigurationItem);
        }
      }

      if (newSubjectCreated)
      {
        this.sourceSubject = this.SourceSubjectPlugin.CreateNew(this.name, sourceConfiguration);
      }
    }

    /// <summary>
    /// Gets the configuration item view model by the given key.
    /// In case it is not existing, a new item is created.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The found or created <see cref="ConfigurationItemViewModel"/>.</returns>
    private ConfigurationItemViewModel GetConfigurationItemViewModel(string key)
    {
      var configItemViewModel = this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
      if (configItemViewModel == null)
      {
        configItemViewModel = new ConfigurationItemViewModel();
        configItemViewModel.PropertyChanged += (s, e) =>
        {
          if (configItemViewModel.IsLoaded)
          {
            this.OnPropertyChanged(nameof(this.ConfigurationItems));
          }
        };

        this.ConfigurationItems.Add(configItemViewModel);
      }

      return configItemViewModel;
    }
  }
}