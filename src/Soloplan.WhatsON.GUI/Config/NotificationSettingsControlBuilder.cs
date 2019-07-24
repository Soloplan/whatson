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
      itemsStackPanel.Orientation = Orientation.Horizontal;
      expander.Content = itemsStackPanel;
      var itemsStackPanelColumn1 = new StackPanel();
      itemsStackPanelColumn1.Margin = new Thickness(0, 0, 50, 0);
      var itemsStackPanelColumn2 = new StackPanel();
      itemsStackPanel.Children.Add(itemsStackPanelColumn1);
      itemsStackPanel.Children.Add(itemsStackPanelColumn2);

      var notificationState = new NotificationsState();

      this.ApplyConfigurationItemToNotificationState(notificationState, configItem, configItemAttribute, expander);
      var configItemPropertyChanged = (INotifyPropertyChanged)configItem;
      configItemPropertyChanged.PropertyChanged += (s, e) => this.ApplyConfigurationItemToNotificationState(notificationState, configItem, configItemAttribute, expander);

      foreach (var notificationStateItem in notificationState.ItemsState)
      {
        var stackPanelForToggleButton = this.CreateControlForNotificationStateItem(notificationStateItem);
        itemsStackPanelColumn1.Children.Add(stackPanelForToggleButton);
      }

      var stackPanelForUseGlobalToggleButton = this.CreateControlForNotificationStateItem(notificationState.UseGlobalSettings, false);
      itemsStackPanelColumn2.Children.Add(stackPanelForUseGlobalToggleButton);

      var stackPanelForOnlyIfStateChangedToggleButton = this.CreateControlForNotificationStateItem(notificationState.OnlyIfStateChanged);
      itemsStackPanelColumn2.Children.Add(stackPanelForOnlyIfStateChangedToggleButton);

      this.AssignPropertyChangeOfStateItems(notificationState, configItem);
      this.UpdateExpanderHeader(expander, notificationState);
      return card;
    }

    private void AssignPropertyChangeOfStateItems(NotificationsState notificationsState, IConfigurationItem configItem)
    {
      notificationsState.UseGlobalSettings.PropertyChanged += (s, e) => this.NotificationStateChanged(notificationsState.UseGlobalSettings, configItem, e.PropertyName);
      notificationsState.OnlyIfStateChanged.PropertyChanged += (s, e) => this.NotificationStateChanged(notificationsState.OnlyIfStateChanged, configItem, e.PropertyName);
      foreach (var notificationStateItem in notificationsState.ItemsState)
      {
        notificationStateItem.PropertyChanged += (s, e) => this.NotificationStateChanged(notificationStateItem, configItem, e.PropertyName);
      }
    }

    /// <summary>
    /// Creates the control for notification state item.
    /// </summary>
    /// <param name="notificationStateItem">The notification state item.</param>
    /// <param name="assignGlobalSettingsEnabledDependency">if set to <c>true</c> [assign global settings enabled dependency].</param>
    /// <returns>The <see cref="StackPanel"/> with the toggle button and caption.</returns>
    private StackPanel CreateControlForNotificationStateItem(NotificationStateItem notificationStateItem, bool assignGlobalSettingsEnabledDependency = true)
    {
      var itemStackPanel = new StackPanel();
      itemStackPanel.Orientation = Orientation.Horizontal;
      itemStackPanel.Margin = new Thickness(2, 6, 2, 0);
      var toggleButton = new ToggleButton();
      var toogleButtonBinding = new Binding(nameof(NotificationStateItem.IsActive));
      toogleButtonBinding.Source = notificationStateItem;
      toogleButtonBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      toggleButton.SetBinding(ToggleButton.IsCheckedProperty, toogleButtonBinding);
      itemStackPanel.Children.Add(toggleButton);
      var label = new Label();
      label.Content = notificationStateItem.Caption;

      if (assignGlobalSettingsEnabledDependency)
      {
        var toogleButtonEnabledBinding = new Binding(nameof(NotificationStateItem.IsNotUseGlobalSettingsForParentListActive));
        toogleButtonEnabledBinding.Source = notificationStateItem;
        toogleButtonEnabledBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        toggleButton.SetBinding(UIElement.IsEnabledProperty, toogleButtonEnabledBinding);
      }

      itemStackPanel.Children.Add(label);
      return itemStackPanel;
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
      this.RemoveNotSupportedObservationState(observationStates, ObservationState.Unknown, () => attribute.SupportsUnknownNotify);
    }

    /// <summary>
    /// Applies the <see cref="IConfigurationItem" />to <see cref="NotificationsState" />s.
    /// </summary>
    /// <param name="notificationsState">The notification state.</param>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <param name="expander">The expander control.</param>
    private void ApplyConfigurationItemToNotificationState(NotificationsState notificationsState, IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute, Expander expander)
    {
      var configItemValue = configItem.GetOrCreateConnectorNotificationConfiguration();
      var observationStates = Enum.GetValues(typeof(ObservationState)).Cast<ObservationState>().ToList();
      if (configItemAttribute is NotificationConfigurationItemAttribute notificationConfigurationItemAttribute)
      {
        this.RemoveNotSupportedObservationStates(observationStates, notificationConfigurationItemAttribute);
      }

      var useGlobalSettingsNewState = configItemValue.UseGlobalNotificationSettings;
      if (useGlobalSettingsNewState != notificationsState.UseGlobalSettings.IsActive)
      {
        notificationsState.UseGlobalSettings.IsActive = useGlobalSettingsNewState;
      }

      var onlyIfStateChangedSettingsNewState = configItemValue.OnlyIfChanged;
      if (onlyIfStateChangedSettingsNewState != notificationsState.OnlyIfStateChanged.IsActive)
      {
        notificationsState.OnlyIfStateChanged.IsActive = onlyIfStateChangedSettingsNewState;
      }

      foreach (var observationState in observationStates)
      {
        var notificationStateItem = notificationsState.ItemsState.FirstOrDefault(n => n.ObservationState == observationState);
        if (notificationStateItem == null)
        {
          notificationStateItem = new NotificationStateItem(notificationsState);
          notificationStateItem.IsActive = configItemValue.AsObservationStateFlag(observationState);
          notificationStateItem.ObservationState = observationState;
          notificationStateItem.Caption = observationState.ToString(); // TODO load from resources
          notificationStateItem.PropertyChanged += (s, e) => this.NotificationStateChanged(notificationStateItem, configItem, e.PropertyName);
          notificationStateItem.PropertyChanged += (s, e) => this.UpdateExpanderHeader(expander, notificationsState);
          notificationsState.ItemsState.Add(notificationStateItem);
        }
        else
        {
          var newActiveState = configItemValue.UseGlobalNotificationSettings;
          if (notificationStateItem.IsActive != newActiveState)
          {
            notificationStateItem.IsActive = configItemValue.AsObservationStateFlag(observationState);
          }
        }
      }
    }

    /// <summary>
    /// Updates the expander header.
    /// </summary>
    /// <param name="expander">The expander.</param>
    /// <param name="notificationsState">The notification state.</param>
    private void UpdateExpanderHeader(Expander expander, NotificationsState notificationsState)
    {
      expander.Header = $"Notification settings ({this.GetNotificationsStateTextRepresentation(notificationsState)})"; // TODO resources
    }

    /// <summary>
    /// Gets the notifications state text representation.
    /// </summary>
    /// <param name="notificationsState">The notification state.</param>
    /// <returns>The text representation for list of <see cref="NotificationStateItem"/>.</returns>
    private string GetNotificationsStateTextRepresentation(NotificationsState notificationsState)
    {
      if (notificationsState.UseGlobalSettings.IsActive)
      {
        return "use main settings"; // TODO resource
      }

      var result = string.Join(", ", notificationsState.ItemsState.Where(n => n.IsActive).Select(n => n.Caption));
      if (notificationsState.OnlyIfStateChanged.IsActive)
      {
        if (!string.IsNullOrWhiteSpace(result))
        {
          result += ", ";
        }

        result += notificationsState.OnlyIfStateChanged.Caption;
      }

      if (string.IsNullOrWhiteSpace(result))
      {
        return "none"; // TODO resource
      }

      return result;
    }

    /// <summary>
    /// Handled the <see cref="NotificationStateItem"/> changed.
    /// Updates the <see cref="IConfigurationItem"/>.
    /// </summary>
    /// <param name="notificationStateItem">State of the notification.</param>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="propertyName">Name of the property.</param>
    private void NotificationStateChanged(NotificationStateItem notificationStateItem, IConfigurationItem configItem, string propertyName)
    {
      if (propertyName == nameof(NotificationStateItem.IsNotUseGlobalSettingsForParentListActive))
      {
        return;
      }

      var connectorNotificationConfiguration = new ConnectorNotificationConfiguration();
      connectorNotificationConfiguration.UseGlobalNotificationSettings = notificationStateItem.NotificationsState.UseGlobalSettings.IsActive;
      connectorNotificationConfiguration.OnlyIfChanged = notificationStateItem.NotificationsState.OnlyIfStateChanged.IsActive;
      foreach (var item in notificationStateItem.NotificationsState.ItemsState)
      {
        connectorNotificationConfiguration.AssignFromObeservationStateActivity(item.ObservationState, item.IsActive);
      }

      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };
      configItem.Value = JsonConvert.SerializeObject(connectorNotificationConfiguration, settings);
    }

    /// <summary>
    /// The notifications state for all possible states plus additional settings.
    /// </summary>
    private class NotificationsState
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="NotificationsState"/> class.
      /// </summary>
      public NotificationsState()
      {
        this.UseGlobalSettings = new NotificationStateItem(this);
        this.UseGlobalSettings.Caption = "Use main configuration"; // TODO load from resources
        this.UseGlobalSettings.PropertyChanged += (s, e) =>
          {
            foreach (var listItem in this.ItemsState)
            {
              listItem.OnPropertyChanged(nameof(NotificationStateItem.IsNotUseGlobalSettingsForParentListActive));
            }

            this.OnlyIfStateChanged.OnPropertyChanged(nameof(NotificationStateItem.IsNotUseGlobalSettingsForParentListActive));
          };

        this.OnlyIfStateChanged = new NotificationStateItem(this);
        this.OnlyIfStateChanged.Caption = "Only if status changed"; // TODO load from resources
      }

      /// <summary>
      /// Gets the state items which represents all possible states.
      /// </summary>
      public IList<NotificationStateItem> ItemsState { get; } = new List<NotificationStateItem>();

      /// <summary>
      /// Gets the state item which represents the use global settings flag.
      /// </summary>
      public NotificationStateItem UseGlobalSettings
      {
        get;
      }

      /// <summary>
      /// Gets the state item which represents the "Only if state changed" flag.
      /// </summary>
      public NotificationStateItem OnlyIfStateChanged
      {
        get;
      }
    }

    /// <summary>
    /// The helper class for binding and keeping information about the active state of the notification setting.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    private class NotificationStateItem : INotifyPropertyChanged
    {
      /// <summary>
      /// The is active flag.
      /// </summary>
      private bool isActive;

      /// <summary>
      /// Initializes a new instance of the <see cref="NotificationStateItem"/> class.
      /// </summary>
      /// <param name="notificationsState">The notification state.</param>
      public NotificationStateItem(NotificationsState notificationsState)
      {
        this.NotificationsState = notificationsState;
      }

      /// <summary>
      /// Occurs when property changed.
      /// </summary>
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Gets the notification state.
      /// </summary>
      public NotificationsState NotificationsState { get; }

      /// <summary>
      /// Gets or sets the <see cref="ObservationState"/> - the reference to the enum value.
      /// </summary>
      public ObservationState ObservationState { get; set; }

      /// <summary>
      /// Gets a value indicating whether global settings for notifications are used..
      /// </summary>
      public bool IsNotUseGlobalSettingsForParentListActive => !this.NotificationsState.UseGlobalSettings.IsActive;

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
        }
      }

      /// <summary>
      /// Gets or sets the caption.
      /// </summary>
      public string Caption { get; set; }

      /// <summary>
      /// Called when property changed.
      /// </summary>
      /// <param name="propertyName">Name of the property.</param>
      public void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}