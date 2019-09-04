// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using NUnit.Framework;

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
  }
}