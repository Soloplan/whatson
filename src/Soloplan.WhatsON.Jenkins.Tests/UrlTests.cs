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
  using Rhino.Mocks;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class UrlTests
  {
    [Test]
    public void EnsureThatInsecureUrlIsSupported()
    {
      var address = "http://www.my-little-jenkins.com";
      var address2 = "http://my-little-jenkins.com";

      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com/api/json?"));
      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("http://my-little-jenkins.com/api/json?"));
    }

    [Test]
    public void EnsureThatSecureUrlIsSupported()
    {
      var address = "https://www.my-little-jenkins.com";
      var address2 = "https://my-little-jenkins.com";

      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("https://www.my-little-jenkins.com/api/json?"));
      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("https://my-little-jenkins.com/api/json?"));
    }

    [Test]
    public void EnsureThatCustomPortsAreSupported()
    {
      var address = "http://www.my-little-jenkins.com:8080";
      var address2 = "https://www.my-little-jenkins.com:8080";

      var address3 = "http://my-little-jenkins.com:8080";
      var address4 = "https://my-little-jenkins.com:8080";

      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com:8080/api/json?"));
      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address2).StartsWith("https://www.my-little-jenkins.com:8080/api/json?"));

      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address3).StartsWith("http://my-little-jenkins.com:8080/api/json?"));
      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address4).StartsWith("https://my-little-jenkins.com:8080/api/json?"));
    }

    [Test]
    public void EnsureThatSubdirectoriesAreSupported()
    {
      var address = "http://www.my-little-jenkins.com/here-i-am";
      Assert.IsTrue(JenkinsApi.UrlHelper.JobsRequest(address).StartsWith("http://www.my-little-jenkins.com/here-i-am/api/json?"));
    }

    [Test]
    public void EnsureProjectNamesAreProperlyConvertedToUrl()
    {
      var api = MockRepository.GenerateMock<IJenkinsApi>();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
        {
          new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
          new ConfigurationItem(Connector.ProjectName) { Value = "my-fancy-build-project" },
        });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.AreEqual("https://www.my-little-jenkins.com/job/my-fancy-build-project", url);
    }

    [Test]
    public void EnsureProjectsInFoldersAreProperlyConvertedToUrl()
    {
      var api = MockRepository.GenerateMock<IJenkinsApi>();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "folder/my-fancy-build-project" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.AreEqual("https://www.my-little-jenkins.com/job/folder/job/my-fancy-build-project", url);
    }

    [Test]
    public void EnsureProjectsWithSpecialCharsAreProperlyConvertedToUrl()
    {
      var api = MockRepository.GenerateMock<IJenkinsApi>();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "playground/strâng335 sads== Jöb NAME" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.AreEqual("https://www.my-little-jenkins.com/job/playground/job/str%C3%A2ng335%20sads==%20J%C3%B6b%20NAME", url);
    }

    [Test]
    public void EnsureLegacyProjectNameFormatPriorTo_0_9_1_Works()
    {
      var api = MockRepository.GenerateMock<IJenkinsApi>();
      var config = new ConnectorConfiguration(JenkinsConnector.ConnectorName, "my project", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://www.my-little-jenkins.com" },
        new ConfigurationItem(Connector.ProjectName) { Value = "folder1/job/folder2/job/my-job" },
      });

      var connector = new JenkinsConnector(config, api);

      var url = JenkinsApi.UrlHelper.ProjectUrl(connector);

      Assert.AreEqual("https://www.my-little-jenkins.com/job/folder1/job/folder2/job/my-job", url);
    }
  }
}