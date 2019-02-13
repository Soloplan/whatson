// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Windows;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    /// <summary>
    /// The configuration.
    /// </summary>
    private Configuration config;

    public MainWindow()
    {
      this.InitializeComponent();
      this.config = SerializationHelper.LoadConfiguration();
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      // TODO
    }
  }
}
