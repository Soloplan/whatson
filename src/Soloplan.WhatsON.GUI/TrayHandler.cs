namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Windows.Forms;
  using Soloplan.WhatsON.Serialization;
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
    private readonly ApplicationConfiguration configuration;

    /// <summary>
    /// The context menu displayed by tray icon.
    /// </summary>
    private ContextMenu contextMenu;

    /// <summary>
    /// The icon.
    /// </summary>
    private NotifyIcon icon;

    private MainWindow mainWindow;

    private bool allowClosingApplication;

    public TrayHandler(ObservationScheduler scheduler, ApplicationConfiguration configuration)
    {
      this.icon = new System.Windows.Forms.NotifyIcon();
      this.icon.Icon = Properties.Resources.whatsONx16;
      this.icon.Visible = true;
      this.scheduler = scheduler;
      this.configuration = configuration;

      this.contextMenu = new ContextMenu();
      this.icon.ContextMenu = this.contextMenu;
      this.contextMenu.Popup += this.OnContextMenuPopup;
      this.icon.DoubleClick += (s, e) => this.ShowOrHideWindow();
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
          this.mainWindow = new MainWindow(this.scheduler, this.configuration);
          this.mainWindow.Closing += this.MainWindowClosing;
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
      this.icon.ShowBalloonTip(1000, title, tipText, icon);
    }

    /// <summary>
    /// Called when user attempts to close <see cref="MainWindow"/>. It prevents window being closed and hides it instead.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!this.allowClosingApplication)
      {
        e.Cancel = true;
        this.ShowOrHideWindow();
      }
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
      this.allowClosingApplication = true;
      Application.Current.Shutdown();
    }

    /// <summary>
    /// Handles showing and hiding main window.
    /// </summary>
    private void ShowOrHideWindow()
    {
      if (this.MainWindowVisible)
      {
        this.MainWindow.Hide();
        Application.Current.MainWindow = null;
      }
      else
      {
        System.Windows.Application.Current.MainWindow = this.MainWindow;
        this.MainWindow.Show();
        this.MainWindow.Activate();
      }
    }
  }
}