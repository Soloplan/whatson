// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Connector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Model
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;

  /// <summary>
  /// The connector - represent an executable job defined by the plugin.
  /// </summary>
  [ConfigurationItem(Category, typeof(string), Priority = 1000000000)]
  [ConfigurationItem(ServerAddress, typeof(string), Optional = false, Priority = 100)]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [ConfigurationItem(DirectAddress, typeof(string), Optional = true, Priority = 0)]
  public abstract class Connector
  {
    /// <summary>
    /// The redirect plugin tag.
    /// </summary>
    public const string NotificationsVisbility = "NotificationsVisbility";

    /// <summary>
    /// The category tag.
    /// </summary>
    public const string Category = "Category";

    public const string ServerAddress = "Address";

    public const string ProjectName = "ProjectName";

    public const string DirectAddress = "DirectAddress";

    /// <summary>
    /// Default value for max nubmer of snapshots.
    /// </summary>
    public const int MaxSnapshots = 5;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Initializes a new instance of the <see cref="Connector"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    protected Connector(ConnectorConfiguration configuration)
    {
      this.Snapshots = new List<Snapshot>();
      if (configuration == null)
      {
        var plugin = PluginManager.Instance.GetPlugin(this);
        configuration = new ConnectorConfiguration(plugin.Name);
      }

      this.Configuration = configuration;
    }

    /// <summary>
    /// Gets or sets the description of the connector.
    /// </summary>
    public string Description { get; set; }

    public Status CurrentStatus { get; set; }

    public List<Snapshot> Snapshots { get; set; }

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address
    {
      get => this.Configuration.GetConfigurationByKey(ServerAddress).Value;
      set => this.Configuration.GetConfigurationByKey(ServerAddress).Value = value;
    }

    public string Project
    {
      get => this.Configuration.GetConfigurationByKey(ProjectName).Value;
      set => this.Configuration.GetConfigurationByKey(ProjectName).Value = value;
    }

    public string directAddress
    {
      get => this.Configuration.GetConfigurationByKey(DirectAddress).Value;
      set => this.Configuration.GetConfigurationByKey(DirectAddress).Value = value;
    }

    /// <summary>
    /// Gets or sets the configuration of a connector.
    /// </summary>
    public ConnectorConfiguration Configuration { get; set; }

    public async Task QueryStatus(CancellationToken cancellationToken)
    {
      log.Trace("Querying status for connector {connector}.", new { this.Configuration.Name, this.CurrentStatus });
      await this.ExecuteQuery(cancellationToken);
      log.Trace("Status for connector {connector} queried.", new { this.Configuration.Name, this.CurrentStatus });

      if (this.CurrentStatus != null && this.ShouldTakeSnapshot(this.CurrentStatus))
      {
        log.Debug("Adding snapshot for connector {connector}", new { this.Configuration.Name, this.CurrentStatus });
        this.AddSnapshot(this.CurrentStatus);
      }
    }

    public void AddSnapshot(Status status)
    {
      while (this.Snapshots.Count >= MaxSnapshots)
      {
        log.Debug("Max number of snapshots exceeded. Dequeuing snapshot.", new { Name = this.Configuration.Name, CurrentStatus = this.CurrentStatus });
        this.Snapshots.RemoveAt(this.Snapshots.Count - 1); // remove the end of the list because we're sortring by age
      }

      this.Snapshots.Add(new Snapshot(status));
      this.Snapshots.Sort((x, y) => x.Age.CompareTo(y.Age));
    }

    public virtual async Task<bool> IsReachableUrl(string urlInput)
    {
      var url = urlInput;
      bool testStatus;
      WebRequest request = WebRequest.Create(url);
      request.Timeout = 50; // in ms.
      try
      {
        using (WebResponse response = await request.GetResponseAsync())
        {
          testStatus = true;
          response.Close();
        }
      }
      catch (Exception)
      {
        testStatus = false;
      }

      return testStatus;
    }

    /// <summary>
    /// Checks correctness of self server URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public virtual async Task<bool> CheckServerURL()
    {
      return false;
    }

    /// <summary>
    /// Checks correctness of self project URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public virtual async Task<bool> CheckProjectURL()
    {
      return false;
    }

    public override string ToString()
    {
      var sb = new StringBuilder(this.Configuration.Name);
      if (!string.IsNullOrWhiteSpace(this.Description))
      {
        sb.Append(" - ");
        sb.Append(this.Description);
      }

      if (this.CurrentStatus != null)
      {
        sb.Append(": ");
        sb.Append(this.CurrentStatus);
      }

      return sb.ToString();
    }

    protected virtual async Task ExecuteQuery(CancellationToken cancellationToken)
    {
      this.CurrentStatus = await this.GetCurrentStatus(cancellationToken);

      // initialize history
      if (this.Snapshots.Count == 0 && this.CurrentStatus != null && this.CurrentStatus.BuildNumber > 1)
      {
        foreach (var snapshot in await this.GetHistory(cancellationToken))
        {
          if (snapshot == null)
          {
            continue;
          }

          this.AddSnapshot(snapshot);
        }
      }

      // push current state to the history because it's done building
      if (this.ShouldTakeSnapshot(this.CurrentStatus))
      {
        this.AddSnapshot(this.CurrentStatus);
      }
    }

    protected virtual bool ShouldTakeSnapshot(Status status)
    {
      log.Trace("Checking if snapshot should be taken...");

      if (status != null && status.State != ObservationState.Running && this.Snapshots.All(x => x.Status.BuildNumber != status.BuildNumber) && status.InvalidBuild == false)
      {
        log.Debug("Snapshot should be taken. {stat}", new { CurrentStatus = status });
        return true;
      }

      return false;
    }

    protected abstract Task<Status> GetCurrentStatus(CancellationToken cancellationToken);

    protected abstract Task<List<Status>> GetHistory(CancellationToken cancellationToken);
  }
}