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
    /// Gets the original subject item.
    /// </summary>
    private Subject subject;

    /// <summary>
    /// The configuration.
    /// </summary>
    private IList<ConfigurationItemViewModel> configurationItems;

    /// <summary>
    /// The name of the subject.
    /// </summary>
    private string name;

    /// <summary>
    /// Gets or sets the name of the subject.
    /// </summary>
    public string Name
    {
      get => this.CheckIsLoadedAndGetValue(() => this.name);
      set => this.name = value;
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public IList<ConfigurationItemViewModel> ConfigurationItems => this.CheckIsLoadedAndGetValue(() => this.configurationItems);

    /// <summary>
    /// Loads the subject view model from the source object.
    /// </summary>
    /// <param name="subjectSource">The subject source.</param>
    public void Load(Subject subjectSource)
    {
      this.subject = subjectSource;
      this.name = subjectSource.Name;
      this.configurationItems = new List<ConfigurationItemViewModel>();
      foreach (var configItem in subjectSource.Configuration)
      {
        var configItemViewModel = new ConfigurationItemViewModel();
        configItemViewModel.Load(configItem);
        this.configurationItems.Add(configItemViewModel);
      }

      this.IsLoaded = true;
    }

    /// <summary>
    /// Gets the subject configuration attributes.
    /// </summary>
    /// <returns>The subject attributes.</returns>
    public IList<ConfigurationItemAttribute> GetSubjectConfigAttributes()
    {
      return this.subject.GetType().GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
    }

    /// <summary>
    /// Gets (and might also create if it does not exists) the configuration view model by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration view model.</returns>
    public ConfigurationItemViewModel GetConfigurationByKey(string key)
    {
      var configItem = this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
      if (configItem == null)
      {
        configItem = new ConfigurationItemViewModel();
        configItem.Load(key);
        this.ConfigurationItems.Add(configItem);
        return configItem;
      }

      return this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
    }
  }
}