// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class ConnectorTests
  {
    [Test]
    public async Task RepeatedPollingOfTheSameBuildDoesNotDuplicateHistory()
    {
      var status = new Status(ObservationState.Success) { BuildNumber = 1 };
      var connector = new TestConnector { Query = token => Task.FromResult<Status?>(status) };

      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 1 }));
      Assert.That(connector.CurrentStatus, Is.SameAs(status));
    }

    [Test]
    public async Task RunningBuildIsRecordedExactlyOnceWhenItFinishes()
    {
      var status = new Status(ObservationState.Running) { BuildNumber = 1 };
      var connector = new TestConnector { Query = token => Task.FromResult<Status?>(status) };
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.Snapshots, Is.Empty);

      status = new Status(ObservationState.Failure) { BuildNumber = 1 };
      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots, Has.Count.EqualTo(1));
      Assert.That(connector.Snapshots.Single().Status.State, Is.EqualTo(ObservationState.Failure));
    }

    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unknown)]
    public async Task InvalidBuildsNeverPolluteHistory(ObservationState state)
    {
      var status = new Status(state) { BuildNumber = 1, InvalidBuild = true };
      var connector = new TestConnector { Query = token => Task.FromResult<Status?>(status) };

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus, Is.SameAs(status));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [Test]
    public async Task MissingStatusDoesNotCreateAPhantomBuild()
    {
      var connector = new TestConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus, Is.Null);
      Assert.That(connector.Snapshots, Is.Empty);
      Assert.That(connector.HistoryRequests, Is.Zero);
    }

    [Test]
    public async Task InitialHistoryIgnoresMissingBuildsAndIsFetchedOnlyOnce()
    {
      var status = new Status(ObservationState.Success) { BuildNumber = 3, Time = new DateTime(2020, 1, 3) };
      var connector = new TestConnector { Query = token => Task.FromResult<Status?>(status) };
      connector.History.Add(new Status(ObservationState.Failure) { BuildNumber = 2, Time = new DateTime(2020, 1, 2) });
      connector.History.Add(null);
      connector.History.Add(new Status(ObservationState.Success) { BuildNumber = 1, Time = new DateTime(2020, 1, 1) });

      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 3, 2, 1 }));
      Assert.That(connector.HistoryRequests, Is.EqualTo(1));
    }

    [Test]
    public void OutOfOrderBuildsAreDisplayedNewestFirst()
    {
      var connector = new TestConnector();
      foreach (var number in new[] { 2, 4, 1, 3 })
      {
        connector.AddSnapshot(new Status(ObservationState.Success) { BuildNumber = number, Time = new DateTime(2020, 1, number) });
      }

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 4, 3, 2, 1 }));
    }

    [Test]
    public void HistoryIsBoundedAndEvictsTheOldestBuild()
    {
      var connector = new TestConnector();
      for (var number = 1; number <= 8; number++)
      {
        connector.AddSnapshot(new Status(ObservationState.Success) { BuildNumber = number, Time = new DateTime(2020, 1, number) });
      }

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
    }

    [Test]
    public async Task FailedQueryPreservesTheLastKnownStatusAndHistory()
    {
      var status = new Status(ObservationState.Success) { BuildNumber = 1 };
      var connector = new TestConnector { Query = token => Task.FromResult<Status?>(status) };
      await connector.QueryStatus(CancellationToken.None);
      connector.Query = token => throw new InvalidOperationException("Server response failed");

      Assert.That(async () => await connector.QueryStatus(CancellationToken.None), Throws.TypeOf<InvalidOperationException>());
      Assert.That(connector.CurrentStatus, Is.SameAs(status));
      Assert.That(connector.Snapshots.Single().Status, Is.SameAs(status));
    }
  }
}
