// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainConfigPage.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Windows;
  using System.Windows.Controls;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Interaction logic for MainConfigPage.xaml.
  /// </summary>
  public partial class MainConfigPage : Page
  {
    public MainConfigPage(ConfigViewModel configurationViewModel)
    {
      this.DataContext = configurationViewModel;
      this.InitializeComponent();
    }

    /// <summary>
    /// Previews the selected appearance without saving other settings.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private void ThemeModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
      if ((sender as ComboBox)?.SelectedValue is ApplicationTheme mode && Application.Current is App app)
      {
        app.ApplyTheme(mode);
      }
    }

    private void LanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
      if ((sender as ComboBox)?.SelectedValue is ApplicationLanguage language)
      {
        Localizer.ApplyLanguage(language);
      }
    }
  }
}
