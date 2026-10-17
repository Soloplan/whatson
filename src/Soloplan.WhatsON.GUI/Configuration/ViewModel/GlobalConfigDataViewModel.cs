// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalConfigDataViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.ViewModel
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Some data of the configuration has to be globally accessible for binding purposes, otherwise configuration data
  /// structure would have to refer to it's parent object.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public partial class GlobalConfigDataViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static volatile GlobalConfigDataViewModel? instance;

    /// <summary>
    /// The configuration view model.
    /// </summary>
    private ConfigViewModel configuration = new ConfigViewModel();

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
        foreach (var connector in this.configuration.Connectors)
        {
          var configItemViewModel = connector.GetConfigurationByKey(Connector.Category);
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
    public void ConfigurationResetOrUpdated() => this.OnPropertyChanged(nameof(this.UsedCategories));
  }
}
