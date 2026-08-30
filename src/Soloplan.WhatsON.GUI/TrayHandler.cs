// <copyright file="TrayHandler.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Drawing;
  using System.Drawing.Drawing2D;
  using System.IO;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Windows.Forms;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Properties;
  using Soloplan.WhatsON.Model;
  using Application = System.Windows.Application;

  /// <summary>
  /// Wrapper for <see cref="NotifyIcon"/> from System.WindowsForms. Handles creation/opening/closing of main window.
  /// </summary>
  public class TrayHandler : IDisposable
  {
    /// <summary>
    /// Scheduler instance.
    /// </summary>
    private readonly ObservationScheduler scheduler;

    /// <summary>
    /// Configuration of application.
    /// </summary>
    private ApplicationConfiguration configuration;

    /// <summary>
    /// The context menu displayed by tray icon.
    /// </summary>
    private ContextMenu contextMenu;

    /// <summary>
    /// The icon.
    /// </summary>
    private NotifyIcon icon;

    /// <summary>
    /// The icon displayed when build-status is pending.
    /// </summary>
    private Icon loadingIcon;

    /// <summary>
    /// The icon displayed when build-status is normal.
    /// </summary>
    private Icon normalIcon;

    /// <summary>
    /// The icon displayed when build-status is unstable.
    /// </summary>
    private Icon unstableIcon;

    /// <summary>
    /// The icon displayed when build-status is broken.
    /// </summary>
    private Icon failureIcon;

    private MainWindow mainWindow;

    private NotificationsModel model;

    /// <summary>
    /// The last time the window was focused by clicking on tray icon.
    /// </summary>
    private DateTime lastWindowFocused;

    public TrayHandler(ObservationScheduler scheduler, ApplicationConfiguration configuration)
    {
      this.loadingIcon = new Icon(Properties.Resources.Whatson, new Size(16, 16));
      this.normalIcon = CreateStatusIcon(this.loadingIcon, Color.LimeGreen);
      this.unstableIcon = CreateStatusIcon(this.loadingIcon, Color.Gold);
      this.failureIcon = CreateStatusIcon(this.loadingIcon, Color.Red);

      this.icon = new System.Windows.Forms.NotifyIcon();
      this.icon.Icon = this.loadingIcon;
      this.icon.Text = Resources.TrayHandler_UpdateTrayIcon_Pending;
      this.icon.Visible = true;
      this.scheduler = scheduler;
      this.configuration = configuration;

      this.contextMenu = new ContextMenu();
      this.icon.ContextMenu = this.contextMenu;
      this.contextMenu.Popup += this.OnContextMenuPopup;
      this.icon.DoubleClick += (s, e) => this.ShowOrHideWindow();
      this.icon.BalloonTipClicked += (s, e) => this.BringToFront(true);
      this.icon.Click += (s, e) => this.BringToFront(false);

      this.model = new NotificationsModel(this.scheduler);
      this.model.PropertyChanged += this.CurrentStatusPropertyChanged;
      this.UpdateTrayIcon();

      if (File.Exists(Path.Combine(SerializationHelper.Instance.ConfigFolder, MainWindow.VisualSettingsFile)))
      {
        this.VisualSettings = SerializationHelper.Load<MainWindowSettings>(Path.Combine(SerializationHelper.Instance.ConfigFolder, MainWindow.VisualSettingsFile));
      }
    }

    public MainWindowSettings VisualSettings { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the main window is visible.
    /// </summary>
    private bool MainWindowVisible => Application.Current.MainWindow is MainWindow window && window.Visibility == System.Windows.Visibility.Visible;

    /// <summary>
    /// Gets instance of <see cref="MainWindow"/>.
    /// </summary>
    private MainWindow MainWindow
    {
      get
      {
        if (this.mainWindow == null)
        {
          this.mainWindow = new MainWindow(this.scheduler, this.configuration, this.model.Connectors.Select(sub => sub.Connector).ToList());

          this.mainWindow.ApplyVisualSettings(this.VisualSettings);

          this.mainWindow.Closing += this.MainWindowClosing;
          this.mainWindow.Closed += this.MainWindowClosed;
          this.mainWindow.ConfigurationApplied += this.MainWindowConfigurationApplied;
        }

        return this.mainWindow;
      }
    }

    public void Dispose()
    {
      this.contextMenu?.Dispose();
      this.contextMenu = null;
      this.icon?.Dispose();
      this.icon = null;
      this.loadingIcon?.Dispose();
      this.loadingIcon = null;
      this.normalIcon?.Dispose();
      this.normalIcon = null;
      this.unstableIcon?.Dispose();
      this.unstableIcon = null;
      this.failureIcon?.Dispose();
      this.failureIcon = null;
    }

    /// <summary>
    /// Shows balloon info.
    /// </summary>
    /// <param name="title">Title of balloon.</param>
    /// <param name="tipText">Text of the balloon.</param>
    /// <param name="i">Icon.</param>
    public void ShowBaloon(string title, string tipText, ToolTipIcon i)
    {
      tipText = string.IsNullOrEmpty(tipText) ? " " : tipText;
      this.icon.ShowBalloonTip(1000, title, tipText, i);
    }

    /// <summary>
    /// Handles showing and hiding main window.
    /// </summary>
    public void ShowOrHideWindow()
    {
      if (this.MainWindowVisible)
      {
        if (this.MainWindow.WindowState == System.Windows.WindowState.Minimized)
        {
          this.MainWindow.WindowState = System.Windows.WindowState.Normal;
        }
        else if ((DateTime.Now - this.lastWindowFocused).TotalMilliseconds > 250)
        {
          this.VisualSettings = this.mainWindow.GetVisualSettings();
          if (this.VisualSettings != null)
          {
            SerializationHelper.Instance.Save(this.VisualSettings, Path.Combine(SerializationHelper.Instance.ConfigFolder, MainWindow.VisualSettingsFile));
          }

          this.MainWindow.Hide();
        }
        else
        {
          this.BringToFront(false);
        }
      }
      else
      {
        bool firstShow = false;
        if (Application.Current.MainWindow != this.MainWindow)
        {
          Application.Current.MainWindow = this.MainWindow;
          firstShow = true;
        }

        this.MainWindow.Show();
        this.MainWindow.Activate();
        if (firstShow)
        {
          this.MainWindow.FinishDrawing();
        }

        this.MainWindow.IsTreeInitialized = true;
      }
    }

    /// <summary>
    /// Gets the overall build state displayed by the tray icon.
    /// </summary>
    /// <param name="states">Current connector states.</param>
    /// <param name="configuredConnectorCount">Number of configured connectors expected to report a state.</param>
    /// <returns>Failure if any build is failing, unstable if any build is unstable, unknown while statuses are pending, otherwise success.</returns>
    internal static ObservationState GetOverallState(System.Collections.Generic.IEnumerable<ObservationState> states, int configuredConnectorCount)
    {
      var currentStates = states.ToList();
      if (currentStates.Contains(ObservationState.Failure))
      {
        return ObservationState.Failure;
      }

      if (currentStates.Contains(ObservationState.Unstable))
      {
        return ObservationState.Unstable;
      }

      return currentStates.Count < configuredConnectorCount || currentStates.Contains(ObservationState.Unknown) ? ObservationState.Unknown : ObservationState.Success;
    }

    /// <summary>
    /// Gets the state used for the tray icon, falling back to the newest completed build when necessary.
    /// </summary>
    /// <param name="currentState">The current connector state.</param>
    /// <param name="snapshotStates">History states ordered from newest to oldest.</param>
    /// <returns>The current state, or the newest completed history state while the current state is unknown or running.</returns>
    internal static ObservationState GetEffectiveState(ObservationState currentState, System.Collections.Generic.IEnumerable<ObservationState> snapshotStates)
    {
      if (currentState != ObservationState.Unknown && currentState != ObservationState.Running)
      {
        return currentState;
      }

      var completedState = snapshotStates
        .Where(state => state != ObservationState.Unknown && state != ObservationState.Running)
        .Select(state => (ObservationState?)state)
        .FirstOrDefault();
      return completedState ?? currentState;
    }

    /// <summary>
    /// Checks if the notification should be shown.
    /// </summary>
    /// <param name="currentStatus">The current status.</param>
    /// <param name="stateToCheck">The state to check.</param>
    /// <param name="notificationConfiguration">The notification configuration.</param>
    /// <returns>True if the notification should be shown, otherwise false.</returns>
    internal bool CheckNotificationShow(StatusViewModel currentStatus, ObservationState stateToCheck, NotificationConfiguration notificationConfiguration)
    {
      var enabledCheck = this.GetEnabledStateCheck(stateToCheck, notificationConfiguration);
      if (!enabledCheck())
      {
        return false;
      }

      var connectorSnapshots = currentStatus.Parent.ConnectorSnapshots;
      if (connectorSnapshots == null || connectorSnapshots.Count == 1 || notificationConfiguration.OnlyIfChanged == false)
      {
        return currentStatus.State == stateToCheck;
      }

      var historicalOrderedConnectorSnapshots = connectorSnapshots.Reverse().Skip(1);
      foreach (var historicalOrderedConnectorSnapshot in historicalOrderedConnectorSnapshots)
      {
        var snapshotEnabledCheck = this.GetEnabledStateCheck(historicalOrderedConnectorSnapshot.State, notificationConfiguration);
        if (snapshotEnabledCheck() && currentStatus.State == stateToCheck)
        {
          if (historicalOrderedConnectorSnapshot.State != currentStatus.State)
          {
            return true;
          }

          return false;
        }
      }

      return false;
    }

    /// <summary>
    /// Creates a copy of the application icon with a colored status badge.
    /// </summary>
    private static Icon CreateStatusIcon(Icon baseIcon, Color statusColor)
    {
      using (var bitmap = new Bitmap(16, 16))
      {
        using (var graphics = Graphics.FromImage(bitmap))
        {
          graphics.DrawIcon(baseIcon, new Rectangle(0, 0, 16, 16));
          graphics.SmoothingMode = SmoothingMode.AntiAlias;
          using (var outline = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
          using (var badge = new SolidBrush(statusColor))
          {
            graphics.FillEllipse(outline, 7, 7, 9, 9);
            graphics.FillEllipse(badge, 8, 8, 7, 7);
          }
        }

        var iconHandle = bitmap.GetHicon();
        try
        {
          return (Icon)Icon.FromHandle(iconHandle).Clone();
        }
        finally
        {
          DestroyIcon(iconHandle);
        }
      }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>
    /// Called when user attempts to close <see cref="MainWindow"/>. It prevents window being closed and hides it instead.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      this.VisualSettings = this.mainWindow.GetVisualSettings();
    }

    private void MainWindowClosed(object sender, EventArgs e)
    {
      if (this.VisualSettings != null)
      {
        SerializationHelper.Instance.Save(this.VisualSettings, Path.Combine(SerializationHelper.Instance.ConfigFolder, MainWindow.VisualSettingsFile));
      }

      this.mainWindow.Closing -= this.MainWindowClosing;
      this.mainWindow.Closed -= this.MainWindowClosed;
      this.mainWindow = null;
    }

    /// <summary>
    /// Creates context menu whenever it is opened.
    /// </summary>
    private void OnContextMenuPopup(object sender, EventArgs e)
    {
      this.contextMenu.MenuItems.Clear();
      var openCloseWindow = new MenuItem();
      openCloseWindow.Text = this.MainWindowVisible ? "Close window" : "Open window";
      openCloseWindow.Click += (s, args) => this.ShowOrHideWindow();

      var closeApplication = new MenuItem();
      closeApplication.Text = "Exit";
      closeApplication.Click += this.OnCloseApplicationClick;

      this.contextMenu.MenuItems.Add(openCloseWindow);
      this.contextMenu.MenuItems.Add(closeApplication);
    }

    /// <summary>
    /// Handles closing application.
    /// </summary>
    private void OnCloseApplicationClick(object sender, EventArgs e)
    {
      Application.Current.Shutdown();
    }

    /// <summary>
    /// Brings the window to front.
    /// </summary>
    /// <param name="ifVisible">If set to true the window will only be shown if it is already visible, just not on top.</param>
    private void BringToFront(bool ifVisible)
    {
      if (this.MainWindowVisible)
      {
        if (!WindowFinder.IsWindowVisible(this.MainWindow))
        {
          this.lastWindowFocused = DateTime.Now;
        }

        this.MainWindow.Show();
        this.MainWindow.Activate();
      }
      else
      {
        if (ifVisible)
        {
          this.ShowOrHideWindow();
        }
      }
    }

    private void MainWindowConfigurationApplied(object sender, ValueEventArgs<ApplicationConfiguration> e)
    {
      this.configuration = e.Value;
      if (this.scheduler.Running)
      {
        this.scheduler.Stop(false);
      }

      this.scheduler.UnobserveAll();
      foreach (var connectorConfiguration in this.configuration.ConnectorsConfiguration)
      {
        var connector = PluginManager.Instance.GetConnector(connectorConfiguration, true);
        this.scheduler.Observe(connector);
      }

      this.mainWindow?.ApplyConfiguration(this.configuration);
      this.model.PropertyChanged -= this.CurrentStatusPropertyChanged;
      this.model = new NotificationsModel(this.scheduler);
      this.model.PropertyChanged += this.CurrentStatusPropertyChanged;
      this.UpdateTrayIcon();

      this.scheduler.Start();
    }

    /// <summary>
    /// Gets the enabled state check method call.
    /// </summary>
    /// <param name="stateToCheck">The state to check.</param>
    /// <param name="notificationConfiguration">The notification configuration.</param>
    /// <returns>The enabled state check method call.</returns>
    private Func<bool> GetEnabledStateCheck(ObservationState stateToCheck, NotificationConfiguration notificationConfiguration)
    {
      Func<bool> enabledCheck;
      switch (stateToCheck)
      {
        case ObservationState.Unknown:
          enabledCheck = () => notificationConfiguration.UnknownNotificationEnabled;
          break;
        case ObservationState.Unstable:
          enabledCheck = () => notificationConfiguration.UnstableNotificationEnabled;
          break;
        case ObservationState.Failure:
          enabledCheck = () => notificationConfiguration.FailureNotificationEnabled;
          break;
        case ObservationState.Success:
          enabledCheck = () => notificationConfiguration.SuccessNotificationEnabled;
          break;
        case ObservationState.Running:
          enabledCheck = () => notificationConfiguration.RunningNotificationEnabled;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(stateToCheck), stateToCheck, null);
      }

      return enabledCheck;
    }

    /// <summary>
    /// Currents status changed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void CurrentStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (sender is StatusViewModel statusViewModel && (e.PropertyName == nameof(StatusViewModel.State) || e.PropertyName == nameof(ConnectorViewModel.CurrentStatus)))
      {
        this.UpdateTrayIcon();

        if (e.PropertyName != nameof(StatusViewModel.State))
        {
          return;
        }

        var connectorConfiguration = this.configuration.ConnectorsConfiguration.FirstOrDefault(s => s.Identifier == statusViewModel.Parent.Identifier);
        var notificationConfiguration = this.configuration.GetNotificationConfiguration(connectorConfiguration);

        var description = $"Project name: {statusViewModel.Parent.Name}.";
        if (this.CheckNotificationShow(statusViewModel, ObservationState.Running, notificationConfiguration))
        {
          this.ShowBaloon("Build started.", description, System.Windows.Forms.ToolTipIcon.None);
        }
        else if (this.CheckNotificationShow(statusViewModel, ObservationState.Failure, notificationConfiguration))
        {
          this.ShowBaloon("Build failed.", description, System.Windows.Forms.ToolTipIcon.Error);
        }
        else if (this.CheckNotificationShow(statusViewModel, ObservationState.Success, notificationConfiguration))
        {
          this.ShowBaloon("Build successful", description, System.Windows.Forms.ToolTipIcon.Info);
        }
        else if (this.CheckNotificationShow(statusViewModel, ObservationState.Unstable, notificationConfiguration))
        {
          this.ShowBaloon("Build successful (Unstable)", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
        else if (this.CheckNotificationShow(statusViewModel, ObservationState.Unknown, notificationConfiguration))
        {
          this.ShowBaloon("Build interrupted", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
      }
    }

    private void UpdateTrayIcon()
    {
      var connectorStates = this.model.Connectors
        .Select(connector => GetEffectiveState(connector.CurrentStatus.State, connector.ConnectorSnapshots.Select(snapshot => snapshot.State)))
        .ToList();
      var overallState = GetOverallState(connectorStates, this.configuration.ConnectorsConfiguration.Count);
      switch (overallState)
      {
        case ObservationState.Failure:
          this.icon.Icon = this.failureIcon;
          this.icon.Text = Resources.TrayHandler_UpdateTrayIcon_BuildFailing;
          break;
        case ObservationState.Unstable:
          this.icon.Icon = this.unstableIcon;
          this.icon.Text = Resources.TrayHandler_UpdateTrayIcon_Unstable;
          break;
        case ObservationState.Unknown:
          this.icon.Icon = this.loadingIcon;
          this.icon.Text = Resources.TrayHandler_UpdateTrayIcon_Pending;
          break;
        default:
          this.icon.Icon = this.normalIcon;
          this.icon.Text = Resources.TrayHandler_UpdateTrayIcon_Normal;
          break;
      }
    }
  }
}
