// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyNotificationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.GUI;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Model;
  using ConnectorViewModel = Soloplan.WhatsON.GUI.Common.ConnectorTreeView.ConnectorViewModel;

  [TestFixture]
  public class PropertyNotificationTests
  {
    [Test]
    public void InheritedTimingUpdatesNotifyTheDisplayedFinishTimeOncePerChange()
    {
      var model = new BuildStatusProbe();
      var changes = Observe(model);
      var start = new DateTime(2026, 10, 17, 12, 0, 0);
      model.SetTiming(start, TimeSpan.FromMinutes(4));
      Assert.That(model.FinishTime, Is.EqualTo(start.AddMinutes(4)));
      Assert.That(changes.Count(name => name == nameof(model.Time)), Is.EqualTo(1));
      Assert.That(changes.Count(name => name == nameof(model.Duration)), Is.EqualTo(1));
      Assert.That(changes.Count(name => name == nameof(model.FinishTime)), Is.EqualTo(2));

      changes.Clear();
      model.SetTiming(start, TimeSpan.FromMinutes(4));
      Assert.That(changes, Is.Empty, "Repeated build polling must not reset unchanged UI state.");
    }

    [Test]
    public void BuildNumberAndDisplayNameRefreshTheCalculatedLabel()
    {
      var model = new BuildStatusProbe();
      var changes = Observe(model);
      model.SetLabelInputs(42, "Release");
      Assert.That(model.Label, Is.EqualTo("Release (#42)"));
      Assert.That(changes.Count(name => name == nameof(model.Label)), Is.EqualTo(2));
      changes.Clear();
      model.SetLabelInputs(43, "Release");
      Assert.That(model.Label, Is.EqualTo("Release (#43)"));
      Assert.That(changes.Count(name => name == nameof(model.Label)), Is.EqualTo(1));
    }

    [Test]
    public void PresentationPluginPropertiesNotifyThroughTheSharedBaseEvent()
    {
      var jenkins = new JenkinsStatusViewModel(new ConnectorViewModel());
      var jenkinsChanges = Observe(jenkins);
      jenkins.CulpritsAndLastCommittedDifferent = true;
      jenkins.CulpritsAndLastCommittedDifferent = true;
      Assert.That(jenkinsChanges, Is.EqualTo([nameof(jenkins.CulpritsAndLastCommittedDifferent)]));

      var cruiseControl = new CruiseControlStatusViewModel(new ConnectorViewModel());
      var cruiseControlChanges = Observe(cruiseControl);
      cruiseControl.BuildTimeUnknown = true;
      cruiseControl.BuildTimeUnknown = true;
      Assert.That(cruiseControlChanges, Is.EqualTo([nameof(cruiseControl.BuildTimeUnknown)]));
    }

    [Test]
    public void UnchangedPreferencesAndTransientUiStateDoNotMarkSettingsAsModified()
    {
      var model = new ConfigViewModel();
      model.Load(new ApplicationConfiguration());
      var changes = Observe(model);
      model.ThemeMode = model.ThemeMode;
      model.Language = model.Language;
      model.AlwaysOnTop = model.AlwaysOnTop;
      Assert.That(changes, Is.Empty);
      model.SingleConnectorMode = !model.SingleConnectorMode;
      Assert.That(model.ConfigurationIsModified, Is.False);
    }

    [Test]
    public void EditingAndReloadingConnectorSettingsRaisesDirtyFlagsWithoutDuplicateSubscriptions()
    {
      var source = new ApplicationConfiguration();
      source.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Main", Connector.Category, "Original"));
      var model = new ConfigViewModel();
      model.Load(source);
      var changes = Observe(model);
      for (var iteration = 0; iteration < 2; iteration++)
      {
        changes.Clear();
        model.Connectors[0].GetConfigurationByKey(Connector.Category).Value = "Edited";
        Assert.That(model.ConfigurationIsModified, Is.True);
        Assert.That(model.ConfigurationIsNotModified, Is.False);
        Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsModified)), Is.EqualTo(1));
        Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsNotModified)), Is.EqualTo(1));
        Assert.That(source.ConnectorsConfiguration[0].GetConfigurationByKey(Connector.Category).Value, Is.EqualTo("Original"));

        changes.Clear();
        model.Load(source);
        Assert.That(model.ConfigurationIsModified, Is.False);
        Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsModified)), Is.EqualTo(1));
        Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsNotModified)), Is.EqualTo(1));
      }
    }

    [TestCase(nameof(ConfigViewModel.ThemeMode))]
    [TestCase(nameof(ConfigViewModel.Language))]
    [TestCase(nameof(ConfigViewModel.ShowInTaskbar))]
    [TestCase(nameof(ConfigViewModel.AlwaysOnTop))]
    [TestCase(nameof(ConfigViewModel.OpenMinimized))]
    [TestCase(nameof(ConfigViewModel.UnstableObservationState))]
    [TestCase(nameof(ConfigViewModel.FailureObservationState))]
    [TestCase(nameof(ConfigViewModel.SuccessObservationState))]
    [TestCase(nameof(ConfigViewModel.RunningObservationState))]
    [TestCase(nameof(ConfigViewModel.UnknownObservationState))]
    [TestCase(nameof(ConfigViewModel.NotifyOnlyIfStatusChanged))]
    [TestCase(nameof(ConfigViewModel.OpenOnSystemStart))]
    [TestCase(nameof(ConfigViewModel.ViewStyle))]
    public void EveryPersistedPreferenceMarksTheSettingsAsModified(string propertyName)
    {
      var model = new ConfigViewModel();
      model.Load(new ApplicationConfiguration());
      var changes = Observe(model);
      var property = typeof(ConfigViewModel).GetProperty(propertyName) ?? throw new AssertionException("Expected member was not found.");
      var originalValue = property.GetValue(model) ?? throw new AssertionException("Expected a property value.");
      var editedValue = property.PropertyType == typeof(bool) ? !(bool)originalValue :
        property.PropertyType.IsEnum ? Enum.GetValues(property.PropertyType).Cast<object>().First(value => !Equals(value, originalValue)) :
        (int)originalValue + 1;
      property.SetValue(model, editedValue);
      Assert.That(model.ConfigurationIsModified, Is.True, propertyName);
      Assert.That(model.ConfigurationIsNotModified, Is.False, propertyName);
      Assert.That(changes.Count(name => name == propertyName), Is.EqualTo(1));
      Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsModified)), Is.EqualTo(1));
      Assert.That(changes.Count(name => name == nameof(model.ConfigurationIsNotModified)), Is.EqualTo(1));
    }

    [Test]
    public void AutomaticDetectionAndAddressChangesRefreshWizardControls()
    {
      var wizard = new WizardController(null, new ApplicationConfiguration(), new WindowSettings());
      var changes = Observe(wizard);
      wizard.IsAutoDetectionEnabled = false;
      Assert.That(wizard.IsAutoDetectionDisabled, Is.True);
      Assert.That(
        changes,
        Does.Contain(nameof(wizard.IsAutoDetectionEnabled)).And.Contain(nameof(wizard.IsAutoDetectionDisabled)).And
          .Contain(nameof(wizard.AvailableServers)));
      changes.Clear();
      wizard.ProposedServerAddress = "https://build.example.invalid";
      Assert.That(wizard.IsNextStepEnabled, Is.True);
      Assert.That(changes, Does.Contain(nameof(wizard.IsProposedAddressEmpty)).And.Contain(nameof(wizard.IsNextStepEnabled)));
      changes.Clear();
      wizard.ProposedServerAddress = " ";
      Assert.That(wizard.IsNextStepEnabled, Is.False);
      Assert.That(changes, Does.Contain(nameof(wizard.IsProposedAddressEmpty)).And.Contain(nameof(wizard.IsNextStepEnabled)));
      changes.Clear();
      wizard.IsAutoDetectionEnabled = true;
      Assert.That(wizard.IsAutoDetectionDisabled, Is.False);
      Assert.That(changes, Does.Contain(nameof(wizard.IsAutoDetectionDisabled)).And.Contain(nameof(wizard.AvailableServers)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SavedServerChoicesIgnoreMissingAndInvalidAddresses(bool autoDetect)
    {
      var configuration = new ApplicationConfiguration();
      foreach (var address in new[] { null, string.Empty, " ", "not a server", "https://build.example.invalid", "https://build.example.invalid/" })
      {
        configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, address));
      }

      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("CruiseControl", "Other", Connector.ServerAddress, "https://other.example.invalid/"));
      var wizard = new WizardController(null, configuration, null)
      {
        SelectedConnectorType = new Soloplan.WhatsON.Jenkins.JenkinsPlugin(),
        IsAutoDetectionEnabled = autoDetect,
      };

      Assert.That(wizard.AvailableServers, Is.EquivalentTo(autoDetect
        ? new[] { "https://build.example.invalid/", "https://other.example.invalid/" }
        : new[] { "https://build.example.invalid/" }));
    }

    [Test]
    public void MissingPluginsHaveNoEditorsAndPreserveTheirSavedConfiguration()
    {
      var original = new ConnectorConfiguration("UnavailablePlugin", "Release", "CustomKey", "Saved value");
      var model = new Configuration.ViewModel.ConnectorViewModel();
      model.Load(original);

      Assert.That(model.GetConnectorConfigAttributes(), Is.Empty);
      Assert.That(model.ApplyToSourceConnectorConfiguration(out var created), Is.SameAs(original));
      Assert.That(created, Is.False);
      Assert.That(original.GetConfigurationByKey("CustomKey").Value, Is.EqualTo("Saved value"));
    }

    [Test]
    public void MissingPluginsKeepTheirIdentityAndSettingsWhenTheProjectListIsSaved()
    {
      var configuration = new ApplicationConfiguration();
      var original = new ConnectorConfiguration("UnavailablePlugin", "Release", "CustomKey", "Saved value", Connector.Category, "Tools");
      configuration.ConnectorsConfiguration.Add(original);
      var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      var group = tree.ConnectorGroups.Single();
      var project = group.ConnectorViewModels.Single();

      Assert.That(project, Is.InstanceOf<ConnectorMissingViewModel>());
      Assert.That(project.Identifier, Is.EqualTo(original.Identifier));
      Assert.That(project.Name, Is.EqualTo("Release"));
      Assert.That(((ConnectorMissingViewModel)project).ExpectedConnectorType, Is.EqualTo("UnavailablePlugin"));
      group.GroupName = "Renamed group";
      tree.WriteToConfiguration(configuration);

      Assert.That(configuration.ConnectorsConfiguration.Single(), Is.SameAs(original));
      Assert.That(original.GetConfigurationByKey("CustomKey").Value, Is.EqualTo("Saved value"));
      Assert.That(original.GetConfigurationByKey(Connector.Category).Value, Is.EqualTo("Renamed group"));
      tree.Update(configuration);
      Assert.That(tree.ConnectorGroups.Single().ConnectorViewModels.Single().Identifier, Is.EqualTo(original.Identifier));
    }

    [Test]
    public void GroupSelectionIncludesHiddenChildrenAndNotifiesAncestors()
    {
      var projects = new ProjectViewModelList { MultiSelectionMode = true };
      var parent = projects.AddProject(CreateProject("Group"));
      var visible = parent.AddProject(CreateProject("Visible"));
      var hidden = parent.AddProject(CreateProject("Hidden"));
      visible.Parent = parent;
      hidden.Parent = parent;
      hidden.IsVisible = false;
      var parentChanges = Observe(parent);
      parent.IsChecked = true;
      Assert.That(visible.IsChecked, Is.True);
      Assert.That(hidden.IsChecked, Is.True, "Selecting a whole folder must include projects hidden by search.");
      parentChanges.Clear();
      visible.IsChecked = false;
      Assert.That(parent.IsChecked, Is.False);
      Assert.That(parent.SelectionState, Is.Null);
      Assert.That(parentChanges, Does.Contain(nameof(parent.IsChecked)));
      hidden.IsChecked = true;
      hidden.IsVisible = true;
      parent.IsChecked = false;
      Assert.That(visible.IsChecked, Is.False);
      Assert.That(hidden.IsChecked, Is.False, "The group setter must run even when its computed value is already false.");
    }

    [Test]
    public void SingleSelectionStillClearsThePreviousProject()
    {
      var projects = new ProjectViewModelList { MultiSelectionMode = false };
      var first = projects.AddProject(CreateProject("First"));
      var second = projects.AddProject(CreateProject("Second"));
      var firstChanges = Observe(first);
      first.IsChecked = true;
      firstChanges.Clear();
      second.IsChecked = true;
      Assert.That(first.IsChecked, Is.False);
      Assert.That(second.IsChecked, Is.True);
      Assert.That(firstChanges, Does.Contain(nameof(first.IsChecked)));
    }

    [Test]
    public void GroupCollectionChangesRefreshTheSingleGroupLayoutAndFirstGroupBinding()
    {
      var model = new ConnectorTreeViewModel();
      var changes = Observe(model);
      var first = new ConnectorGroupViewModel();
      var second = new ConnectorGroupViewModel();
      model.ConnectorGroups.Add(first);
      Assert.That(model.OneGroup, Is.True);
      Assert.That(model.FirstGroup, Is.SameAs(first));
      Assert.That(changes, Does.Contain(nameof(model.OneGroup)).And.Contain(nameof(model.FirstGroup)));
      changes.Clear();
      model.ConnectorGroups.Add(second);
      Assert.That(model.OneGroup, Is.False);
      Assert.That(changes, Is.EqualTo([nameof(model.OneGroup)]));
      changes.Clear();
      model.ConnectorGroups.Remove(first);
      Assert.That(model.OneGroup, Is.True);
      Assert.That(model.FirstGroup, Is.SameAs(second));
      Assert.That(changes, Does.Contain(nameof(model.OneGroup)).And.Contain(nameof(model.FirstGroup)));
      model.ConnectorGroups.Clear();
      Assert.That(model.OneGroup, Is.False);
      Assert.That(model.FirstGroup, Is.Null);
    }

    [Test]
    public void RenamingTreeItemsStillSignalsAConfigurationEditOnlyWhenTheNameChanges()
    {
      foreach (var item in new TreeItemViewModel[] { new ConnectorViewModel(), new ConnectorGroupViewModel() })
      {
        var changes = Observe(item);
        var configurationEdits = 0;
        item.ConfigurationChanged += (sender, args) =>
        {
          Assert.That(sender, Is.SameAs(item));
          configurationEdits++;
        };
        if (item is ConnectorViewModel connector)
        {
          connector.Name = "Release";
          connector.Name = "Release";
        }
        else if (item is ConnectorGroupViewModel group)
        {
          group.GroupName = "Release";
          group.GroupName = "Release";
        }

        Assert.That(configurationEdits, Is.EqualTo(1));
        Assert.That(changes, Has.Count.EqualTo(1));
      }
    }

    [Test]
    public void ApplyingSavedProjectNamesUpdatesBindingsWithoutMarkingConfigurationAsEdited()
    {
      var model = new ConnectorViewModel { Name = "Original name" };
      var notifications = Observe(model);
      var configurationEdits = 0;
      model.ConfigurationChanged += (sender, args) => configurationEdits++;

      model.ApplyConfiguration(new ConnectorConfiguration("Jenkins", "Saved name"));

      Assert.That(model.Name, Is.EqualTo("Saved name"));
      Assert.That(notifications, Does.Contain(nameof(model.Name)));
      Assert.That(configurationEdits, Is.Zero);

      model.Name = "User edit";
      Assert.That(configurationEdits, Is.EqualTo(1), "Configuration refresh must not suppress later user edits.");
    }

    private static Project CreateProject(string name) => new Project("https://build.example.invalid", name, null, name);

    private static List<string> Observe(INotifyPropertyChanged model)
    {
      var changes = new List<string>();
      model.PropertyChanged += (sender, args) =>
      {
        Assert.That(sender, Is.SameAs(model));
        changes.Add(args.PropertyName ?? throw new AssertionException("Expected a property name."));
      };
      return changes;
    }

    private sealed class BuildStatusProbe : BuildStatusViewModel
    {
      public BuildStatusProbe()
        : base(new ConnectorViewModel())
      {
      }

      public void SetTiming(DateTime time, TimeSpan duration)
      {
        this.Time = time;
        this.Duration = duration;
      }

      public void SetLabelInputs(int number, string displayName)
      {
        this.BuildNumber = number;
        this.DisplayName = displayName;
      }
    }
  }
}
