// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DesktopRuntimeTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Markup;
  using System.Windows.Threading;
  using NLog;
  using NLog.Config;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.View;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class DesktopRuntimeTests
  {
    [Test]
    [Apartment(ApartmentState.STA)]
    public void ApplicationResourcesAndBuiltInPluginsLoadOnDesktopRuntime()
    {
      // WPF queues startup even without Run; keep rendering isolated from real servers.
      var app = new ResourceTestApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
      app.Resources =
        (ResourceDictionary)Application.LoadComponent(new Uri("/Soloplan.WhatsON.GUI;component/ApplicationResources.xaml", UriKind.Relative));
      app.ApplyTheme(ApplicationTheme.System);
      Assert.That(
        System.Windows.Forms.Application.ColorMode,
        Is.EqualTo(app.IsDarkThemeEnabled ? System.Windows.Forms.SystemColorMode.Dark : System.Windows.Forms.SystemColorMode.Classic));
      app.ApplyTheme(ApplicationTheme.Light);
      Assert.That(app.IsDarkThemeEnabled, Is.False);
      Assert.That(System.Windows.Forms.Application.ColorMode, Is.EqualTo(System.Windows.Forms.SystemColorMode.Classic));
      app.ApplyTheme(ApplicationTheme.Dark);
      Assert.That(app.IsDarkThemeEnabled, Is.True);
      Assert.That(System.Windows.Forms.Application.ColorMode, Is.EqualTo(System.Windows.Forms.SystemColorMode.Dark));
      Assert.That(app.Resources["MaterialDesignPaper"], Is.Not.Null);

      var plugins = PluginManager.Instance.Plugins;
      Assert.That(plugins.OfType<ConnectorPlugin>().Select(plugin => plugin.Name), Is.EquivalentTo(["Jenkins", "CruiseControl"]));
      var presentationPlugins = plugins.OfType<PresentationPlugin>().ToList();
      Assert.That(presentationPlugins, Has.Count.EqualTo(2));
      foreach (var plugin in presentationPlugins)
      {
        using (var reader = plugin.GetDataTempletXaml())
        {
          var resources = (ResourceDictionary)XamlReader.Load(reader);
          var templates = resources.Values.OfType<DataTemplate>().ToList();
          Assert.That(templates, Is.Not.Empty, plugin.ConnectorType);
          foreach (var template in templates)
          {
            Assert.That(template.LoadContent(), Is.Not.Null, plugin.ConnectorType);
          }
        }
      }

      var scheduler = new ObservationScheduler();
      var configuration = new ApplicationConfiguration();
      var connectors = new List<Connector>();
      var window = new MainWindow(scheduler, configuration, connectors);
      try
      {
        ((ConnectorsTreeView)window.FindName("mainTreeView")).Init(scheduler, configuration, connectors);
        Assert.That(window.Content, Is.Not.Null);
        Assert.That(window.Style, Is.Not.Null);
      }
      finally
      {
        window.Close();
      }

      var preferences = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light };
      var settings = new ConfigWindow(preferences, new WindowSettings());
      try
      {
        var sections = (ListBox)settings.FindName("ConfigTopicsListBox");
        var frame = (Frame)settings.FindName("ConfigFrame");
        foreach (var mode in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
        {
          app.ApplyTheme(mode);
          foreach (var section in new[] { 0, 1, 2 })
          {
            sections.SelectedIndex = section;
            settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.That(frame.Content, Is.InstanceOf<Page>());
            var page = (Page)frame.Content;
            page.Measure(new Size(700, 650));
            page.Arrange(new Rect(0, 0, 700, 650));
            page.UpdateLayout();
            Assert.That(page.ActualWidth, Is.EqualTo(700));
            if (page is ConnectorsPage)
            {
              Assert.That(((Border)page.FindName("EmptyProjectsState")).Visibility, Is.EqualTo(Visibility.Visible));
              Assert.That(((Button)page.FindName("DeleteButton")).IsEnabled, Is.False);
            }
          }
        }

        VerifySettingsPageEdges(settings, sections, frame);
        sections.SelectedIndex = 0;
        settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var selector = (ComboBox)((Page)frame.Content).FindName("ThemeModeSelector");
        selector.SelectedValue = ApplicationTheme.Dark;
        Assert.That(app.IsDarkThemeEnabled, Is.True);
        Assert.That(settings.ConfigurationViewModel.ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
        Assert.That(preferences.ThemeMode, Is.EqualTo(ApplicationTheme.Light));

        ((Button)settings.FindName("ResetButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.That(app.IsDarkThemeEnabled, Is.False);
        Assert.That(selector.SelectedValue, Is.EqualTo(ApplicationTheme.Light));
        selector.SelectedValue = ApplicationTheme.System;
        Assert.That(app.CurrentThemeMode, Is.EqualTo(ApplicationTheme.System));
        Assert.That(app.IsDarkThemeEnabled, Is.EqualTo(ThemeHelper.SystemUsesDarkMode()));
        selector.SelectedValue = ApplicationTheme.Dark;
      }
      finally
      {
        settings.Close();
      }

      Assert.That(app.IsDarkThemeEnabled, Is.False, "Closing without saving must restore the saved appearance.");

      var projectSettings = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light };
      projectSettings.ConnectorsConfiguration.Add(
        new ConnectorConfiguration(
          "Jenkins",
          "Application build",
          [
            new ConfigurationItem(Connector.ServerAddress) { Value = "https://ci.example.invalid" },
            new ConfigurationItem(Connector.ProjectName) { Value = "main" },
          ]));
      var populatedSettings = new ConfigWindow(projectSettings, new WindowSettings());
      try
      {
        ((ListBox)populatedSettings.FindName("ConfigTopicsListBox")).SelectedIndex = 1;
        populatedSettings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var page = (Page)((Frame)populatedSettings.FindName("ConfigFrame")).Content;
        page.Measure(new Size(550, 450));
        page.Arrange(new Rect(0, 0, 550, 450));
        page.UpdateLayout();
        Assert.That(((ComboBox)page.FindName("uxConnectors")).SelectedItem, Is.Not.Null);
        Assert.That(((Border)page.FindName("EmptyProjectsState")).Visibility, Is.EqualTo(Visibility.Collapsed));
        Assert.That(((Button)page.FindName("DeleteButton")).IsEnabled, Is.True);
        Assert.That(((Frame)page.FindName("uxConnectorFrame")).Content, Is.InstanceOf<ConnectorConfigPage>());
      }
      finally
      {
        populatedSettings.Close();
      }

      VerifyTitleBarActionsAndLayout();
      VerifyScrollSpacing();
      VerifyWizardLayouts(app);
      ProjectPickerChecks.Verify(app);
      VerifyWizardNavigationNotifications();
      ProjectDiscoveryTests.VerifyWizardNextButton();
      VerifyGermanViews(app);
      VerifyNotificationBindings();
      VerifyTrayNotificationForwarding();
      ProjectListChecks.Verify(app);
      ToolTipChecks.Verify(app);
      LiveLocalizationChecks.Verify(app);
      VerifyHandledExceptionsAreNotLoggedAsErrors();

      using (var stream = Application.GetResourceStream(new Uri("/Soloplan.WhatsON.GUI;component/Logging/LogConfig.xml", UriKind.Relative)).Stream)
      using (var reader = new StreamReader(stream))
      using (var factory = new LogFactory())
      {
        var loggingConfiguration = XmlLoggingConfiguration.CreateFromXmlString(reader.ReadToEnd(), factory);
        Assert.That(loggingConfiguration.AllTargets.Select(target => target.Name), Is.EquivalentTo(["whatsONMainLog", "whatsONConsoleLog"]));
      }
    }

    private static void VerifyHandledExceptionsAreNotLoggedAsErrors()
    {
      var previous = LogManager.Configuration;
      var target = new NLog.Targets.MemoryTarget("handled-exceptions");
      var configuration = new LoggingConfiguration();
      configuration.AddRule(LogLevel.Error, LogLevel.Fatal, target, typeof(Logging.ExceptionHandlingInitialization).FullName);
      try
      {
        LogManager.Configuration = configuration;
        Logging.ExceptionHandlingInitialization.Initialize();

        Assert.Throws<System.Threading.Tasks.TaskCanceledException>(() => throw new System.Threading.Tasks.TaskCanceledException("Handled request cancellation"));
        Assert.Throws<System.Net.WebException>(() => throw new System.Net.WebException("Handled request timeout", System.Net.WebExceptionStatus.Timeout));

        Assert.That(target.Logs, Is.Empty, "Handled network failures must not be logged as unhandled application errors.");
      }
      finally
      {
        LogManager.Configuration = previous;
      }
    }

    private static void VerifyWizardNavigationNotifications()
    {
      var wizard = new WizardController(null, new ApplicationConfiguration(), new WindowSettings());
      var changes = new List<string>();
      wizard.PropertyChanged += (sender, args) => changes.Add(args.PropertyName ?? throw new AssertionException("Expected a property name."));
      var currentPage = typeof(WizardController).GetProperty(
        "CurrentPage",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.");
      currentPage.SetValue(wizard, new ProjectSelectionWizardPage(wizard));
      Assert.That(wizard.IsLastStep, Is.True);
      Assert.That(wizard.IsPreviousStepEnabled, Is.True);
      Assert.That(
        changes,
        Does.Contain(nameof(wizard.IsLastStep)).And.Contain(nameof(wizard.IsNotLastStep)).And.Contain(nameof(wizard.IsPreviousStepEnabled)));
      changes.Clear();
      currentPage.SetValue(wizard, new ConnectionWizardPage(wizard));
      Assert.That(wizard.IsLastStep, Is.False);
      Assert.That(wizard.IsPreviousStepEnabled, Is.False);
      Assert.That(
        changes,
        Does.Contain(nameof(wizard.IsLastStep)).And.Contain(nameof(wizard.IsNotLastStep)).And.Contain(nameof(wizard.IsPreviousStepEnabled)));
    }

    private static void VerifyTrayNotificationForwarding()
    {
      var notifications = new NotificationsModel(new ObservationScheduler());
      var connector = PluginManager.Instance.GetConnector(
        new ConnectorConfiguration(
          "Jenkins",
          "Main",
          [
            new ConfigurationItem(Connector.ServerAddress) { Value = "https://build.example.invalid" },
            new ConfigurationItem(Connector.ProjectName) { Value = "main" },
          ])) ?? throw new AssertionException("Expected connector plugin was not found.");
      connector.CurrentStatus = new Status { Name = "Main", State = ObservationState.Success };
      var queryCompleted = typeof(NotificationsModel).GetMethod(
        "SchedulerStatusQueried",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.");
      var changes = new List<(object? Sender, string? Property)>();
      notifications.StatusChanged += (sender, args) => changes.Add((sender, args.PropertyName));
      for (var iteration = 0; iteration < 3; iteration++)
      {
        queryCompleted.Invoke(notifications, [null, connector]);
        Assert.That(changes.Last().Sender, Is.SameAs(notifications.Connectors.Single().CurrentStatus));
        Assert.That(changes.Last().Property, Is.EqualTo(nameof(ConnectorViewModel.CurrentStatus)));
      }

      changes.Clear();
      notifications.Connectors.Single().CurrentStatus.ErrorMessage = "Build failed";
      Assert.That(changes, Has.Count.EqualTo(1), "Repeated queries must not duplicate status subscriptions.");
      Assert.That(
        changes[0].Sender,
        Is.SameAs(notifications.Connectors.Single().CurrentStatus),
        "The tray identifies projects using the original event sender.");
    }

    private static void VerifyNotificationBindings()
    {
      var item = new Configuration.ViewModel.ConfigurationItemViewModel();
      item.Load(
        new ConfigurationItem(Connector.NotificationsVisbility)
        {
          Value = Newtonsoft.Json.JsonConvert.SerializeObject(new ConnectorNotificationConfiguration { UseGlobalNotificationSettings = true }),
        });
      var card = (ContentControl)new NotificationSettingsControlBuilder().GetControl(
        item,
        new ConfigurationItemAttribute(Connector.NotificationsVisbility, typeof(ConnectorNotificationConfiguration)));
      var columns = (StackPanel)((Expander)card.Content).Content;
      var states = (StackPanel)columns.Children[0];
      var options = (StackPanel)columns.Children[1];
      var useGlobalSettings = (ToggleButton)((StackPanel)options.Children[0]).Children[0];
      var onlyIfChanged = (ToggleButton)((StackPanel)options.Children[1]).Children[0];
      foreach (var useGlobal in new[] { true, false, true, false })
      {
        useGlobalSettings.IsChecked = useGlobal;
        useGlobalSettings.GetBindingExpression(ToggleButton.IsCheckedProperty).UpdateSource();
        card.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.That(onlyIfChanged.IsEnabled, Is.EqualTo(!useGlobal));
        foreach (StackPanel row in states.Children)
        {
          Assert.That(((ToggleButton)row.Children[0]).IsEnabled, Is.EqualTo(!useGlobal));
        }

        Assert.That(
          Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectorNotificationConfiguration>(item.Value ?? throw new AssertionException("Expected settings."))?.UseGlobalNotificationSettings,
          Is.EqualTo(useGlobal));
      }
    }

    private static void VerifyTitleBarActionsAndLayout()
    {
      var target = new Window { Title = "A long window title that must not overlap its controls" };
      var header = new CustomTitleBar { Window = target, CustomButtonVisible = true, ShowMinimizeButton = true };
      foreach (var width in new[] { 300, 760 })
      {
        header.Measure(new Size(width, 44));
        header.Arrange(new Rect(0, 0, width, 44));
        header.UpdateLayout();
        foreach (var name in new[] { "SettingsButton", "AddMenuButton", "MinimizeButton", "CloseButton" })
        {
          var button = (Button)header.FindName(name);
          var bounds = button.TransformToAncestor(header).TransformBounds(new Rect(button.RenderSize));
          Assert.That(bounds.Width, Is.EqualTo(36), name);
          Assert.That(bounds.Height, Is.EqualTo(32), name);
          Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(4), name);
          Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(40), name);
          Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(0), name);
          Assert.That(bounds.Right, Is.LessThanOrEqualTo(width), name);
          Assert.That(button.Focusable, Is.True, name);
        }
      }

      var settingsClicks = 0;
      var projectClicks = 0;
      var importClicks = 0;
      var groupClicks = 0;
      header.ButtonClicked += (_, _) => settingsClicks++;
      header.AddConnectorClicked += (_, _) => projectClicks++;
      header.ImportClicked += (_, _) => importClicks++;
      header.AddGroupClicked += (_, _) => groupClicks++;
      ((Button)header.FindName("SettingsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      var menu = ((Button)header.FindName("AddMenuButton")).ContextMenu;
      foreach (MenuItem item in menu.Items)
      {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
      }

      Assert.That(new[] { settingsClicks, projectClicks, importClicks, groupClicks }, Is.EqualTo([1, 1, 1, 1]));
      var closed = false;
      System.ComponentModel.CancelEventHandler cancelClose = (_, e) => e.Cancel = true;
      target.Closing += cancelClose;
      target.Closed += (_, _) => closed = true;
      var closeButton = (Button)header.FindName("CloseButton");
      closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      Assert.That(closed, Is.False, "The shared close button must respect a window's close cancellation.");
      target.Closing -= cancelClose;
      closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      Assert.That(closed, Is.True);
    }

    private static void VerifySettingsPageEdges(ConfigWindow settings, ListBox sections, Frame frame)
    {
      var root = (FrameworkElement)settings.Content;
      foreach (var section in new[] { 0, 2 })
      {
        sections.SelectedIndex = section;
        settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var viewer = (ScrollViewer)((Page)frame.Content).Content;
        var content = (StackPanel)viewer.Content;
        foreach (var height in new[] { 580, 1000, 580 })
        {
          root.Measure(new Size(850, height));
          root.Arrange(new Rect(0, 0, 850, height));
          root.UpdateLayout();
          var scrollbarVisible = height == 580;
          Assert.That(viewer.ComputedVerticalScrollBarVisibility, Is.EqualTo(scrollbarVisible ? Visibility.Visible : Visibility.Collapsed));
          var viewerRight = viewer.TransformToAncestor(root).Transform(new Point(viewer.ActualWidth, 0)).X;
          Assert.That(root.ActualWidth - viewerRight, Is.EqualTo(9).Within(1), "The scrollbar must stay near the window edge.");
          if (!scrollbarVisible)
          {
            var contentRight = content.TransformToAncestor(root).Transform(new Point(content.ActualWidth, 0)).X;
            Assert.That(root.ActualWidth - contentRight, Is.EqualTo(29).Within(1), "Pages without scrollbars must retain their content gutter.");
          }
        }
      }
    }

    private static void VerifyScrollSpacing()
    {
      var content = new StackPanel { Style = (Style)Application.Current.FindResource("SettingsScrollContent") };
      content.Children.Add(new Border { Height = 400 });
      var viewer = new ScrollViewer
      {
        Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
      };
      foreach (var height in new[] { 600, 200, 600 })
      {
        viewer.Measure(new Size(500, height));
        viewer.Arrange(new Rect(0, 0, 500, height));
        viewer.UpdateLayout();
        viewer.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.That(viewer.ComputedVerticalScrollBarVisibility, Is.EqualTo(height < 400 ? Visibility.Visible : Visibility.Collapsed));
        Assert.That(content.Margin, Is.EqualTo(height < 400 ? new Thickness(0, 0, 20, 0) : new Thickness(0)));
      }
    }

    private static void VerifyWizardLayouts(App app)
    {
      var controller = new WizardController(null, new ApplicationConfiguration(), new WindowSettings());
      var wizard = new WizardWindow(controller);
      try
      {
        foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
        {
          app.ApplyTheme(theme);
          var connection = new ConnectionWizardPage(controller);
          wizard.Frame.Content = connection;
          wizard.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
          var content = (FrameworkElement)wizard.Content;
          content.Measure(new Size(600, 600));
          content.Arrange(new Rect(0, 0, 600, 600));
          content.UpdateLayout();
          var next = (Button)wizard.FindName("NextButton");
          var address = (ComboBox)connection.FindName("AddressComboBox");
          address.Text = string.Empty;
          Assert.That(next.IsEnabled, Is.False);
          address.Text = "h";
          Assert.That(address.Text, Is.EqualTo("h"), "Partial addresses must remain editable.");
          address.Text = "https://ci.example.invalid";
          Assert.That(next.IsEnabled, Is.True);
          Assert.That(controller.ProposedServerAddress, Is.EqualTo("https://ci.example.invalid/"));
          Assert.That(address.ActualHeight, Is.GreaterThanOrEqualTo(40));

          controller.Projects.AddProject(
            new Project("https://ci.example.invalid", "Application build", "https://ci.example.invalid/job/app", "Application build"));
          var selection = new ProjectSelectionWizardPage(controller) { DataContext = controller };
          wizard.Frame.Content = selection;
          wizard.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
          content.Measure(new Size(600, 600));
          content.Arrange(new Rect(0, 0, 600, 600));
          content.UpdateLayout();
          var projects = (ProjectsTreeView)selection.FindName("projectsTreeView");
          var list = ProjectPickerChecks.GetProjectList(projects, 0);
          var firstProject = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
          Assert.That(firstProject, Is.Not.Null);
          Assert.That(firstProject.ActualHeight, Is.GreaterThan(0));
          Assert.That(projects.ActualHeight, Is.GreaterThan(50), "The project list must remain usable at minimum window size.");
          Assert.That(((ComboBox)selection.FindName("uxGrouppingSettings")).ActualHeight, Is.GreaterThanOrEqualTo(36));
        }
      }
      finally
      {
        wizard.Close();
      }
    }

    private static void VerifyGermanViews(App app)
    {
      var previousCulture = CultureInfo.CurrentUICulture;
      try
      {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
        {
          app.ApplyTheme(theme);
          var configuration = new ApplicationConfiguration { Language = ApplicationLanguage.German, ThemeMode = theme };
          var settings = new ConfigWindow(configuration, new WindowSettings());
          try
          {
            Assert.That(settings.Title, Is.EqualTo("Einstellungen"));
            var sections = (ListBox)settings.FindName("ConfigTopicsListBox");
            var frame = (Frame)settings.FindName("ConfigFrame");
            foreach (var section in new[] { 0, 1, 2 })
            {
              sections.SelectedIndex = section;
              settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
              var root = (FrameworkElement)settings.Content;
              root.Measure(new Size(800, 580));
              root.Arrange(new Rect(0, 0, 800, 580));
              root.UpdateLayout();
              if (section == 0)
              {
                var selector = (ComboBox)((Page)frame.Content).FindName("LanguageSelector");
                Assert.That(selector.SelectedValue, Is.EqualTo(ApplicationLanguage.German));
                selector.SelectedValue = ApplicationLanguage.English;
                Assert.That(
                  configuration.Language,
                  Is.EqualTo(ApplicationLanguage.German),
                  "The language must remain unsaved until Save changes is used.");
                Assert.That(CultureInfo.CurrentUICulture.Name, Is.EqualTo("en-US"));
                Assert.That(settings.Title, Is.EqualTo("Settings"));
                selector.SelectedValue = ApplicationLanguage.German;
              }
              else if (section == 1)
              {
                Assert.That(((Button)((Page)frame.Content).FindName("AddButton")).Content, Is.EqualTo("Manuell hinzufügen"));
              }
            }
          }
          finally
          {
            settings.Close();
          }

          var controller = new WizardController(null, configuration, new WindowSettings());
          var wizard = new WizardWindow(controller);
          try
          {
            Assert.That(wizard.Title, Is.EqualTo("Projekte hinzufügen"));
            Assert.That(((Button)wizard.FindName("NextButton")).Content, Is.EqualTo("Weiter"));
            wizard.Frame.Content = new ConnectionWizardPage(controller);
            wizard.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var root = (FrameworkElement)wizard.Content;
            root.Measure(new Size(600, 600));
            root.Arrange(new Rect(0, 0, 600, 600));
            root.UpdateLayout();
          }
          finally
          {
            wizard.Close();
          }
        }
      }
      finally
      {
        CultureInfo.CurrentUICulture = previousCulture;
      }
    }

    private sealed class ResourceTestApp : App
    {
      protected override void OnStartup(StartupEventArgs e)
      {
      }
    }
  }
}
