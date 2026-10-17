// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservationSchedulerTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections.Concurrent;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class ObservationSchedulerTests
  {
    private ObservationScheduler scheduler = new ObservationScheduler();

    [SetUp]
    public void SetUp() => this.scheduler = new ObservationScheduler();

    [TearDown]
    public void TearDown() => this.scheduler.Stop(true);

    [Test]
    public async Task DuplicateObservationAndRepeatedStartDoNotCreateDuplicateQueries()
    {
      var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var release = new TaskCompletionSource<Status?>(TaskCreationOptions.RunContinuationsAsynchronously);
      var queries = 0;
      var connector = new TestConnector
      {
        Query = token =>
        {
          Interlocked.Increment(ref queries);
          started.TrySetResult(true);
          return release.Task;
        },
      };
      this.scheduler.Observe(connector, 3600);
      this.scheduler.Observe(connector, 3600);
      var published = new TaskCompletionSource<Connector>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.scheduler.StatusQueried += (sender, value) =>
      {
        this.scheduler.Stop(false);
        published.TrySetResult(value);
      };

      this.scheduler.Start();
      this.scheduler.Start();
      await Await(started.Task);
      release.SetResult(new Status(ObservationState.Running));

      Assert.That(await Await(published.Task), Is.SameAs(connector));
      Assert.That(queries, Is.EqualTo(1));
    }

    [Test]
    public async Task ForceStopCancelsTheTokenOfAnInFlightQuery()
    {
      var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
      var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var notifications = 0;
      var connector = new TestConnector
      {
        Query = async token =>
        {
          started.TrySetResult(token);
          try
          {
            await Task.Delay(Timeout.Infinite, token);
            return null;
          }
          finally
          {
            finished.TrySetResult(true);
          }
        },
      };
      this.scheduler.StatusQueried += (sender, value) => Interlocked.Increment(ref notifications);
      this.scheduler.Observe(connector);
      this.scheduler.Start();
      var queryToken = await Await(started.Task);

      this.scheduler.Stop(true);
      await Await(finished.Task);

      Assert.That(queryToken.IsCancellationRequested, Is.True);
      Assert.That(this.scheduler.Running, Is.False);
      Assert.That(notifications, Is.Zero);
    }

    [Test]
    public async Task GracefulStopLetsTheCurrentQueryFinishWithoutCancellation()
    {
      var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
      var release = new TaskCompletionSource<Status?>(TaskCreationOptions.RunContinuationsAsynchronously);
      var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var connector = new TestConnector
      {
        Query = async token =>
        {
          started.TrySetResult(token);
          var result = await release.Task;
          finished.SetResult(true);
          return result;
        },
      };
      this.scheduler.Observe(connector);
      this.scheduler.Start();
      var queryToken = await Await(started.Task);

      this.scheduler.Stop(false);
      release.SetResult(new Status(ObservationState.Running));
      await Await(finished.Task);

      Assert.That(queryToken.IsCancellationRequested, Is.False);
      Assert.That(this.scheduler.Running, Is.False);
    }

    [Test]
    public async Task ObservationCannotBeRemovedWhileQueriesAreRunning()
    {
      var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var connector = new TestConnector
      {
        Query = async token =>
        {
          started.TrySetResult(true);
          await Task.Delay(Timeout.Infinite, token);
          return null;
        },
      };
      this.scheduler.Observe(connector);
      this.scheduler.Start();
      await Await(started.Task);

      Assert.That(() => this.scheduler.UnobserveAll(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task RemovedConnectorsAreNotQueriedWhenNewObservationStarts()
    {
      var oldQueries = 0;
      var oldConnector = new TestConnector
      {
        Query = token =>
        {
          Interlocked.Increment(ref oldQueries);
          return Task.FromResult<Status?>(new Status(ObservationState.Running));
        },
      };
      var completed = new TaskCompletionSource<Connector>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.scheduler.StatusQueried += (sender, connector) =>
      {
        this.scheduler.Stop(false);
        completed.TrySetResult(connector);
      };
      this.scheduler.Observe(oldConnector);
      this.scheduler.Start();
      await Await(completed.Task);
      this.scheduler.UnobserveAll();
      completed = new TaskCompletionSource<Connector>(TaskCreationOptions.RunContinuationsAsynchronously);
      var replacement = new TestConnector();
      this.scheduler.Observe(replacement);

      this.scheduler.Start();

      Assert.That(await Await(completed.Task), Is.SameAs(replacement));
      Assert.That(oldQueries, Is.EqualTo(1));
    }

    [Test]
    public async Task TransientQueryFailureDoesNotPermanentlyStopObservation()
    {
      var attempts = 0;
      var connector = new TestConnector
      {
        Query = token =>
        {
          if (Interlocked.Increment(ref attempts) == 1)
          {
            throw new InvalidOperationException("Temporary API failure");
          }

          return Task.FromResult<Status?>(new Status(ObservationState.Running));
        },
      };
      var published = new TaskCompletionSource<Connector>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.scheduler.StatusQueried += (sender, value) =>
      {
        this.scheduler.Stop(false);
        published.TrySetResult(value);
      };
      this.scheduler.Observe(connector, 1);

      this.scheduler.Start();

      Assert.That(await Await(published.Task), Is.SameAs(connector));
      Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public async Task EachConfiguredConnectorPublishesItsOwnStatus()
    {
      var observed = new ConcurrentDictionary<Connector, bool>();
      var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var first = new TestConnector();
      var second = new TestConnector();
      this.scheduler.StatusQueried += (sender, value) =>
      {
        observed.TryAdd(value, true);
        if (observed.Count == 2)
        {
          this.scheduler.Stop(false);
          completed.TrySetResult(true);
        }
      };
      this.scheduler.Observe(first, 3600);
      this.scheduler.Observe(second, 3600);

      this.scheduler.Start();
      await Await(completed.Task);

      Assert.That(observed.Keys, Is.EquivalentTo(new[] { first, second }));
    }

    private static async Task<T> Await<T>(Task<T> task)
    {
      Assert.That(
        await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))),
        Is.SameAs(task),
        "Observation did not complete within five seconds.");
      return await task;
    }
  }
}
