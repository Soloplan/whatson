// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalConfigDataViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;

  /// <summary>
  /// Some data of the configuration has to be globally accessible for binding purposes, otherwise configuration data structure would have to refer to it's parent object.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public class GlobalConfigDataViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static volatile GlobalConfigDataViewModel instance;

    /// <summary>
    /// The configuration view model.
    /// </summary>
    private ConfigViewModel configuration;

    /// <summary>
    /// Occurs when property was changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <remarks>
    /// Getter of this property is thread-safe.
    /// </remarks>
    public static GlobalConfigDataViewModel Instance
    {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => instance ?? (instance = new GlobalConfigDataViewModel());
    }

    /// <summary>
    /// Gets the used categories.
    /// </summary>
    public IList<string> UsedCategories
    {
      get
      {
        var result = new List<string>();
        foreach (var subject in this.configuration.Subjects)
        {
          var configItemViewModel = subject.GetConfigurationByKey(Subject.Category);
          if (configItemViewModel != null && !string.IsNullOrWhiteSpace(configItemViewModel.Value) && !result.Contains(configItemViewModel.Value))
          {
            result.Add(configItemViewModel.Value);
          }
        }

        return result;
      }
    }

    /// <summary>
    /// Sets the used configuration.
    /// </summary>
    /// <param name="configurationViewModel">The configuration view model.</param>
    public void UseConfiguration(ConfigViewModel configurationViewModel)
    {
      this.configuration = configurationViewModel;
      this.ConfigurationResetOrUpdated();
    }

    /// <summary>
    /// Configurations was reset or updated.
    /// </summary>
    public void ConfigurationResetOrUpdated()
    {
      this.OnPropertyChanged(nameof(this.UsedCategories));
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}