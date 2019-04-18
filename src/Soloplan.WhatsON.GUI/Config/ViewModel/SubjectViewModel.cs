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
  using Soloplan.WhatsON.Serialization;

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
    private SubjectConfiguration sourceSubjectConfiguration;

    /// <summary>
    /// The source subject Plugin.
    /// </summary>
    private ISubjectPlugin sourceSubjectPlugin;

    /// <summary>
    /// Gets the subject source configuration.
    /// </summary>
    public SubjectConfiguration SourceSubjectConfiguration
    {
      get => this.sourceSubjectConfiguration;
      private set
      {
        this.sourceSubjectConfiguration = value;
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
        if (this.sourceSubjectPlugin == null && this.SourceSubjectConfiguration != null)
        {
          this.sourceSubjectPlugin = PluginsManager.Instance.GetPlugin(this.SourceSubjectConfiguration);
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
    public void Load(SubjectConfiguration subjectSource)
    {
      this.IsLoaded = false;
      try
      {
        this.SourceSubjectConfiguration = subjectSource;
        if (this.ConfigurationItems == null)
        {
          this.ConfigurationItems = new List<ConfigurationItemViewModel>();
        }

        if (subjectSource != null)
        {
          this.Name = subjectSource.Name;
          this.Identifier = subjectSource.Identifier;
          var configurationItemsToNull = this.ConfigurationItems.Where(cvm => this.SourceSubjectConfiguration.ConfigurationItems.All(c => c.Key != cvm.Key));
          foreach (var configurationItemToNull in configurationItemsToNull)
          {
            configurationItemToNull.Value = null;
          }

          foreach (var configItem in subjectSource.ConfigurationItems)
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
      var configurationItemAttributes = this.SourceSubjectPlugin.SubjectType.GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
      foreach (var configurationItemAttribute in configurationItemAttributes)
      {
        ConfigResourcesHelper.ApplyConfigResourses(configurationItemAttribute, this.SourceSubjectPlugin.SubjectType);
      }

      return configurationItemAttributes;
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

    public delegate ConfigurationItem ApplyToDelegate(ConfigurationItemViewModel configurationItemViewModel, out bool newItemCreated);

    public void ApplyToSubjectConfiguration(SubjectConfiguration sourceSubjectConfig, ApplyToDelegate applyToDelegate)
    {
      foreach (var configurationItem in this.ConfigurationItems)
      {
        var appliedConfigurationItem = applyToDelegate(configurationItem, out bool newItemCreated);
        if (newItemCreated)
        {
          sourceSubjectConfig.ConfigurationItems.Add(appliedConfigurationItem);
        }
      }
    }

    public static SubjectConfiguration ApplyToSourceSubjectConfiguration(SubjectViewModel viewModel, out bool newSubjectConfigurationCreated)
    {
      newSubjectConfigurationCreated = false;
      if (viewModel.SourceSubjectConfiguration == null)
      {
        newSubjectConfigurationCreated = true;
        viewModel.SourceSubjectConfiguration = CreateNewSubjectConfiguration(viewModel);
      }
      else
      {
        viewModel.SourceSubjectConfiguration.Name = viewModel.name;
        viewModel.ApplyToSubjectConfiguration(viewModel.SourceSubjectConfiguration, ConfigurationItemViewModel.ApplyToSource);
      }

      return viewModel.SourceSubjectConfiguration;
    }

    public static SubjectConfiguration ApplyToNewSubjectConfiguration(SubjectViewModel viewModel, out bool newSubjectConfigurationCreated)
    {
      newSubjectConfigurationCreated = true;
      var sourceSubjectConfig = CreateNewSubjectConfiguration(viewModel);
      return sourceSubjectConfig;
    }

    public static SubjectConfiguration CreateNewSubjectConfiguration(SubjectViewModel viewModel)
    {
      var sourceSubjectConfig = new SubjectConfiguration(viewModel.SourceSubjectPlugin.GetType().FullName);
      sourceSubjectConfig.Name = viewModel.name;
      viewModel.ApplyToSubjectConfiguration(sourceSubjectConfig, ConfigurationItemViewModel.CreateNewConfigurationItem);
      return sourceSubjectConfig;
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