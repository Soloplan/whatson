// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationDecisionTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class NotificationDecisionTests
  {
    private static readonly ObservationState[] States = Enum.GetValues<ObservationState>();

    [TestCaseSource(nameof(Transitions))]
    public void EveryTransitionRespectsEnabledStatesAndTheMostRecentRelevantBuild(
      ObservationState currentState, ObservationState previousState, int mask, bool onlyChanges)
    {
      var settings = Settings(mask, onlyChanges);
      var current = new Status(currentState) { BuildNumber = 3 };
      var previous = new Status(previousState) { BuildNumber = 2 };
      foreach (var olderState in States)
      {
        var older = new Status(olderState) { BuildNumber = 1 };
        var enabled = Enabled(currentState, mask);
        var changed = Enabled(previousState, mask) ? previousState != currentState
          : !Enabled(olderState, mask) || olderState != currentState;
        var expected = enabled && (!onlyChanges || changed);

        Assert.That(settings.ShouldNotify(current, new[] { previous, older }), Is.EqualTo(expected), $"Older state: {olderState}");
        Assert.That(settings.ShouldNotify(current, new[] { current, previous, older }), Is.EqualTo(expected), $"Current build already snapshotted; older state: {olderState}");
      }

      Assert.That(settings.ShouldNotify(current, Array.Empty<Status>()), Is.EqualTo(Enabled(currentState, mask)), "First observation");
      Assert.That(settings.ShouldNotify(current, new[] { current }), Is.EqualTo(Enabled(currentState, mask)), "First completed build");
    }

    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Unknown)]
    [TestCase(ObservationState.Running)]
    public void HistoryUsesTheConnectorsActualNewestFirstOrder(ObservationState currentState)
    {
      var connector = new TestConnector();
      var current = new Status(currentState) { BuildNumber = 3, Time = DateTime.UtcNow };
      connector.AddSnapshot(new Status(currentState) { BuildNumber = 1, Time = current.Time.AddMinutes(-2) });
      connector.AddSnapshot(new Status(currentState == ObservationState.Failure ? ObservationState.Success : ObservationState.Failure)
      {
        BuildNumber = 2, Time = current.Time.AddMinutes(-1),
      });
      connector.AddSnapshot(current);

      Assert.That(new NotificationConfiguration { OnlyIfChanged = true }.ShouldNotify(current, connector.Snapshots.Select(snapshot => snapshot.Status)), Is.True);
    }

    [Test]
    public void AStateChangeWithinTheSameBuildStillNotifies()
    {
      var current = new Status(ObservationState.Failure) { BuildNumber = 1 };
      var previous = new Status(ObservationState.Running) { BuildNumber = 1 };
      Assert.That(new NotificationConfiguration { OnlyIfChanged = true }.ShouldNotify(current, new[] { previous }), Is.True);
    }

    private static IEnumerable<TestCaseData> Transitions()
    {
      foreach (var current in States)
      {
        foreach (var previous in States)
        {
          for (var mask = 0; mask < 1 << States.Length; mask++)
          {
            foreach (var onlyChanges in new[] { false, true })
            {
              yield return new TestCaseData(current, previous, mask, onlyChanges);
            }
          }
        }
      }
    }

    private static bool Enabled(ObservationState state, int mask) => (mask & (1 << Array.IndexOf(States, state))) != 0;

    private static NotificationConfiguration Settings(int mask, bool onlyChanges)
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = onlyChanges };
      foreach (var state in States)
      {
        settings.AssignFromObeservationStateActivity(state, Enabled(state, mask));
      }

      return settings;
    }

    private sealed class TestConnector : Connector
    {
      public TestConnector()
        : base(new ConnectorConfiguration("Fake", "Project"))
      {
      }

      protected override Task<Status?> GetCurrentStatus(CancellationToken cancellationToken) => Task.FromResult<Status?>(null);

      protected override Task<List<Status?>> GetHistory(CancellationToken cancellationToken) => Task.FromResult(new List<Status?>());
    }
  }
}
