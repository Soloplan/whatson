// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorViewModelCollection.cs" company="Soloplan GmbH">
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
  /// The <see cref="ObservableCollection{T}"/> implementation for connectors with some additional events.
  /// </summary>
  /// <seealso cref="ConnectorViewModel" />
  public class ConnectorViewModelCollection : ObservableCollection<ConnectorViewModel>
  {
    /// <summary>
    /// Occurs when connectors were loaded.
    /// </summary>
    public event EventHandler<EventArgs> Loaded;

    /// <summary>
    /// Occurs when collection item property changed.
    /// </summary>
    public event EventHandler<PropertyChangedEventArgs> CollectionItemPropertyChanged;

    /// <summary>
    /// Should be called when connectors were loaded by external code.
    /// </summary>
    public void LoadCompleted()
    {
      this.Loaded?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Loads the connectors from the source configuration.
    /// </summary>
    /// <param name="configurationSource">The source configuration.</param>
    public void Load(ApplicationConfiguration configurationSource)
    {
      try
      {
        var connectorsToRemove = this.Where(svm => configurationSource.ConnectorsConfiguration.All(s => s.Identifier != svm.Identifier)).ToList();
        foreach (var connectorToRemove in connectorsToRemove)
        {
          this.Remove(connectorToRemove);
        }

        foreach (var connectorConfiguration in configurationSource.ConnectorsConfiguration)
        {
          var connectorViewModel = this.FirstOrDefault(x => x.Identifier == connectorConfiguration.Identifier);
          if (connectorViewModel != null)
          {
            connectorViewModel.Load(connectorConfiguration);
            continue;
          }

          connectorViewModel = new ConnectorViewModel();
          connectorViewModel.Load(connectorConfiguration);
          this.Add(connectorViewModel);
          connectorViewModel.PropertyChanged += (s, e) =>
          {
            if (connectorViewModel.IsLoaded)
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
    /// Applies connectors collection to configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void ApplyToConfiguration(ApplicationConfiguration configuration)
    {
      IList<ConnectorConfiguration> connectorsToRemove = new List<ConnectorConfiguration>();
      foreach (var sourceConnector in configuration.ConnectorsConfiguration)
      {
        if (this.All(s => s.SourceConnectorConfiguration != sourceConnector))
        {
          connectorsToRemove.Add(sourceConnector);
        }
      }

      foreach (var connectorToRemove in connectorsToRemove)
      {
        configuration.ConnectorsConfiguration.Remove(connectorToRemove);
      }

      foreach (var connector in this)
      {
        var connectorConfiguration = connector.ApplyToSourceConnectorConfiguration(out bool newConnectorConfigurationCreated);
        if (newConnectorConfigurationCreated)
        {
          configuration.ConnectorsConfiguration.Add(connectorConfiguration);
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