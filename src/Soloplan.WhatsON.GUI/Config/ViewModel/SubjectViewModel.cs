// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubjectsViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  /// The view model for see <see cref="Subject"/>.
  /// </summary>
  public class SubjectViewModel : ViewModelBase
  {
    /// <summary>
    /// The configuration.
    /// </summary>
    private List<ConfigurationItemViewModel> configurationItems;

    /// <summary>
    /// The name of the subject.
    /// </summary>
    private string name;

    /// <summary>
    /// Gets the subject source configuration.
    /// </summary>
    public Subject SourceSubject { get; private set; }

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
    public List<ConfigurationItemViewModel> ConfigurationItems => this.configurationItems;

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
        this.name = subjectSource.Name;
        if (this.configurationItems == null)
        {
          this.configurationItems = new List<ConfigurationItemViewModel>();
        }

        var configurationItemsToNull = this.configurationItems.Where(cvm => this.SourceSubject.Configuration.All(c => c.Key != cvm.Key));
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
      return this.SourceSubject.GetType().GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
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
    /// Marks the subject for deletion.
    /// </summary>
    public void Delete()
    {
      this.IsDeleted = true;
    }

    /// <summary>
    /// The flag which indicated that the <see cref="Subject"/> will deleted on apply operation.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is deleted; otherwise, <c>false</c>.
    /// </value>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Applies modifications to source.
    /// </summary>
    public void ApplyToSource(out bool newSubjectCreated)
    {
      newSubjectCreated = false;
      if (this.SourceSubject == null)
      {
        newSubjectCreated = true;
        // TODO create a new subject of specific type
      }

      this.SourceSubject.Name = this.name;
      foreach (var configurationItem in this.ConfigurationItems)
      {
        configurationItem.ApplyToSource(out bool newItemCreated);
        if (newItemCreated)
        {
          this.SourceSubject.Configuration.Add(configurationItem.ConfigurationItem);
        }
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
      var configItemViewModel = this.configurationItems.FirstOrDefault(x => x.Key == key);
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

        this.configurationItems.Add(configItemViewModel);
      }

      return configItemViewModel;
    }
  }
}