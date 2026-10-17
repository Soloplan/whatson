// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonApiTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Net;
  using System.Net.Sockets;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;

  [TestFixture]
  [NonParallelizable]
  public class JsonApiTests
  {
    private TcpListener listener;
    private Task responseTask;
    private TaskCompletionSource<string> receivedRequest;
    private TaskCompletionSource<bool> sendResponse;
    private IWebProxy originalProxy;
    private bool disposing;

    [SetUp]
    public void SetUp()
    {
      this.originalProxy = WebRequest.DefaultWebProxy;
      this.receivedRequest = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.sendResponse = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.disposing = false;
    }

    [TearDown]
    public async Task TearDown()
    {
      try
      {
        this.disposing = true;
        this.sendResponse.TrySetResult(true);
        this.listener?.Stop();
        if (this.responseTask != null)
        {
          Assert.That(await Task.WhenAny(this.responseTask, Task.Delay(5000)), Is.SameAs(this.responseTask), "Local HTTP responder did not stop.");
          await this.responseTask;
        }
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

      var result = await new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address, requestCallback: request => request.Headers["X-Test-Api"] = "synthetic-test-value");
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

      Assert.That(async () => await new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address), Throws.TypeOf<InvalidPlugInApiResponseException>());
    }

    [TestCase("{broken")]
    [TestCase("{\"build\":\"not-a-number\"}")]
    [TestCase("[1,2,3]")]
    public void MalformedOrIncompatibleJsonIsReportedAsAnInvalidApiResponse(string json)
    {
      var address = this.StartServer(json);

      var exception = Assert.ThrowsAsync<InvalidPlugInApiResponseException>(async () => await new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address));

      Assert.That(exception.InnerException, Is.InstanceOf<Newtonsoft.Json.JsonException>());
    }

    [TestCase("401 Unauthorized")]
    [TestCase("503 Service Unavailable")]
    public void AuthenticationAndServerErrorsAreReportedInsteadOfReturningDefaultData(string status)
    {
      var address = this.StartServer("{\"build\":42}", status: status);

      var exception = Assert.ThrowsAsync<WebException>(async () => await new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address));

      Assert.That(exception.Status, Is.EqualTo(WebExceptionStatus.ProtocolError));
      exception.Response?.Dispose();
    }

    [Test]
    public void UnexpectedSuccessfulHttpStatusIsStillRejected()
    {
      var address = this.StartServer("{\"build\":42}", status: "201 Created");

      Assert.That(async () => await new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address), Throws.TypeOf<InvalidPlugInApiResponseException>());
    }

    [Test]
    public async Task CancellationAbortsAnInFlightRequestAndPreservesItsCancellationToken()
    {
      var address = this.StartServer("{\"build\":42}", holdResponse: true);
      using (var cancellation = new CancellationTokenSource())
      {
        var query = Task.Run(() => new SerializationHelper().GetJsonModel<Dictionary<string, int>>(address, cancellation.Token));
        Assert.That(await Task.WhenAny(this.receivedRequest.Task, Task.Delay(5000)), Is.SameAs(this.receivedRequest.Task), "Request did not reach the local responder.");

        cancellation.Cancel();

        Assert.That(await Task.WhenAny(query, Task.Delay(5000)), Is.SameAs(query), "Cancelled request did not finish.");
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await query);
        Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
      }
    }

    private string StartServer(string body, string contentType = "application/json", string status = "200 OK", bool holdResponse = false)
    {
      this.listener = new TcpListener(IPAddress.Loopback, 0);
      this.listener.Start();
      if (!holdResponse)
      {
        this.sendResponse.SetResult(true);
      }

      this.responseTask = Task.Run(async () =>
      {
        try
        {
          using (var client = await this.listener.AcceptTcpClientAsync())
          using (var stream = client.GetStream())
          using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
          {
            var headers = new StringBuilder();
            string line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
              headers.AppendLine(line);
            }

            this.receivedRequest.TrySetResult(headers.ToString());
            await this.sendResponse.Task;
            var bytes = Encoding.UTF8.GetBytes(body);
            var headerBytes = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(bytes, 0, bytes.Length);
          }
        }
        catch (Exception ex) when (this.disposing && (ex is IOException || ex is SocketException || ex is ObjectDisposedException))
        {
          // Stopping the listener or an aborted client can close the responder during cleanup.
        }
      });

      return $"http://127.0.0.1:{((IPEndPoint)this.listener.LocalEndpoint).Port}/api/json";
    }
  }
}
