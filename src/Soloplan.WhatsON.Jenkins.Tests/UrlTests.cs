// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System.Collections.Generic;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class UrlTests
  {
    [TestCase("build-jobs/Release", "https://builds.example/job/build-jobs/job/Release")]
    [TestCase("folder/Release #1?", "https://builds.example/job/folder/job/Release%20%231%3F")]
    public void NestedProjectNamesRemainPathSegments(string project, string expected)
    {
      var connector = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example/", Connector.ProjectName, project), new FakeJenkinsApi());

      Assert.That(JenkinsApi.UrlHelper.ProjectUrl(connector), Is.EqualTo(expected));
    }

    [Test]
    public void RedirectPluginCanBeDisabledForAProjectLink()
    {
      var connector = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "Release", JenkinsConnector.RedirectPlugin, "true"), new FakeJenkinsApi());

      Assert.That(JenkinsApi.UrlHelper.ProjectUrl(connector), Is.EqualTo("https://builds.example/job/Release/display/redirect"));
      Assert.That(JenkinsApi.UrlHelper.ProjectUrl(connector, false), Is.EqualTo("https://builds.example/job/Release"));
    }

    [Test]
    public void ApiBuildRequestsNeverUseBrowserRedirectUrls()
    {
      var connector = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "Release", JenkinsConnector.RedirectPlugin, "true"), new FakeJenkinsApi());

      Assert.That(JenkinsApi.UrlHelper.BuildUrl(connector, 42), Is.EqualTo("https://builds.example/job/Release/42/display/redirect"));
      Assert.That(JenkinsApi.UrlHelper.BuildRequest(connector, 42), Does.StartWith("https://builds.example/job/Release/42/api/json?"));
      Assert.That(JenkinsApi.UrlHelper.JobRequest(connector), Does.StartWith("https://builds.example/job/Release/api/json?"));
    }

    [Test]
    public void EnsureThatInsecureUrlIsSupported()
    {
      var address = "http://www.my-little-jenkins.com";
      var address2 = "http://my-little-jenkins.com";

      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com/api/json?"), Is.True);
      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("http://my-little-jenkins.com/api/json?"), Is.True);
    }

    [Test]
    public void EnsureThatSecureUrlIsSupported()
    {
      var address = "https://www.my-little-jenkins.com";
      var address2 = "https://my-little-jenkins.com";

      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("https://www.my-little-jenkins.com/api/json?"), Is.True);
      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("https://my-little-jenkins.com/api/json?"), Is.True);
    }

    [Test]
    public void EnsureThatCustomPortsAreSupported()
    {
      var address = "http://www.my-little-jenkins.com:8080";
      var address2 = "https://www.my-little-jenkins.com:8080";

      var address3 = "http://my-little-jenkins.com:8080";
      var address4 = "https://my-little-jenkins.com:8080";

      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com:8080/api/json?"), Is.True);
      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("https://www.my-little-jenkins.com:8080/api/json?"), Is.True);

      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address3).StartsWith("http://my-little-jenkins.com:8080/api/json?"), Is.True);
      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address4).StartsWith("https://my-little-jenkins.com:8080/api/json?"), Is.True);
    }

    [Test]
    public void EnsureThatSubdirectoriesAreSupported()
    {
      var address = "http://www.my-little-jenkins.com/here-i-am";
      Assert.That(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com/here-i-am/api/json?"), Is.True);
    }

    [Test]
    public void EnsureProjectNamesAreProperlyConvertedToUrl()
    {
      var api = new FakeJenkinsApi();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
        {
          new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
          new ConfigurationItem(Connector.ProjectName) { Value = "my-fancy-build-project" },
        });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.That(url, Is.EqualTo("https://www.my-little-jenkins.com/job/my-fancy-build-project"));
    }

    [Test]
    public void EnsureProjectsInFoldersAreProperlyConvertedToUrl()
    {
      var api = new FakeJenkinsApi();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "folder/my-fancy-build-project" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.That(url, Is.EqualTo("https://www.my-little-jenkins.com/job/folder/job/my-fancy-build-project"));
    }

    [Test]
    public void EnsureProjectsWithSpecialCharsAreProperlyConvertedToUrl()
    {
      var api = new FakeJenkinsApi();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "playground/strâng335 sads== Jöb NAME" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.That(url, Is.EqualTo("https://www.my-little-jenkins.com/job/playground/job/str%C3%A2ng335%20sads%3D%3D%20J%C3%B6b%20NAME"));
    }

    [Test]
    public void EnsureLegacyProjectNameFormatPriorTo_0_9_1_Works()
    {
      var api = new FakeJenkinsApi();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "folder1/job/folder2/job/my-job" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.That(url, Is.EqualTo("https://www.my-little-jenkins.com/job/folder1/job/folder2/job/my-job"));
    }
  }
}
