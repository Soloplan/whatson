// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonApiTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;

  [TestFixture]
  [NonParallelizable]
  public class JsonApiTests
  {
    private WireMockServer? server;

    private TaskCompletionSource<string> receivedRequest = new TaskCompletionSource<string>();

    private TaskCompletionSource<bool> sendResponse = new TaskCompletionSource<bool>();

    private IWebProxy? originalProxy;

    [SetUp]
    public void SetUp()
    {
      this.originalProxy = WebRequest.DefaultWebProxy;
      this.receivedRequest = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.sendResponse = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [TearDown]
    public void TearDown()
    {
      try
      {
        this.sendResponse.TrySetResult(true);
        this.server?.Dispose();
      }
      finally
      {
        WebRequest.DefaultWebProxy = this.originalProxy;
      }
    }

    [Test]
    public async Task JsonResponseWithACharsetIsDeserializedAndRequestHeadersCanBeCustomized()
    {
      var address = this.StartServer("{\"build\":42}", "application/json; charset=utf-8");

      var result = await new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(
        address,
        requestCallback: request => request.Headers["X-Test-Api"] = "synthetic-test-value");
      var headers = await this.receivedRequest.Task;

      Assert.That(result["build"], Is.EqualTo(42));
      Assert.That(headers, Does.Contain("Accept: application/json"));
      Assert.That(headers, Does.Contain("X-Test-Api: synthetic-test-value"));
    }

    [TestCase("text/html")]
    [TestCase("text/plain")]
    public void HtmlLoginPagesAndPlainTextErrorsAreNotAcceptedAsApiData(string contentType)
    {
      var address = this.StartServer("{\"build\":42}", contentType);

      Assert.That(
        async () => await new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(address),
        Throws.TypeOf<InvalidPlugInApiResponseException>());
    }

    [TestCase("{broken")]
    [TestCase("null")]
    [TestCase("{\"build\":\"not-a-number\"}")]
    [TestCase("[1,2,3]")]
    public void MalformedOrIncompatibleJsonIsReportedAsAnInvalidApiResponse(string json)
    {
      var address = this.StartServer(json);

      var exception =
        Assert.ThrowsAsync<InvalidPlugInApiResponseException>(async () =>
          await new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(address));

      Assert.That(exception?.InnerException, Is.InstanceOf<Newtonsoft.Json.JsonException>());
    }

    [TestCase("401 Unauthorized")]
    [TestCase("503 Service Unavailable")]
    public void AuthenticationAndServerErrorsAreReportedInsteadOfReturningDefaultData(string status)
    {
      var address = this.StartServer("{\"build\":42}", status: status);

      var exception = Assert.ThrowsAsync<WebException>(async () => await new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(address));

      Assert.That(exception?.Status, Is.EqualTo(WebExceptionStatus.ProtocolError));
      exception?.Response?.Dispose();
    }

    [Test]
    public void UnexpectedSuccessfulHttpStatusIsStillRejected()
    {
      var address = this.StartServer("{\"build\":42}", status: "201 Created");

      Assert.That(
        async () => await new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(address),
        Throws.TypeOf<InvalidPlugInApiResponseException>());
    }

    [Test]
    public async Task CancellationAbortsAnInFlightRequestAndPreservesItsCancellationToken()
    {
      var address = this.StartServer("{\"build\":42}", holdResponse: true);
      using (var cancellation = new CancellationTokenSource())
      {
        var query = new SerializationHelper().GetJsonModelAsync<Dictionary<string, int>>(address, cancellation.Token);
        Assert.That(
          await Task.WhenAny(this.receivedRequest.Task, Task.Delay(5000)),
          Is.SameAs(this.receivedRequest.Task),
          "Request did not reach the local responder.");

        cancellation.Cancel();

        Assert.That(await Task.WhenAny(query, Task.Delay(5000)), Is.SameAs(query), "Cancelled request did not finish.");
        var exception = Assert.CatchAsync<OperationCanceledException>(async () => await query);
        Assert.That(exception?.CancellationToken, Is.EqualTo(cancellation.Token));
      }
    }

    private string StartServer(string body, string contentType = "application/json", string status = "200 OK", bool holdResponse = false)
    {
      this.server = WireMockServer.Start();
      if (!holdResponse)
      {
        this.sendResponse.SetResult(true);
      }

      this.server.Given(Request.Create().WithPath("/api/json").UsingGet()).RespondWith(Response.Create().WithCallback(async request =>
      {
        this.receivedRequest.TrySetResult(string.Join(Environment.NewLine,
          request.Headers?.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}") ?? Array.Empty<string>()));
        await this.sendResponse.Task.ConfigureAwait(false);
        return Response.Create().WithStatusCode(int.Parse(status.AsSpan(0, 3), CultureInfo.InvariantCulture))
          .WithHeader("Content-Type", contentType).WithBody(body).ResponseMessage;
      }));

      return this.server.Urls[0] + "/api/json";
    }
  }
}
