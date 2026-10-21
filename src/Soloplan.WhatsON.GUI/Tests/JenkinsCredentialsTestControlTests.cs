// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsCredentialsTestControlTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Net;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.GUI;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class JenkinsCredentialsTestControlTests
  {
    [Test]
    public void MissingSettingsDisableTestingAndEditsEnableItWithoutSaving()
    {
      var source = new ConnectorConfiguration(JenkinsConnector.ConnectorName);
      var settings = new ConnectorViewModel();
      settings.Load(source);
      var calls = 0;
      var control = new JenkinsCredentialsTestControl(settings, (_, _) =>
      {
        calls++;
        return Task.FromResult("alex");
      });
      Assert.That(control.TestButton.IsEnabled, Is.False);
      Assert.That(control.TestButton.HorizontalAlignment, Is.EqualTo(HorizontalAlignment.Right));
      Assert.That(control.Children[1].Visibility, Is.EqualTo(Visibility.Collapsed));
      Assert.That(ToolTipService.GetShowOnDisabled(control.TestButton), Is.True);
      Assert.That(control.TestButton.ToolTip, Is.EqualTo(Strings.JenkinsCredentialsRequired));
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(calls, Is.Zero);

      settings.GetConfigurationByKey(Connector.ServerAddress).Value = "https://ci.example.invalid/jenkins";
      settings.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      settings.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-placeholder";
      Assert.That(control.TestButton.IsEnabled, Is.True);
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(calls, Is.EqualTo(1));
      Assert.That(control.ResultText, Is.EqualTo(string.Format(Strings.JenkinsCredentialsValid, "alex")));
      Assert.That(control.Children[1].Visibility, Is.EqualTo(Visibility.Visible));
      Assert.That(source.GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.Null.Or.Empty, "Testing must not save the edited settings.");

      settings.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = string.Empty;
      Assert.That(control.TestButton.IsEnabled, Is.False);
      Assert.That(control.ResultText, Is.Empty, "A previous success must disappear when credentials change.");
      Assert.That(control.Children[1].Visibility, Is.EqualTo(Visibility.Collapsed));
    }

    [Test]
    public void TestUsesSnapshotAndSuppressesResultsAfterSettingsChange()
    {
      var settings = CreateSettings();
      JenkinsCredentialsTestControl? control = null;
      control = new JenkinsCredentialsTestControl(settings, (connector, token) =>
      {
        Assert.That(control?.TestButton.IsEnabled, Is.False);
        Assert.That(connector.AuthenticationUser, Is.EqualTo("alex"));
        Assert.That(connector.EncryptedApiToken, Is.EqualTo("encrypted-placeholder"));
        settings.GetConfigurationByKey(JenkinsConnector.UserName).Value = "sam";
        Assert.That(token.IsCancellationRequested, Is.True);
        Assert.That(connector.AuthenticationUser, Is.EqualTo("alex"), "The in-flight request must use an immutable snapshot.");
        return Task.FromResult("alex");
      });
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(control.ResultText, Is.Empty);
      Assert.That(control.TestButton.IsEnabled, Is.True);
    }

    [Test]
    public void LeavingSettingsCancelsTheTestAndSuppressesItsResult()
    {
      JenkinsCredentialsTestControl? control = null;
      control = new JenkinsCredentialsTestControl(CreateSettings(), (_, token) =>
      {
        control?.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert.That(token.IsCancellationRequested, Is.True);
        return Task.FromResult("alex");
      });
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(control.ResultText, Is.Empty);
    }

    [Test]
    public void PendingTestPreventsDuplicateRequestsAndAllowsRetryAfterCompletion()
    {
      var previousContext = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
      try
      {
        var response = new TaskCompletionSource<string>();
        var calls = 0;
        var control = new JenkinsCredentialsTestControl(CreateSettings(), (_, _) =>
        {
          calls++;
          return response.Task;
        });
        var pending = control.TestAsync();
        Assert.That(control.TestButton.IsEnabled, Is.False);
        control.TestAsync().GetAwaiter().GetResult();
        Assert.That(calls, Is.EqualTo(1));
        response.SetResult("alex");
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.That(pending.IsCompletedSuccessfully, Is.True);
        Assert.That(control.TestButton.IsEnabled, Is.True);
        Assert.That(control.ResultText, Is.EqualTo(string.Format(Strings.JenkinsCredentialsValid, "alex")));
      }
      finally
      {
        SynchronizationContext.SetSynchronizationContext(previousContext);
      }
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void FailuresAreReadableAndNeverExposeExceptionDetails(HttpStatusCode status)
    {
      var control = new JenkinsCredentialsTestControl(CreateSettings(), (_, _) =>
        Task.FromException<string>(new HttpRequestException("secret-token-do-not-display", null, status)));
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(control.TestButton.IsEnabled, Is.True);
      Assert.That(control.ResultText, Is.EqualTo(status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        ? Strings.JenkinsCredentialsRejected : Strings.JenkinsCredentialsConnectionFailed));
      Assert.That(control.ResultText, Does.Not.Contain("secret-token"));
    }

    [Test]
    public void TimeoutsAllowRetry()
    {
      var control = new JenkinsCredentialsTestControl(CreateSettings(), (_, _) =>
        Task.FromException<string>(new TaskCanceledException()));
      control.TestAsync().GetAwaiter().GetResult();
      Assert.That(control.ResultText, Is.EqualTo(Strings.JenkinsCredentialsTimedOut));
      Assert.That(control.TestButton.IsEnabled, Is.True);
    }

    [Test]
    public void OnlyJenkinsApiTokensOfferTheCredentialTest()
    {
      var settings = CreateSettings();
      var jenkins = new JenkinsPresentationPlugin();
      Assert.That(jenkins.CreateConfigurationAction(JenkinsConnector.ApiToken, settings), Is.InstanceOf<JenkinsCredentialsTestControl>());
      Assert.That(jenkins.CreateConfigurationAction(JenkinsConnector.UserName, settings), Is.Null);
      Assert.That(new CruiseControlPresentationPlugin().CreateConfigurationAction(JenkinsConnector.ApiToken, settings), Is.Null);
    }

    private static ConnectorViewModel CreateSettings()
    {
      var settings = new ConnectorViewModel();
      settings.Load(new ConnectorConfiguration(JenkinsConnector.ConnectorName));
      settings.GetConfigurationByKey(Connector.ServerAddress).Value = "https://ci.example.invalid";
      settings.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      settings.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-placeholder";
      return settings;
    }
  }
}
