// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Subject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Collections.Generic;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;

  /// <summary>
  /// The subject - represent an executable job defined by the plugin.
  /// </summary>
  [ConfigurationItem(Category, typeof(string), Priority = 2147483500)]
  public abstract class Subject
  {
    /// <summary>
    /// The category tag.
    /// </summary>
    public const string Category = "Category";

    /// <summary>
    /// Default value for max nubmer of snapshots.
    /// </summary>
    private const int MaxSnapshotsDefault = 5;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Initializes a new instance of the <see cref="Subject"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    protected Subject(SubjectConfiguration configuration)
    {
      this.Snapshots = new Queue<Snapshot>();
      this.MaxSnapshots = MaxSnapshotsDefault;
      if (configuration == null)
      {
        var plugin = PluginsManager.Instance.GetPlugin(this);
        configuration = new SubjectConfiguration(plugin.GetType().FullName);
      }

      this.SubjectConfiguration = configuration;
    }

    public string Description { get; set; }

    public Status CurrentStatus { get; set; }

    public int MaxSnapshots { get; set; }

    public Queue<Snapshot> Snapshots { get; set; }

    /// <summary>
    /// Gets or sets the configuration of a subject.
    /// </summary>
    public SubjectConfiguration SubjectConfiguration { get; set; }

    public async Task QueryStatus(CancellationToken cancellationToken, params string[] args)
    {
      log.Trace("Querying status for subject {subject}.", new { Name = this.SubjectConfiguration.Name, CurrentStatus = this.CurrentStatus });
      await this.ExecuteQuery(cancellationToken, args);
      log.Trace("Status for subject {subject} queried.", new { Name = this.SubjectConfiguration.Name, CurrentStatus = this.CurrentStatus });

      if (this.CurrentStatus != null && this.ShouldTakeSnapshot(this.CurrentStatus))
      {
        log.Debug("Adding snapshot for subject {subject}", new { Name = this.SubjectConfiguration.Name, CurrentStatus = this.CurrentStatus });
        this.AddSnapshot(this.CurrentStatus);
      }
    }

    public void AddSnapshot(Status status)
    {
      while (this.Snapshots.Count >= this.MaxSnapshots)
      {
        log.Debug("Max number of snapshots exceeded. Dequeuing snapshot.", new { Name = this.SubjectConfiguration.Name, CurrentStatus = this.CurrentStatus });
        this.Snapshots.Dequeue();
      }

      this.Snapshots.Enqueue(new Snapshot(status));
    }

    public override string ToString()
    {
      var sb = new StringBuilder(this.SubjectConfiguration.Name);
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

    protected abstract Task ExecuteQuery(CancellationToken cancellationToken, params string[] args);

    protected virtual bool ShouldTakeSnapshot(Status status)
    {
      return false;
    }
  }
}