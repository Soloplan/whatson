// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsStatusViewModelTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class JenkinsStatusViewModelTests
  {
    [Test]
    public void VolunteerChangesNotifyAndEnableTheAppropriateCommands()
    {
      var configuration = new ConnectorConfiguration("Jenkins", "job", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "job");
      configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      configuration.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-placeholder";
      var connector = new JenkinsConnector(configuration, new JenkinsApi());
      var model = (JenkinsStatusViewModel)new JenkinsProjectViewModel(connector).CurrentStatus;
      var changes = new List<string?>();
      model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
      model.Update(new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 });
      Assert.That(model.VolunteerToFix.CanExecute(null), Is.True);
      Assert.That(model.ClearVolunteer.CanExecute(null), Is.False);

      model.Update(new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42, Details = JenkinsVolunteer.SetOwner(null, "alex") });
      Assert.That(model.Volunteer, Is.EqualTo("alex"));
      Assert.That(changes, Does.Contain(nameof(model.Volunteer)));
      Assert.That(model.VolunteerToFix.CanExecute(null), Is.False);
      Assert.That(model.ClearVolunteer.CanExecute(null), Is.True);

      model.Update(new JenkinsStatus(ObservationState.Success) { BuildNumber = 43 });
      Assert.That(model.Volunteer, Is.Null);
      Assert.That(model.VolunteerToFix.CanExecute(null), Is.False);
      Assert.That(model.ClearVolunteer.CanExecute(null), Is.False);
    }

    [TestCase(ObservationState.Failure, true)]
    [TestCase(ObservationState.Unstable, true)]
    [TestCase(ObservationState.Success, false)]
    [TestCase(ObservationState.Running, false)]
    [TestCase(ObservationState.Unknown, false)]
    public void OnlyTheCurrentBrokenBuildCanBeClaimedAndCredentialsAreRequired(ObservationState state, bool canClaim)
    {
      var configuration = new ConnectorConfiguration("Jenkins", "job", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "job");
      var connector = new JenkinsConnector(configuration, new JenkinsApi());
      var project = new JenkinsProjectViewModel(connector);
      var current = project.CurrentStatus;
      current.Update(new JenkinsStatus(state) { BuildNumber = 43, Building = state == ObservationState.Running });
      Assert.That(current.VolunteerToFix, Is.Not.Null, "Keep the action visible without credentials.");
      Assert.That(current.VolunteerToFix?.CanExecute(null), Is.False);
      configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      Assert.That(current.VolunteerToFix?.CanExecute(null), Is.False);
      configuration.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-placeholder";
      Assert.That(current.VolunteerToFix?.CanExecute(null), Is.EqualTo(canClaim));

      var history = new JenkinsStatusViewModel(project);
      history.Update(new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 });
      Assert.That(history.VolunteerToFix.CanExecute(null), Is.False, "A historical failure is not the current run.");
      history.Update(new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42, Details = JenkinsVolunteer.SetOwner(null, "alex") });
      Assert.That(history.ClearVolunteer.CanExecute(null), Is.True, "Existing historical markers can still be cleared.");
    }

    [Test]
    public void UnconfiguredViewModelHasAStatusBeforeTheFirstPoll()
    {
      var model = new ConnectorViewModel();
      model.CurrentStatus.Update(null);

      Assert.That(model.CurrentStatus.State, Is.EqualTo(ObservationState.Unknown));
      Assert.That(model.ConnectorSnapshots, Is.Empty);
    }

    [TestCase("")]
    [TestCase(",\"culprits\":null,\"changeSets\":null")]
    [TestCase(",\"culprits\":[],\"changeSets\":[]")]
    [TestCase(",\"culprits\":[null],\"changeSets\":[null,{\"items\":null},{\"items\":[null,{\"author\":null}]}]")]
    public async Task MissingContributorsCanBeDisplayedAndClearPreviousContributors(string contributors)
    {
      await using var server = new ProjectDiscoveryTests.DiscoveryServer();
      server.Respond = _ => ("200 OK", "application/json", "{\"lastBuild\":{\"number\":1,\"result\":\"SUCCESS\",\"timestamp\":1" + contributors + "}}");
      var connector = new JenkinsConnector(
        new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Address, Connector.ProjectName, "Release"), new JenkinsApi());
      var viewModel = new JenkinsStatusViewModel(new ConnectorViewModel(connector));
      viewModel.Update(new JenkinsStatus(ObservationState.Failure)
      {
        Culprits = new List<JenkinsUser> { new JenkinsUser { FullName = "Previous culprit" } },
        CommittedToThisBuild = new List<JenkinsUser> { new JenkinsUser { FullName = "Previous author" } },
      });
      Assert.That(viewModel.Culprits, Has.Count.EqualTo(1));
      Assert.That(viewModel.CommittedToThisBuild, Has.Count.EqualTo(1));

      await connector.QueryStatus(CancellationToken.None);
      var status = connector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Expected a Jenkins build status.");
      viewModel.Update(status);

      Assert.Multiple(() =>
      {
        Assert.That(status.Culprits, Is.Empty);
        Assert.That(status.CommittedToThisBuild, Is.Empty);
        Assert.That(viewModel.Culprits, Is.Empty);
        Assert.That(viewModel.CommittedToThisBuild, Is.Empty);
        Assert.That(viewModel.CulpritsAndLastCommittedDifferent, Is.False);
        Assert.That(viewModel.State, Is.EqualTo(ObservationState.Success));
        Assert.That(server.Requests, Has.Count.EqualTo(1));
      });
    }
  }
}
