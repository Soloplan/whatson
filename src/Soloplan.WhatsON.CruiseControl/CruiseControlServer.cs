// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlServer.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml.Serialization;
  using Soloplan.WhatsON.CruiseControl.Model;

  public class CruiseControlServer
  {
    private string address;

    private DateTime lastPoolled;

    private CruiseControlJobs cache;

    public CruiseControlServer(string address)
    {
      this.address = address.Trim('/') + "/XmlStatusReport.aspx";
    }

    public async Task<CruiseControlJob> GetProjectStatus(CancellationToken cancellationToken, string projectName, int interval)
    {
      var pollInterval = new TimeSpan(0, 0, 0, interval, 0);
      if (DateTime.Now - this.lastPoolled > pollInterval)
      {
        try
        {
          this.cache = await this.GetStatusAsync<CruiseControlJobs>(cancellationToken, this.address);
        }
        finally
        {
          this.lastPoolled = DateTime.Now;
        }
      }

      return this.cache.CruiseControlProject.FirstOrDefault(job => job.Name == projectName);
    }

    private async Task<TModel> GetStatusAsync<TModel>(CancellationToken cancellationToken, string requestUrl)
    where TModel : class
    {
      var request = WebRequest.Create(requestUrl);
      try
      {
        using (cancellationToken.Register(() => request.Abort(), false))
        using (var response = await request.GetResponseAsync())
        {
          // Get the stream containing content returned by the server
          // Open the stream using a StreamReader for easy access
          using (var dataStream = response.GetResponseStream())
          {
            var xmlSerializer = new XmlSerializer(typeof(TModel));
            return xmlSerializer.Deserialize(dataStream) as TModel;
          }
        }
      }
      catch (WebException ex)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }

        throw;
      }
    }
  }
}