// <copyright file="WizardWindow.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Soloplan.WhatsON.GUI.Config.Wizard
{
  using System;
  using System.Windows;

  /// <summary>
  /// Interaction logic for WizardWindow.xaml
  /// </summary>
  public partial class WizardWindow : Window
  {
    /// <summary>
    /// The wizard controller.
    /// </summary>
    private readonly WizardController wizardController;

    /// <summary>
    /// The window shown flag.
    /// </summary>
    private bool windowShown;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardWindow" /> class.
    /// </summary>
    /// <param name="wizardController">The wizard controller.</param>
    public WizardWindow(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.DataContext = wizardController;
      this.InitializeComponent();
    }

    /// <summary>
    /// Raises the <see cref="E:ContentRendered" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      if (this.windowShown)
      {
        return;
      }

      this.windowShown = true;
      var app = (App)Application.Current;
      app.ApplyTheme(app.IsDarkThemeEnabled);
    }

    /// <summary>
    /// Next page click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void NextClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.GoToNextPage();
    }

    /// <summary>
    /// Previous page click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void PrevClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.GoToPrevPage();
    }

    /// <summary>
    /// Finish click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void FinishClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.Finish();
    }
  }
}
