// <copyright file="CustomTitleBar.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using MaterialDesignThemes.Wpf;
  using NLog;

  /// <summary>
  /// Control used instead of standard window title bar.
  /// </summary>
  public partial class CustomTitleBar : UserControl
  {
    /// <summary>
    /// Dependency property for <see cref="ShowMinimizeButton"/>.
    /// </summary>
    public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(CustomTitleBar));

    /// <summary>
    /// Dependency property for <see cref="Window"/>.
    /// </summary>
    public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(nameof(Window), typeof(Window), typeof(CustomTitleBar));

    /// <summary>
    /// Dependency property for <see cref="Text"/>.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(CustomTitleBar));

    /// <summary>
    /// Dependency property for <see cref="CustomButtonIcon"/>.
    /// </summary>
    public static readonly DependencyProperty CustomButtonIconProperty = DependencyProperty.Register(nameof(CustomButtonIcon), typeof(PackIconKind), typeof(CustomTitleBar));

    /// <summary>
    /// Dependency property for <see cref="CustomButtonVisible"/>.
    /// </summary>
    public static readonly DependencyProperty CustomButtonVisibleProperty = DependencyProperty.Register(nameof(CustomButtonVisible), typeof(bool), typeof(CustomTitleBar));

    /// <summary>
    /// Logger used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTitleBar"/> class.
    /// </summary>
    public CustomTitleBar()
    {
      this.InitializeComponent();
      this.DataContext = this;
    }

    /// <summary>
    /// Called when custom button was clicked.
    /// </summary>
    public event EventHandler ButtonClicked;

    public event EventHandler AddConnectorClicked;

    public event EventHandler AddGroupClicked;

    /// <summary>
    /// Gets or sets the window handled by this instance.
    /// </summary>
    public Window Window
    {
      get => (Window)this.GetValue(WindowProperty);
      set => this.SetValue(WindowProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether minimize button should be shown.
    /// </summary>
    public bool ShowMinimizeButton
    {
      get => (bool)this.GetValue(ShowMinimizeButtonProperty);
      set => this.SetValue(ShowMinimizeButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets text displayed on title bar.
    /// </summary>
    /// <remarks>
    /// By default the property is set to <see cref="Window.Title"/> when <see cref="Window"/> is assigned.
    /// </remarks>
    public string Text
    {
      get => (string)this.GetValue(TextProperty);
      set => this.SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets icon of custom button (located on the left of <see cref="CustomTitleBar"/>).
    /// </summary>
    public PackIconKind CustomButtonIcon
    {
      get => (PackIconKind)this.GetValue(CustomButtonIconProperty);
      set => this.SetValue(CustomButtonIconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether custom button should be visible.
    /// </summary>
    public bool CustomButtonVisible
    {
      get => (bool)this.GetValue(CustomButtonVisibleProperty);
      set => this.SetValue(CustomButtonVisibleProperty, value);
    }

    /// <summary>Invoked whenever the effective value of any dependency property on this <see cref="T:System.Windows.FrameworkElement" /> has been updated. The specific dependency property that changed is reported in the arguments parameter. Overrides <see cref="M:System.Windows.DependencyObject.OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs)" />.</summary>
    /// <param name="e">The event data that describes the property that changed, as well as old and new values.</param>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
      if (e.Property.Name == nameof(this.Window) && this.EnsureWindow())
      {
        this.Text = this.Window.Title;
      }

      base.OnPropertyChanged(e);
    }

    /// <summary>
    /// Main window bar mouse down.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
    private void MainWindowBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (!this.EnsureWindow())
      {
        return;
      }

      if (e.ChangedButton != MouseButton.Left || e.Handled || e.ButtonState == MouseButtonState.Released)
      {
        return;
      }

      if (e.ClickCount == 2)
      {
        this.Window.WindowState = this.Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
      }
      else
      {
        this.Window.DragMove();
      }
    }

    /// <summary>
    /// Minimizes the <see cref="Window"/>.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
    private void MinimizeButonClick(object sender, RoutedEventArgs e)
    {
      if (!this.EnsureWindow())
      {
        return;
      }

      this.Window.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Closes the <see cref="Window"/>.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
      if (!this.EnsureWindow())
      {
        return;
      }

      this.Window.Close();
    }

    /// <summary>
    /// Verifies that <see cref="Window"/> is set.
    /// </summary>
    /// <returns>True if the window is set; false otherwise.</returns>
    private bool EnsureWindow()
    {
      if (this.Window == null)
      {
        log.Error("Window property is not set. The control can't function properly");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Handles click on custom button. Calls <see cref="ButtonClicked"/>.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void OnButtonClicked(object sender, MouseButtonEventArgs e)
    {
      this.ButtonClicked?.Invoke(sender, e);
    }

    private void NewGroupClick(object sender, RoutedEventArgs e)
    {
      this.AddGroupClicked?.Invoke(sender, e);
    }

    private void NewConnectorClick(object sender, RoutedEventArgs e)
    {
      this.AddConnectorClicked?.Invoke(sender, e);
    }
  }
}
