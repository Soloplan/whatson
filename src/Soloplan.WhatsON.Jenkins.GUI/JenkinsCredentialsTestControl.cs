// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsCredentialsTestControl.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.ComponentModel;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Net.Http;
  using System.Security.Cryptography;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Automation;
  using System.Windows.Controls;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.Localization;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  public class JenkinsCredentialsTestControl : StackPanel
  {
    private readonly IConfigurationItem[] settings;
    private readonly Func<JenkinsConnector, CancellationToken, Task<string>> test;
    private readonly TextBlock result = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };
    private CancellationTokenSource? pending;

    public JenkinsCredentialsTestControl(IConfigurationItemProvider configuration)
      : this(configuration, new JenkinsApi().TestAuthentication)
    {
    }

    internal JenkinsCredentialsTestControl(IConfigurationItemProvider configuration, Func<JenkinsConnector, CancellationToken, Task<string>> test)
    {
      this.test = test;
      this.settings = new[] { Connector.ServerAddress, JenkinsConnector.UserName, JenkinsConnector.ApiToken }
        .Select(configuration.GetConfigurationByKey).ToArray();
      this.TestButton = new Button { HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 110 };
      this.TestButton.SetBinding(ToolTipProperty, TextExtension.CreateBinding(() => Strings.JenkinsCredentialsRequired));
      ToolTipService.SetShowOnDisabled(this.TestButton, true);
      AutomationProperties.SetLiveSetting(this.result, AutomationLiveSetting.Polite);
      this.TestButton.Click += async (_, _) => await this.TestAsync();
      this.Children.Add(this.TestButton);
      this.Children.Add(this.result);
      foreach (var item in this.settings.OfType<INotifyPropertyChanged>())
      {
        PropertyChangedEventManager.AddHandler(item, this.SettingsChanged, nameof(IConfigurationItem.Value));
      }

      this.Unloaded += (_, _) => this.CancelTest();
      this.RefreshButton();
    }

    internal Button TestButton { get; }

    internal string ResultText => this.result.Text;

    internal async Task TestAsync()
    {
      if (this.pending != null || this.settings.Any(item => string.IsNullOrWhiteSpace(item.Value)))
      {
        return;
      }

      using var cancellation = new CancellationTokenSource();
      this.pending = cancellation;
      this.RefreshButton();
      this.SetResult(() => Strings.JenkinsCredentialsTesting);
      var snapshot = new ConnectorConfiguration(JenkinsConnector.ConnectorName);
      foreach (var item in this.settings)
      {
        snapshot.ConfigurationItems.Add(new ConfigurationItem(item.Key) { Value = item.Value });
      }

      Func<string> message;
      try
      {
        var name = await this.test(new JenkinsConnector(snapshot, new JenkinsApi()), cancellation.Token);
        message = () => string.Format(Strings.JenkinsCredentialsValid, name);
      }
      catch (OperationCanceledException)
      {
        message = () => Strings.JenkinsCredentialsTimedOut;
      }
      catch (HttpRequestException exception)
      {
        message = exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
          ? () => Strings.JenkinsCredentialsRejected
          : () => Strings.JenkinsCredentialsConnectionFailed;
      }
      catch (Exception exception) when (exception is CryptographicException or IOException or FormatException or UnauthorizedAccessException)
      {
        message = () => Strings.TokenCannotBeDecrypted;
      }
      catch (Exception exception) when (exception is JsonException or InvalidPlugInApiResponseException)
      {
        message = () => Strings.JenkinsCredentialsInvalidResponse;
      }
      catch (InvalidOperationException)
      {
        message = () => Strings.JenkinsCredentialsInvalidSettings;
      }
      finally
      {
        this.pending = null;
        this.RefreshButton();
      }

      if (!cancellation.IsCancellationRequested)
      {
        this.SetResult(message);
      }
    }

    private void SettingsChanged(object? sender, PropertyChangedEventArgs args)
    {
      this.CancelTest();
      this.RefreshButton();
    }

    private void CancelTest()
    {
      this.pending?.Cancel();
      this.SetResult(() => string.Empty);
    }

    private void SetResult(Func<string> message)
    {
      this.result.SetBinding(TextBlock.TextProperty, TextExtension.CreateBinding(message));
      this.result.Visibility = string.IsNullOrEmpty(this.result.Text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RefreshButton()
    {
      this.TestButton.IsEnabled = this.pending == null && this.settings.All(item => !string.IsNullOrWhiteSpace(item.Value));
      this.TestButton.SetBinding(ContentControl.ContentProperty, TextExtension.CreateBinding(
        () => this.pending == null ? Strings.TestApiCredentials : Strings.JenkinsCredentialsTesting));
    }
  }
}
