// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Windows;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    /// <summary>
    /// The configuration.
    /// </summary>
    private readonly Configuration config;

    public MainWindow()
    {
      this.InitializeComponent();
      this.config = SerializationHelper.LoadConfiguration();

      var themeHelper = new ThemeHelper();
      themeHelper.Initialize();
      themeHelper.ApplyLightDarkMode(this.config.DarkThemeEnabled);
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.ShowDialog();
    }
  }
}
