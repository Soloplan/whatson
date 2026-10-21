// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerConfigPage.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Documents;
  using System.Windows.Media;
  using Soloplan.WhatsON.GUI.Common.Localization;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Localization;

  public class ServerConfigPage : Page
  {
    public ServerConfigPage(ConfigViewModel configuration)
    {
      this.FontFamily = new FontFamily("Segoe UI");
      this.FontSize = 14;
      this.Background = Brushes.Transparent;
      this.SetResourceReference(TextElement.ForegroundProperty, "MaterialDesignBody");
      configuration.RefreshServers();
      var panel = new DockPanel();
      var heading = new TextBlock { Style = (Style)this.FindResource("SettingsHeading") };
      heading.SetBinding(TextBlock.TextProperty, TextExtension.CreateBinding(() => Strings.Servers));
      DockPanel.SetDock(heading, Dock.Top);
      panel.Children.Add(heading);
      var hint = new TextBlock { Style = (Style)this.FindResource("SettingsIntro") };
      hint.SetBinding(TextBlock.TextProperty, TextExtension.CreateBinding(() => Strings.ServerCredentialsDescription));
      DockPanel.SetDock(hint, Dock.Top);
      panel.Children.Add(hint);
      var servers = new ComboBox
      {
        ItemsSource = configuration.Servers,
        DisplayMemberPath = nameof(ConnectorViewModel.Name),
        MinHeight = 40,
      };
      servers.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, TextExtension.CreateBinding(() => Strings.ServerCredentials));
      var serverCard = new Border { Style = (Style)this.FindResource("SettingsCard"), Child = servers };
      DockPanel.SetDock(serverCard, Dock.Top);
      panel.Children.Add(serverCard);
      var editor = new Frame { NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden, VerticalAlignment = VerticalAlignment.Top };
      panel.Children.Add(editor);
      servers.SelectionChanged += (_, _) => editor.Content = new ConnectorConfigPage(servers.SelectedItem as ConnectorViewModel, true);
      servers.SelectedIndex = configuration.Servers.Count == 0 ? -1 : 0;
      this.Content = panel;
    }
  }
}
