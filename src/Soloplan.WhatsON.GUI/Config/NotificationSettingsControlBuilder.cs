// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationSettingsControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Data;
  using MaterialDesignThemes.Wpf;
  using Newtonsoft.Json;

  /// <summary>
  /// The control builder for the notification settings.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.ConfigControlBuilder" />
  public class NotificationSettingsControlBuilder : ConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public override string SupportedConfigurationItemsKey => null;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="Control" /> for the <see cref="configItem" />.
    /// </returns>
    public override FrameworkElement GetControlInternal(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var card = new Card();
      card.Margin = new Thickness(4, 0, 4, 0);
      card.SetResourceReference(Control.BackgroundProperty, "MaterialDesignBackground");
      var expander = new Expander();
      card.Content = expander;
      expander.HorizontalAlignment = HorizontalAlignment.Stretch;
      var itemsStackPanel = new StackPanel();
      expander.Content = itemsStackPanel;

      IList<NotificationState> notificationStates = new List<NotificationState>();
      this.ApplyConfigurationItemToNotificationStateList(notificationStates, configItem, configItemAttribute, expander);
      var configItemPropertyChanged = (INotifyPropertyChanged)configItem;
      configItemPropertyChanged.PropertyChanged += (s, e) => this.ApplyConfigurationItemToNotificationStateList(notificationStates, configItem, configItemAttribute, expander);

      foreach (var notificationState in notificationStates)
      {
        var itemStackPanel = new StackPanel();
        itemStackPanel.Orientation = Orientation.Horizontal;
        itemStackPanel.Margin = new Thickness(2, 6, 2, 0);
        var toggleButton = new ToggleButton();
        var toogleButtonBinding = new Binding(nameof(NotificationState.IsActive));
        toogleButtonBinding.Source = notificationState;
        toogleButtonBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        toggleButton.SetBinding(ToggleButton.IsCheckedProperty, toogleButtonBinding);
        itemStackPanel.Children.Add(toggleButton);
        var label = new Label();
        label.Content = notificationState.Caption;

        if (!notificationState.IsUseGlobalSettings)
        {
          var toogleButtonEnabledBinding = new Binding(nameof(NotificationState.IsNotUseGlobalSettingsForParentListActive));
          toogleButtonEnabledBinding.Source = notificationState;
          toogleButtonEnabledBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
          toggleButton.SetBinding(UIElement.IsEnabledProperty, toogleButtonEnabledBinding);
        }

        itemStackPanel.Children.Add(label);
        itemsStackPanel.Children.Add(itemStackPanel);
      }

      this.UpdateExpanderHeader(expander, notificationStates);
      return card;
    }

    /// <summary>
    /// Removes the unsupported <see cref="ObservationState"/>s by plugin.
    /// </summary>
    /// <param name="observationStates">The observation states collection.</param>
    /// <param name="observationState"><see cref="ObservationState"/> to check it's support on plugin.</param>
    /// <param name="supportsState">Call to support state check.</param>
    private void RemoveNotSupportedObservationState(ICollection<ObservationState> observationStates, ObservationState observationState, Func<bool> supportsState)
    {
      var observationStateListItem = observationStates.FirstOrDefault(s => s == observationState);
      if (observationStateListItem != default(ObservationState) && !supportsState())
      {
        observationStates.Remove(observationStateListItem);
      }
    }

    private void RemoveNotSupportedObservationStates(IList<ObservationState> observationStates, NotificationConfigurationItemAttribute attribute)
    {
      this.RemoveNotSupportedObservationState(observationStates, ObservationState.Failure, () => attribute.SupportsFailureNotify);
      this.RemoveNotSupportedObservationState(observationStates, ObservationState.Success, () => attribute.SupportsSuccessNotify);
      this.RemoveNotSupportedObservationState(observationStates, ObservationState.Running, () => attribute.SupportsRunningNotify);
      this.RemoveNotSupportedObservationState(observationStates, ObservationState.Unstable, () => attribute.SupportsUnstableNotify);
    }

    /// <summary>
    /// Gets the <see cref="ConnectorNotificationConfiguration"/> based on <see cref="IConfigurationItem"/> using deserialization.
    /// </summary>
    /// <param name="configItem">The configuration item.</param>
    /// <returns>The deserialized instance of <see cref="ConnectorNotificationConfiguration"/> or a new instance if it does not exists.</returns>
    private ConnectorNotificationConfiguration GetConnectorNotificationConfiguration(IConfigurationItem configItem)
    {
      if (string.IsNullOrWhiteSpace(configItem.Value))
      {
        return new ConnectorNotificationConfiguration();
      }

      try
      {
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        return JsonConvert.DeserializeObject<ConnectorNotificationConfiguration>(configItem.Value, settings);
      }
      catch
      {
        return new ConnectorNotificationConfiguration();
      }
    }

    /// <summary>
    /// Applies the <see cref="IConfigurationItem" />to list of <see cref="NotificationState" />s.
    /// </summary>
    /// <param name="notificationStates">The notification states.</param>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <param name="expander">The expander control.</param>
    private void ApplyConfigurationItemToNotificationStateList(IList<NotificationState> notificationStates, IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute, Expander expander)
    {
      var configItemValue = this.GetConnectorNotificationConfiguration(configItem);
      var observationStates = Enum.GetValues(typeof(ObservationState)).Cast<ObservationState>().ToList();
      observationStates.Remove(observationStates.First(i => i == ObservationState.Unknown));
      if (configItemAttribute is NotificationConfigurationItemAttribute notificationConfigurationItemAttribute)
      {
        this.RemoveNotSupportedObservationStates(observationStates, notificationConfigurationItemAttribute);
      }

      var useGlobalNotificationState = notificationStates.FirstOrDefault(n => n.IsUseGlobalSettings);
      if (useGlobalNotificationState == null)
      {
        useGlobalNotificationState = new NotificationState(notificationStates);
        useGlobalNotificationState.IsActive = configItemValue.UseGlobalNotificationSettings;
        useGlobalNotificationState.Caption = "Use global configuration"; // TODO load from resources
        useGlobalNotificationState.PropertyChanged += (s, e) => this.NotificationStateChanged(useGlobalNotificationState, configItem, e.PropertyName);
        useGlobalNotificationState.IsUseGlobalSettings = true;
        notificationStates.Add(useGlobalNotificationState);
      }
      else
      {
        useGlobalNotificationState.IsActive = configItemValue.UseGlobalNotificationSettings;
      }

      foreach (var observationState in observationStates)
      {
        var notificationState = notificationStates.FirstOrDefault(n => n.ObservationState == observationState);
        if (notificationState == null)
        {
          notificationState = new NotificationState(notificationStates);
          notificationState.IsActive = configItemValue.AsObservationStateFlag(observationState);
          notificationState.ObservationState = observationState;
          notificationState.Caption = observationState.ToString(); // TODO load from resources
          notificationState.PropertyChanged += (s, e) => this.NotificationStateChanged(notificationState, configItem, e.PropertyName);
          notificationState.PropertyChanged += (s, e) => this.UpdateExpanderHeader(expander, notificationStates);
          notificationStates.Add(notificationState);
        }
        else
        {
          notificationState.IsActive = configItemValue.AsObservationStateFlag(observationState);
        }
      }
    }

    /// <summary>
    /// Updates the expander header.
    /// </summary>
    /// <param name="expander">The expander.</param>
    /// <param name="notificationStates">The notification states.</param>
    private void UpdateExpanderHeader(Expander expander, IList<NotificationState> notificationStates)
    {
      expander.Header = $"Notification settings ({this.GetNotificationsStateTextRepresentation(notificationStates)})"; // TODO resources
    }

    /// <summary>
    /// Gets the notifications state text representation.
    /// </summary>
    /// <param name="notificationStates">The notification states.</param>
    /// <returns>The text representation for list of <see cref="NotificationState"/>.</returns>
    private string GetNotificationsStateTextRepresentation(IList<NotificationState> notificationStates)
    {
      if (notificationStates.First(n => n.IsUseGlobalSettings).IsActive)
      {
        return "use global settings"; // TODO resource
      }

      var result = string.Join(", ", notificationStates.Where(n => n.IsActive && !n.IsUseGlobalSettings).Select(n => n.Caption));
      if (string.IsNullOrWhiteSpace(result))
      {
        return "none"; // TODO resource
      }

      return result;
    }

    /// <summary>
    /// Handled the <see cref="NotificationState"/> changed.
    /// Updates the <see cref="IConfigurationItem"/>.
    /// </summary>
    /// <param name="notificationState">State of the notification.</param>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="propertyName">Name of the property.</param>
    private void NotificationStateChanged(NotificationState notificationState, IConfigurationItem configItem, string propertyName)
    {
      if (propertyName == nameof(NotificationState.IsNotUseGlobalSettingsForParentListActive))
      {
        return;
      }

      var connectorNotificationConfiguration = new ConnectorNotificationConfiguration();
      connectorNotificationConfiguration.UseGlobalNotificationSettings = notificationState.ParentList.First(i => i.IsUseGlobalSettings).IsActive;
      foreach (var item in notificationState.ParentList)
      {
        if (item.IsUseGlobalSettings)
        {
          continue;
        }

        connectorNotificationConfiguration.AssignFromObeservationStateActivity(item.ObservationState, item.IsActive);
      }

      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };
      configItem.Value = JsonConvert.SerializeObject(connectorNotificationConfiguration, settings);
    }

    /// <summary>
    /// The helper class for binding and keeping information about the active state of the notification setting.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    private class NotificationState : INotifyPropertyChanged
    {
      /// <summary>
      /// The is active flag.
      /// </summary>
      private bool isActive;

      /// <summary>
      /// Initializes a new instance of the <see cref="NotificationState"/> class.
      /// </summary>
      /// <param name="parentList">The parent list.</param>
      public NotificationState(IList<NotificationState> parentList)
      {
        this.ParentList = parentList;
      }

      /// <summary>
      /// Occurs when property changed.
      /// </summary>
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Gets the parent <see cref="NotificationState"/> list.
      /// </summary>
      public IList<NotificationState> ParentList { get; }

      /// <summary>
      /// Gets or sets the <see cref="ObservationState"/> - the reference to the enum value.
      /// </summary>
      public ObservationState ObservationState { get; set; }

      /// <summary>
      /// Gets a value indicating whether global settings for notifications are used..
      /// </summary>
      public bool IsNotUseGlobalSettingsForParentListActive => !this.ParentList.First(i => i.IsUseGlobalSettings).isActive;

      /// <summary>
      /// Gets or sets a value indicating whether this notification setting is active.
      /// </summary>
      public bool IsActive
      {
        get => this.isActive;
        set
        {
          this.isActive = value;
          this.OnPropertyChanged();
          foreach (var listItem in this.ParentList)
          {
            listItem.OnPropertyChanged(nameof(this.IsNotUseGlobalSettingsForParentListActive));
          }
        }
      }

      /// <summary>
      /// Gets or sets a value indicating whether this state is flag for use of notification settings from global configurations.
      /// </summary>s
      public bool IsUseGlobalSettings { get; set; }

      /// <summary>
      /// Gets or sets the caption.
      /// </summary>
      public string Caption { get; set; }

      /// <summary>
      /// Called when property changed.
      /// </summary>
      /// <param name="propertyName">Name of the property.</param>
      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}