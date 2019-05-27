namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Drawing;
  using System.IO;
  using System.Linq;
  using System.Windows.Forms;
  using NLog;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Serialization;
  using Application = System.Windows.Application;

  /// <summary>
  /// Wrapper for <see cref="NotifyIcon"/> from System.WindowsForms. Handles creation/opening/closing of main window.
  /// </summary>
  public class TrayHandler : IDisposable
  {
    private const string VisualSettingsFile = "VisualSettingsFile.json";

    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

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

    private MainWindow mainWindow;

    private NotificationsModel model;

    private MainWindowSettigns visualSettings;

    public TrayHandler(ObservationScheduler scheduler, ApplicationConfiguration configuration)
    {
      this.icon = new System.Windows.Forms.NotifyIcon();
      this.icon.Icon = new Icon(Properties.Resources.Whatson, new Size(16,16));
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

      if (File.Exists(Path.Combine(SerializationHelper.ConfigFolder, VisualSettingsFile)))
      {
        this.visualSettings = SerializationHelper.Load<MainWindowSettigns>(Path.Combine(SerializationHelper.ConfigFolder, VisualSettingsFile));
      }

      if (!this.configuration.OpenMinimized)
      {
        this.ShowOrHideWindow();
      }
    }

    /// <summary>
    /// Gets a value indicating whether the main window is visible.
    /// </summary>
    private bool MainWindowVisible => Application.Current.MainWindow is MainWindow;

    /// <summary>
    /// Gets instance of <see cref="MainWindow"/>.
    /// </summary>
    private MainWindow MainWindow
    {
      get
      {
        if (this.mainWindow == null)
        {
          this.mainWindow = new MainWindow(this.scheduler, this.configuration, this.model.Subjects.Select(sub => sub.Subject).ToList());

          this.mainWindow.ApplyVisualSettings(this.visualSettings);

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
    }

    /// <summary>
    /// Shows balloon info.
    /// </summary>
    /// <param name="title">Title of balloon.</param>
    /// <param name="tipText">Text of the balloon.</param>
    /// <param name="icon">Icon.</param>
    public void ShowBaloon(string title, string tipText, ToolTipIcon icon)
    {
      tipText = string.IsNullOrEmpty(tipText) ? " " : tipText;

      log.Info("Showing notification: {notification},", new { Title = title, Text = tipText, Icon = icon });
      this.icon.ShowBalloonTip(1000, title, tipText, icon);
    }

    /// <summary>
    /// Called when user attempts to close <see cref="MainWindow"/>. It prevents window being closed and hides it instead.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      this.visualSettings = this.mainWindow.GetVisualSettigns();
    }

    private void MainWindowClosed(object sender, EventArgs e)
    {
      if (this.visualSettings != null)
      {
        SerializationHelper.Save(this.visualSettings, Path.Combine(SerializationHelper.ConfigFolder, VisualSettingsFile));
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
    /// Handles showing and hiding main window.
    /// </summary>
    private void ShowOrHideWindow()
    {
      if (this.MainWindowVisible)
      {
        this.MainWindow.Close();
      }
      else
      {
        Application.Current.MainWindow = this.MainWindow;
        this.MainWindow.Show();
        this.MainWindow.Activate();
        this.MainWindow.FinishDrawing();
        this.MainWindow.IsTreeInitialized = true;
      }
    }

    /// <summary>
    /// Brings the window to front.
    /// </summary>
    /// <param name="ifVisible">If set to true the window will only be shown if it is already visible, just not on top.</param>
    private void BringToFront(bool ifVisible)
    {
      if (this.MainWindowVisible)
      {
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
      var schedulerRunning = this.scheduler.Running;
      if (this.scheduler.Running)
      {
        this.scheduler.Stop(false);
      }

      this.scheduler.UnobserveAll();
      foreach (var subjectConfiguration in this.configuration.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        this.scheduler.Observe(subject);
      }

      this.mainWindow?.ApplyConfiguration(this.configuration);
      this.model.PropertyChanged -= this.CurrentStatusPropertyChanged;
      this.model = new NotificationsModel(this.scheduler);
      this.model.PropertyChanged += this.CurrentStatusPropertyChanged;

      if (schedulerRunning)
      {
        this.scheduler.Start();
      }
    }

    private void CurrentStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (sender is StatusViewModel statusViewModel && e.PropertyName == nameof(StatusViewModel.State))
      {
        var description = $"Project name: {statusViewModel.Parent.Name}.";
        if (statusViewModel.State == ObservationState.Running)
        {
          this.ShowBaloon("Build started.", description, System.Windows.Forms.ToolTipIcon.None);
        }
        else if (statusViewModel.State == ObservationState.Failure)
        {
          this.ShowBaloon("Build failed.", description, System.Windows.Forms.ToolTipIcon.Error);
        }
        else if (statusViewModel.State == ObservationState.Success)
        {
          this.ShowBaloon("Build successful", description, System.Windows.Forms.ToolTipIcon.Info);
        }
        else if (statusViewModel.State == ObservationState.Unstable)
        {
          this.ShowBaloon("Build successful (Unstable)", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
        else if (statusViewModel.State == ObservationState.Unknown)
        {
          this.ShowBaloon("Build interrupted", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
      }
    }
  }
}