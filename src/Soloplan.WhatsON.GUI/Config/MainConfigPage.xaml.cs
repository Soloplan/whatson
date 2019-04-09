// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Interaction logic for MainConfigPage.xaml
  /// </summary>
  public partial class MainConfigPage : Page
  {
    /// <summary>
    /// The Theme helper.
    /// </summary>
    private readonly ThemeHelper themeHelper;

    public MainConfigPage(ConfigViewModel configurationViewModel, ThemeHelper themeHelper)
    {
      this.themeHelper = themeHelper;
      this.DataContext = configurationViewModel;
      this.InitializeComponent();
    }

    /// <summary>
    /// Handles the Checked event of the ToggleButton control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void LightOrDarkModeToggleButtonChecked(object sender, RoutedEventArgs e)
    {
      this.SwitchLightDarkMode((ToggleButton)e.Source);
    }

    /// <summary>
    /// Handles the Unchecked event of the ToggleButton control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void LightOrDarkModeToggleButtonUnchecked(object sender, RoutedEventArgs e)
    {
      this.SwitchLightDarkMode((ToggleButton)e.Source);
    }

    /// <summary>
    /// Switches the color shame to light or dark.
    /// </summary>
    /// <param name="lightDarkToggleButton">The light dark toggle button.</param>
    private void SwitchLightDarkMode(ToggleButton lightDarkToggleButton)
    {
      var isDark = lightDarkToggleButton.IsChecked ?? false;
      this.themeHelper.ApplyLightDarkMode(isDark);
    }
  }
}
