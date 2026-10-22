// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LiveLocalizationChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Threading;
  using MaterialDesignThemes.Wpf;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.View;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  internal static class LiveLocalizationChecks
  {
    internal static void Verify(App app)
    {
      var previousCulture = CultureInfo.CurrentCulture;
      var previousUiCulture = CultureInfo.CurrentUICulture;
      var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
      var previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
      try
      {
        VerifyLanguageChanges(app);
      }
      finally
      {
        Localizer.ApplyLanguage(previousUiCulture.TwoLetterISOLanguageName == "de" ? ApplicationLanguage.German : ApplicationLanguage.English);
        CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUiCulture;
        CultureInfo.CurrentCulture = previousCulture;
        CultureInfo.CurrentUICulture = previousUiCulture;
      }
    }

    private static void VerifyLanguageChanges(App app)
    {
      Localizer.ApplyLanguage(ApplicationLanguage.System);
      var systemCulture = CultureInfo.CurrentUICulture;
      Localizer.ApplyLanguage(ApplicationLanguage.English);
      foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
      {
        app.ApplyTheme(theme);
        var source = new ApplicationConfiguration { Language = ApplicationLanguage.English };
        source.ConnectorsConfiguration.Add(new ConnectorConfiguration(
          "Jenkins", "User project name", Connector.ServerAddress, "https://build.example.invalid", Connector.ProjectName, "main"));
        var settings = new ConfigWindow(source, new WindowSettings()) { Left = -20000, Top = -20000, ShowActivated = false };
        var projectSettings = new ConfigWindow(source, new WindowSettings()) { Left = -20000, Top = -20000, ShowActivated = false };
        var controller = new WizardController(null, source, new WindowSettings());
        var wizard = new WizardWindow(controller) { Left = -20000, Top = -20000, ShowActivated = false };
        var history = new HistoryBuildTooltipControl
        {
          DataContext = new { Time = DateTime.Now.AddMinutes(-5), FinishTime = DateTime.Now.AddMinutes(-5), Duration = TimeSpan.FromMinutes(1) },
        };
        var tooltip = new ToolTip { Content = history };
        using var tray = new TrayHandler(new ObservationScheduler(), new ApplicationConfiguration());
        try
        {
          settings.Show();
          projectSettings.Show();
          ((ListBox)projectSettings.FindName("ConfigTopicsListBox")).SelectedIndex = 1;
          Flush(projectSettings);
          var projects = (ConnectorsPage)((Frame)projectSettings.FindName("ConfigFrame")).Content;
          var connectorEditor = (ConnectorConfigPage)((Frame)projects.FindName("uxConnectorFrame")).Content;
          var general = (Page)((Frame)settings.FindName("ConfigFrame")).Content;
          var selector = (ComboBox)general.FindName("LanguageSelector");
          var connection = new ConnectionWizardPage(controller);
          wizard.Frame.Content = connection;
          wizard.Show();
          var address = (ComboBox)connection.FindName("AddressComboBox");
          address.Text = "https://typed.example.invalid/";
          var selection = new ProjectSelectionWizardPage(controller) { DataContext = controller };
          wizard.Frame.Content = selection;
          Flush(settings);
          var grouping = (ComboBox)selection.FindName("uxGrouppingSettings");
          grouping.SelectedIndex = 1;
          var selectedGrouping = grouping.SelectedItem;
          Descendants<Expander>(connectorEditor).Single().IsExpanded = true;
          Flush(projectSettings);
          var jobName = Descendants<TextBox>(connectorEditor).Single(text => text.DataContext is ConfigurationItemViewModel item && item.Key == Connector.ProjectName);
          jobName.Text = "edited/job";
          var notificationCaption = Descendants<Label>(connectorEditor).Single(label => Equals(label.Content, Strings.UseMainConfiguration));
          var project = (Configuration.ViewModel.ConnectorViewModel)connectorEditor.DataContext;
          var notificationValue = project.GetConfigurationByKey(Connector.NotificationsVisbility).Value;
          tooltip.PlacementTarget = selector;
          tooltip.IsOpen = true;
          Flush(settings);
          var relativeTime = Descendants<TextBlock>(history).Single(text => text.Text.Contains("ago", StringComparison.Ordinal));
          var icon = (typeof(TrayHandler).GetField("icon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).GetValue(tray) as System.Windows.Forms.NotifyIcon ?? throw new AssertionException("Expected icon.");
          var menu = (typeof(TrayHandler).GetField("contextMenu", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).GetValue(tray) as System.Windows.Forms.ContextMenuStrip ?? throw new AssertionException("Expected menu.");
          var flags = BindingFlags.Instance | BindingFlags.NonPublic;
          (typeof(TrayHandler).GetField("nextUpdateCheck", flags) ?? throw new AssertionException("Expected update check field.")).SetValue(tray, DateTime.MaxValue);
          (typeof(TrayHandler).GetField("updateRestartReadyAt", flags) ?? throw new AssertionException("Expected restart readiness field.")).SetValue(tray, 0L);
          (typeof(TrayHandler).GetMethod("SetUpdateAvailability", flags) ?? throw new AssertionException("Expected update method.")).Invoke(
            tray, new object[] { Windows.ApplicationModel.PackageUpdateAvailability.Available, new Uri("https://updates.example.invalid/whatsON.appinstaller") });

          foreach (var language in new[] { ApplicationLanguage.German, ApplicationLanguage.English, ApplicationLanguage.German, ApplicationLanguage.System })
          {
            selector.SelectedValue = language;
            Flush(settings);
            var german = language == ApplicationLanguage.German || (language == ApplicationLanguage.System && systemCulture.TwoLetterISOLanguageName == "de");
            Assert.That(settings.Title, Is.EqualTo(german ? "Einstellungen" : "Settings"));
            var header = Descendants<CustomTitleBar>(settings).Single();
            Assert.That(((TextBlock)header.FindName("WindowTitle")).Text, Is.EqualTo(settings.Title), "The visible custom header must follow the translated window title.");
            Assert.That(wizard.Title, Is.EqualTo(german ? "Projekte hinzufügen" : "Add projects"));
            Assert.That(((Button)wizard.FindName("NextButton")).Content, Is.EqualTo(german ? "Weiter" : "Next"));
            Assert.That(HintAssist.GetHint(jobName), Is.EqualTo(german ? "Projektname" : "Project name"));
            Assert.That(notificationCaption.Content, Is.EqualTo(Strings.UseMainConfiguration));
            relativeTime = Descendants<TextBlock>(history).Single(text => text.Text.Contains(german ? "vor" : "ago", StringComparison.Ordinal));
            Assert.That(relativeTime.Text, Does.Contain(german ? "vor" : "ago"));
            Assert.That(relativeTime.Text, german ? Does.EndWith("beendet") : Does.StartWith("Finished"));
            Assert.That(((GrouppingSetting)selectedGrouping).Caption, Is.EqualTo(Strings.AddHierarchyToProjectName));
            Assert.That(grouping.SelectedItem, Is.SameAs(selectedGrouping));
            Assert.That(((Frame)settings.FindName("ConfigFrame")).Content, Is.SameAs(general));
            Assert.That(((Frame)projects.FindName("uxConnectorFrame")).Content, Is.SameAs(connectorEditor));
            Assert.That(jobName.Text, Is.EqualTo("edited/job"));
            Assert.That(address.Text, Is.EqualTo("https://typed.example.invalid/"));
            Assert.That(project.GetConfigurationByKey(Connector.NotificationsVisbility).Value, Is.EqualTo(notificationValue));
            Assert.That(source.Language, Is.EqualTo(ApplicationLanguage.English), "Previewing must not save the preference.");
            Assert.That(icon.Text, Is.EqualTo(german ? "whatsON - Keine Projekte konfiguriert" : "whatsON - No projects configured"));
            Assert.That(menu.Items.Count, Is.EqualTo(3));
            Assert.That(menu.Items[1].Text, Is.EqualTo(german ? "Zum Aktualisieren neu starten" : "Restart to update"));
            Assert.That(menu.Items[2].Text, Is.EqualTo(german ? "Beenden" : "Exit"));
            Assert.That(settings.Language.IetfLanguageTag, Is.EqualTo(CultureInfo.CurrentUICulture.IetfLanguageTag).IgnoreCase);
            Assert.That(tooltip.Language, Is.EqualTo(settings.Language));
          }

          selector.SelectedValue = ApplicationLanguage.German;
          var exported = new ApplicationConfiguration();
          settings.ConfigurationViewModel.ApplyToConfiguration(exported);
          Assert.That(JsonConvert.DeserializeObject<ApplicationConfiguration>(JsonConvert.SerializeObject(exported))?.Language, Is.EqualTo(ApplicationLanguage.German));
          ((Button)settings.FindName("ResetButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
          Flush(settings);
          Assert.That(settings.Title, Is.EqualTo("Settings"));
          Assert.That(selector.SelectedValue, Is.EqualTo(ApplicationLanguage.English));
          Assert.That(settings.ConfigurationViewModel.ConfigurationIsModified, Is.False);
          selector.SelectedValue = ApplicationLanguage.German;
          settings.Close();
          Flush(wizard);
          Assert.That(wizard.Title, Is.EqualTo("Add projects"), "Closing unsaved settings must restore the saved language.");
        }
        finally
        {
          tooltip.IsOpen = false;
          wizard.Close();
          projectSettings.Close();
          settings.Close();
        }
      }
    }

    private static void Flush(Window window)
    {
      window.UpdateLayout();
      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent)
      where T : DependencyObject
    {
      for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
      {
        var child = VisualTreeHelper.GetChild(parent, index);
        if (child is T match)
        {
          yield return match;
        }

        foreach (var descendant in Descendants<T>(child))
        {
          yield return descendant;
        }
      }
    }
  }
}
