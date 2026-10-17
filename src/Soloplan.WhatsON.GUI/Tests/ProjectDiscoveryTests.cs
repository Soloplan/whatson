// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectDiscoveryTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Compression;
  using System.Linq;
  using System.Net;
  using System.Net.Http;
  using System.Net.Sockets;
  using System.Reflection;
  using System.Runtime.ExceptionServices;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Threading;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class ProjectDiscoveryTests
  {
    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public async Task AutoDetectionIgnoresMissingEndpointsWithoutThrowingWebExceptions(bool jenkins, bool empty)
    {
      await using var server = new DiscoveryServer();
      server.Respond = path => jenkins
        ? path.StartsWith("/api/json", StringComparison.Ordinal)
          ? ("200 OK", "application/json; charset=utf-8", empty ? "{\"jobs\":[]}" : "{\"jobs\":[{\"name\":\"Release\",\"url\":\"" + server.Address + "job/Release/\"}]}")
          : ("404 Not Found", "text/html", "<html>Not found</html>")
        : path == "/XmlStatusReport.aspx"
          ? ("200 OK", "text/plain", empty ? "<Projects/>" : "<Projects><Project name='Release'/></Projects>")
          : ("404 Not Found", "text/html", "<html>Not found</html>");
      var exceptions = new ConcurrentBag<Exception>();
      EventHandler<FirstChanceExceptionEventArgs> observe = (sender, args) =>
      {
        if (args.Exception is WebException)
        {
          exceptions.Add(args.Exception);
        }
      };
      AppDomain.CurrentDomain.FirstChanceException += observe;
      try
      {
        var jenkinsQuery = new JenkinsPlugin().GetProjects(server.Address);
        var cruiseControlQuery = new CruiseControlPlugin().GetProjects(server.Address);
        var discovered = await WizardController.DiscoverProjects(
          new[]
          {
            new DiscoveryPlugin(() => jenkinsQuery, typeof(JenkinsConnector)),
            new DiscoveryPlugin(() => cruiseControlQuery, typeof(CruiseControlConnector)),
          },
          server.Address,
          TimeSpan.FromSeconds(5));
        await Task.WhenAll(jenkinsQuery, cruiseControlQuery);

        Assert.That(discovered.Plugin.Name, Is.EqualTo(jenkins ? "Jenkins" : "CruiseControl"));
        Assert.That(discovered.Projects.Count, Is.EqualTo(empty ? 0 : 1));
        if (!empty)
        {
          Assert.That(discovered.Projects.Single().Name, Is.EqualTo("Release"));
        }

        Assert.That(exceptions, Is.Empty, "A normal non-matching probe must not raise a first-chance WebException in the debugger.");
      }
      finally
      {
        AppDomain.CurrentDomain.FirstChanceException -= observe;
      }
    }

    [Test]
    public async Task JenkinsFoldersPreserveTheirProjectHierarchy()
    {
      await using var server = new DiscoveryServer();
      server.Respond = path => ("200 OK", "application/json", path.StartsWith("/job/", StringComparison.Ordinal)
        ? "{\"jobs\":[{\"name\":\"Release\",\"fullName\":\"Folder/Release\",\"url\":\"" + server.Address + "job/Folder/job/Release/\"}]}"
        : "{\"jobs\":[{\"name\":\"Folder\",\"_class\":\"com.cloudbees.hudson.plugins.folder.Folder\",\"url\":\"" + server.Address + "job/Folder/\"}]}");

      var projects = await new JenkinsPlugin().GetProjects(server.Address) ?? throw new AssertionException("Expected discovered projects.");

      var folder = projects.Single();
      var child = folder.Children.Single();
      Assert.That(child.Name, Is.EqualTo("Release"));
      Assert.That(child.FullName, Is.EqualTo("Folder/Release"));
      Assert.That(child.Parent, Is.SameAs(folder));
      Assert.That(server.Requests, Has.Count.EqualTo(2), "Older servers can omit nested data and must still be traversed.");
    }

    [Test]
    public async Task JenkinsNestedFoldersAndEmptyFoldersAreDiscoveredInOneRequest()
    {
      await using var server = new DiscoveryServer();
      server.Respond = path => ("200 OK", "application/json", $$"""
        {"jobs":[
          {"name":"Folder","_class":"com.cloudbees.hudson.plugins.folder.Folder","url":"{{server.Address}}job/Folder/","jobs":[
            {"name":"Pipeline","_class":"org.jenkinsci.plugins.workflow.multibranch.WorkflowMultiBranchProject","url":"{{server.Address}}job/Folder/job/Pipeline/","jobs":[
              {"name":"main","fullName":"Folder/Pipeline/main","url":"{{server.Address}}job/Folder/job/Pipeline/job/main/"}
            ]}
          ]},
          {"name":"Empty","_class":"com.cloudbees.hudson.plugins.folder.Folder","url":"{{server.Address}}job/Empty/","jobs":[]}
        ]}
        """);

      var projects = await new JenkinsPlugin().GetProjects(server.Address) ?? throw new AssertionException("Expected discovered projects.");

      var folder = projects.First();
      var pipeline = folder.Children.Single();
      var branch = pipeline.Children.Single();
      Assert.That(branch.FullName, Is.EqualTo("Folder/Pipeline/main"));
      Assert.That(branch.Parent, Is.SameAs(pipeline));
      Assert.That(pipeline.Parent, Is.SameAs(folder));
      Assert.That(projects.Last().Children, Is.Empty);
      Assert.That(server.Requests, Has.Count.EqualTo(1));
      Assert.That(Uri.UnescapeDataString(server.Requests.Single()), Does.Contain("jobs[_class,url,name,fullName,displayName,description,jobs["));
    }

    [Test]
    public async Task JenkinsStartupFetchesStatusAndHistoryTogetherAndLaterPollsStaySmall()
    {
      await using var server = new DiscoveryServer();
      var builds = Enumerable.Range(1, 6).Reverse().Select(number => new JenkinsBuild
      {
        Number = number,
        Result = "SUCCESS",
        Timestamp = number * 60000L,
      }).ToList();
      server.Respond = path => ("200 OK", "application/json", JsonConvert.SerializeObject(new JenkinsJob { LastBuild = builds.First(), Builds = builds }));
      var connector = new JenkinsConnector(
        new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Address, Connector.ProjectName, "Release"), new JenkinsApi());

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots.Select(snapshot => snapshot.Status.BuildNumber), Is.EqualTo(new[] { 6, 5, 4, 3, 2 }));
      Assert.That(server.Requests, Has.Count.EqualTo(1));
      Assert.That(Uri.UnescapeDataString(server.Requests.Single()), Does.Contain("builds[").And.EndWith("{0,6}"));

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(server.Requests, Has.Count.EqualTo(2));
      Assert.That(server.Requests.Select(Uri.UnescapeDataString).Count(path => path.Contains("builds[", StringComparison.Ordinal)), Is.EqualTo(1));
    }

    [Test]
    public async Task JenkinsFoldersBeyondTheBatchedDepthAreStillDiscovered()
    {
      await using var server = new DiscoveryServer();
      server.Respond = path => ("200 OK", "application/json", path.StartsWith("/api/", StringComparison.Ordinal) ? $$"""
        {"jobs":[{"name":"Top","_class":"com.cloudbees.hudson.plugins.folder.Folder","url":"{{server.Address}}job/Top/","jobs":[
          {"name":"Middle","_class":"com.cloudbees.hudson.plugins.folder.Folder","url":"{{server.Address}}job/Top/job/Middle/","jobs":[
            {"name":"Deep","_class":"com.cloudbees.hudson.plugins.folder.Folder","url":"{{server.Address}}job/Top/job/Middle/job/Deep/"}
          ]}
        ]}]}
        """ : $$"""
        {"jobs":[{"name":"Release","fullName":"Top/Middle/Deep/Release","url":"{{server.Address}}job/Top/job/Middle/job/Deep/job/Release/"}]}
        """);

      var top = (await new JenkinsPlugin().GetProjects(server.Address) ?? throw new AssertionException("Expected discovered projects.")).Single();
      var middle = top.Children.Single();
      var deep = middle.Children.Single();
      var project = deep.Children.Single();

      Assert.That(project.FullName, Is.EqualTo("Top/Middle/Deep/Release"));
      Assert.That(project.Parent, Is.SameAs(deep));
      Assert.That(deep.Parent, Is.SameAs(middle));
      Assert.That(middle.Parent, Is.SameAs(top));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
      Assert.That(server.Requests.Count(path => path.StartsWith("/job/Top/job/Middle/job/Deep/api/", StringComparison.Ordinal)), Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task JenkinsBatchRequestsRespectCancellation(bool history)
    {
      await using var server = new DiscoveryServer();
      var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      server.ResponseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
      server.Respond = path =>
      {
        received.TrySetResult();
        return ("200 OK", "application/json", "{}");
      };
      var api = new JenkinsApi();
      var connector = new JenkinsConnector(
        new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Address, Connector.ProjectName, "Release"), api);
      using var cancellation = new CancellationTokenSource();
      Task request = history ? api.GetBuilds(connector, cancellation.Token) : api.GetJenkinsJob(connector, cancellation.Token);
      await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

      cancellation.Cancel();

      Assert.That(async () => await request.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ConcurrentCruiseControlProjectsWaitForOneSharedReport(bool cancelWaitingProject)
    {
      await using var server = new DiscoveryServer();
      var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      server.ResponseGate = release.Task;
      server.Respond = path =>
      {
        received.TrySetResult();
        return ("200 OK", "text/xml", "<Projects><Project name='First' lastBuildLabel='1'/><Project name='Second' lastBuildLabel='2'/></Projects>");
      };
      var shared = CruiseControlManager.GetServer(server.Address);
      using var cancellation = new CancellationTokenSource();
      var first = shared.GetProjectStatus(CancellationToken.None, "First", 60);
      try
      {
        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = CruiseControlManager.GetServer(server.Address).GetProjectStatus(cancellation.Token, "Second", 60);
        Assert.That(second.IsCompleted, Is.False, "A project must await the pending server report rather than read an empty cache.");
        if (cancelWaitingProject)
        {
          cancellation.Cancel();
          Assert.ThrowsAsync<OperationCanceledException>(async () => await second);
        }

        release.TrySetResult();
        Assert.That((await first.WaitAsync(TimeSpan.FromSeconds(5)))?.LastBuildLabel, Is.EqualTo("1"));
        var secondStatus = cancelWaitingProject
          ? await shared.GetProjectStatus(CancellationToken.None, "Second", 60)
          : await second.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(secondStatus?.LastBuildLabel, Is.EqualTo("2"));
        Assert.That(server.Requests, Is.EquivalentTo(new[] { "/XmlStatusReport.aspx" }));
      }
      finally
      {
        release.TrySetResult();
      }
    }

    [Test]
    public async Task CreatingAJenkinsProjectWithoutBlueOceanDoesNotRaiseAWebException()
    {
      await using var server = new DiscoveryServer { Respond = path => ("404 Not Found", "text/html", "Not installed") };
      var exceptions = new ConcurrentBag<Exception>();
      EventHandler<FirstChanceExceptionEventArgs> observe = (sender, args) =>
      {
        if (args.Exception is WebException)
        {
          exceptions.Add(args.Exception);
        }
      };
      AppDomain.CurrentDomain.FirstChanceException += observe;
      try
      {
        var configuration = new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Address, Connector.ProjectName, "Release");

        var connector = new JenkinsPlugin().CreateNew(configuration, true);

        Assert.That(connector, Is.Not.Null);
        Assert.That(bool.TryParse(configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin).Value, out var redirect) && redirect, Is.False);
        Assert.That(server.Requests.Single(), Is.EqualTo("/job/Release/display/redirect"));
        Assert.That(exceptions, Is.Empty);
      }
      finally
      {
        AppDomain.CurrentDomain.FirstChanceException -= observe;
      }
    }

    [Test]
    public async Task MissingEndpointsFailPromptlyInsteadOfSelectingAnEmptyServer()
    {
      await using var server = new DiscoveryServer { Respond = path => ("404 Not Found", "text/plain", "Missing") };
      var query = WizardController.DiscoverProjects(
        new ConnectorPlugin[] { new JenkinsPlugin(), new CruiseControlPlugin() }, server.Address, TimeSpan.FromSeconds(10));

      Assert.That(await Task.WhenAny(query, Task.Delay(1000)), Is.SameAs(query));
      Assert.ThrowsAsync<InvalidOperationException>(async () => await query);
    }

    [Test]
    public async Task AnUnrecognizedJsonObjectDoesNotBecomeAnEmptyJenkinsServer()
    {
      await using var server = new DiscoveryServer { Respond = path => ("200 OK", "application/json", "{}") };

      Assert.That(await new JenkinsPlugin().GetProjects(server.Address), Is.Null);
    }

    [TestCase(true, "401 Unauthorized", HttpStatusCode.Unauthorized)]
    [TestCase(false, "401 Unauthorized", HttpStatusCode.Unauthorized)]
    [TestCase(true, "503 Service Unavailable", HttpStatusCode.ServiceUnavailable)]
    [TestCase(false, "503 Service Unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task AuthenticationAndServerFailuresKeepTheirHttpStatus(bool jenkins, string status, HttpStatusCode expected)
    {
      await using var server = new DiscoveryServer { Respond = path => (status, "text/plain", "Unavailable") };
      ConnectorPlugin plugin = jenkins ? new JenkinsPlugin() : new CruiseControlPlugin();

      var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await plugin.GetProjects(server.Address));

      Assert.That(exception?.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public async Task AnHtmlLoginPageDoesNotBecomeAnEmptyProjectServer()
    {
      await using var server = new DiscoveryServer { Respond = path => ("200 OK", "text/html", "<html><body>Sign in</body></html>") };

      Assert.That(await new JenkinsPlugin().GetProjects(server.Address), Is.Null);
      Assert.That(await new CruiseControlPlugin().GetProjects(server.Address), Is.Null);
    }

    [Test]
    public async Task AllUnrecognizedPluginsFailPromptlyEvenWhenTheyShareACompletedTask()
    {
      var plugins = new[] { new DiscoveryPlugin(() => Task.FromResult<IList<Project>?>(null)), new DiscoveryPlugin(() => Task.FromResult<IList<Project>?>(null)) };
      var query = WizardController.DiscoverProjects(plugins, "https://build.example.invalid/", TimeSpan.FromSeconds(10));

      Assert.That(await Task.WhenAny(query, Task.Delay(1000)), Is.SameAs(query));
      Assert.ThrowsAsync<InvalidOperationException>(async () => await query);
    }

    [Test]
    public async Task OneFailingPluginDoesNotPreventAnotherFromDiscoveringProjects()
    {
      var projects = new List<Project> { new Project("https://build.example.invalid/", "Release", "https://build.example.invalid/job/Release/") };
      var failure = new DiscoveryPlugin(() => throw new HttpRequestException("Not a matching API"));
      var success = new DiscoveryPlugin(() => Task.FromResult<IList<Project>?>(projects));

      var discovered = await WizardController.DiscoverProjects(new[] { failure, success }, "https://build.example.invalid/", TimeSpan.FromSeconds(1));

      Assert.That(discovered.Plugin, Is.SameAs(success));
      Assert.That(discovered.Projects, Is.SameAs(projects));
    }

    [Test]
    public async Task AStalledProbeTimesOutInsteadOfSpinningOrKeepingTheWizardBusyForever()
    {
      var completion = new TaskCompletionSource<IList<Project>?>(TaskCreationOptions.RunContinuationsAsynchronously);
      var query = WizardController.DiscoverProjects(
        new[] { new DiscoveryPlugin(() => completion.Task) }, "https://build.example.invalid/", TimeSpan.FromMilliseconds(30));
      try
      {
        Assert.That(await Task.WhenAny(query, Task.Delay(1000)), Is.SameAs(query));
        Assert.ThrowsAsync<TimeoutException>(async () => await query);
      }
      finally
      {
        completion.TrySetResult(null);
      }
    }

    internal static void VerifyWizardNextButton()
    {
      var server = new DiscoveryServer();
      server.Respond = path => path.StartsWith("/api/json", StringComparison.Ordinal)
        ? ("200 OK", "application/json", "{\"jobs\":[{\"name\":\"Release\",\"url\":\"" + server.Address + "job/Release/\"}]}")
        : ("404 Not Found", "text/html", "<html>Not found</html>");
      var controller = new WizardController(null, new ApplicationConfiguration(), new WindowSettings()) { ProposedServerAddress = server.Address };
      var window = new WizardWindow(controller) { Left = -20000, Top = -20000, ShowActivated = false };
      var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
      var previousContext = SynchronizationContext.Current;
      try
      {
        (typeof(WizardController).GetField("wizardWindow", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).SetValue(controller, window);
        (typeof(WizardController).GetMethod("GoToConnectionStep", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).Invoke(controller, null);
        window.Show();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(window.Dispatcher));
        ((Button)window.FindName("NextButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var frame = new DispatcherFrame();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        timer.Tick += (sender, args) =>
        {
          if (window.Frame.Content is ProjectSelectionWizardPage || DateTime.UtcNow >= deadline)
          {
            frame.Continue = false;
          }
        };
        timer.Start();
        Dispatcher.PushFrame(frame);

        Assert.That(window.Frame.Content, Is.InstanceOf<ProjectSelectionWizardPage>(), "Next must reach project selection despite the other build system's 404.");
        Assert.That(controller.Projects.Single().Name, Is.EqualTo("Release"));
        Assert.That(((MaterialDesignThemes.Wpf.DialogHost)window.FindName("WizardWaitDialogHost")).IsOpen, Is.False);
      }
      finally
      {
        timer.Stop();
        window.Close();
        server.DisposeAsync().AsTask().GetAwaiter().GetResult();
        SynchronizationContext.SetSynchronizationContext(previousContext);
      }
    }

    internal sealed class DiscoveryServer : IAsyncDisposable
    {
      private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      private readonly CancellationTokenSource stopping = new CancellationTokenSource();
      private readonly Task requests;

      public DiscoveryServer()
      {
        this.listener.Start();
        this.Address = $"http://127.0.0.1:{((IPEndPoint)this.listener.LocalEndpoint).Port}/";
        this.requests = this.Serve();
      }

      public string Address { get; }

      public ConcurrentBag<string> Requests { get; } = new ConcurrentBag<string>();

      public Func<string, (string Status, string ContentType, string Body)> Respond { get; set; } = _ => ("404 Not Found", "text/plain", "Not found");

      public Task ResponseGate { get; set; } = Task.CompletedTask;

      public bool Chunked { get; set; }

      public bool Gzip { get; set; }

      public async ValueTask DisposeAsync()
      {
        this.stopping.Cancel();
        this.listener.Stop();
        await this.requests.ConfigureAwait(false);
        this.stopping.Dispose();
      }

      private async Task Serve()
      {
        try
        {
          while (!this.stopping.IsCancellationRequested)
          {
            using var client = await this.listener.AcceptTcpClientAsync(this.stopping.Token).ConfigureAwait(false);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
            var request = await reader.ReadLineAsync(this.stopping.Token).ConfigureAwait(false);
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync(this.stopping.Token).ConfigureAwait(false)))
            {
            }

            if (string.IsNullOrEmpty(request))
            {
              continue;
            }

            var path = request.Split(' ')[1];
            this.Requests.Add(path);
            var response = this.Respond(path);
            await this.ResponseGate.WaitAsync(this.stopping.Token).ConfigureAwait(false);
            var body = Encoding.UTF8.GetBytes(response.Body);
            if (this.Gzip)
            {
              using var compressed = new MemoryStream();
              using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, true))
              {
                gzip.Write(body);
              }

              body = compressed.ToArray();
            }

            var length = this.Chunked ? "Transfer-Encoding: chunked" : $"Content-Length: {body.Length}";
            var encoding = this.Gzip ? "Content-Encoding: gzip\r\n" : string.Empty;
            var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {response.Status}\r\nContent-Type: {response.ContentType}\r\n{length}\r\n{encoding}Connection: close\r\n\r\n");
            try
            {
              await stream.WriteAsync(headers, this.stopping.Token).ConfigureAwait(false);
              if (this.Chunked)
              {
                await stream.WriteAsync(Encoding.ASCII.GetBytes(body.Length.ToString("X", System.Globalization.CultureInfo.InvariantCulture) + "\r\n"), this.stopping.Token).ConfigureAwait(false);
              }

              await stream.WriteAsync(body, this.stopping.Token).ConfigureAwait(false);
              if (this.Chunked)
              {
                await stream.WriteAsync(Encoding.ASCII.GetBytes("\r\n0\r\n\r\n"), this.stopping.Token).ConfigureAwait(false);
              }
            }
            catch (IOException)
            {
              // Size-limited clients may close the connection before the entire test response is written.
            }
          }
        }
        catch (OperationCanceledException) when (this.stopping.IsCancellationRequested)
        {
        }
        catch (SocketException) when (this.stopping.IsCancellationRequested)
        {
        }
      }
    }

    private class DiscoveryPlugin : ConnectorPlugin
    {
      private readonly Func<Task<IList<Project>?>> query;

      public DiscoveryPlugin(Func<Task<IList<Project>?>> query, Type? connectorType = null)
        : base(connectorType ?? typeof(JenkinsConnector)) => this.query = query;

      public override Task<IList<Project>?> GetProjects(string address) => this.query();

      public override Connector CreateNew(ConnectorConfiguration configuration, bool? checkRedirect = null) => throw new NotSupportedException();

      public override void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string? serverAddress = null) => throw new NotSupportedException();
    }
  }
}
