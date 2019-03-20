// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Subject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Collections.Generic;
  using System.Text;

  /// <summary>
  /// The subject - represent an executable job defined by the plugin.
  /// </summary>
  [ConfigurationItem(Category, typeof(string))]
  public abstract class Subject
  {
    /// <summary>
    /// The category tag.
    /// </summary>
    public const string Category = "Category";

    private const int MaxSnapshotsDefault = 5;

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

    public void QueryStatus(params string[] args)
    {
      this.ExecuteQuery(args);

      if (this.CurrentStatus != null && this.ShouldTakeSnapshot(this.CurrentStatus))
      {
        this.AddSnapshot(this.CurrentStatus);
      }
    }

    public void AddSnapshot(Status status)
    {
      while (this.Snapshots.Count >= this.MaxSnapshots)
      {
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

    protected abstract void ExecuteQuery(params string[] args);

    protected virtual bool ShouldTakeSnapshot(Status status)
    {
      return false;
    }
  }
}