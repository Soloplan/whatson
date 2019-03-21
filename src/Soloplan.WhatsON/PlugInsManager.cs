// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlugInsManager.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using Soloplan.WhatsON.Composition;

  /// <summary>
  /// The Manager for Subject Plugins.
  /// </summary>
  public sealed class PluginsManager
  {
    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static volatile PluginsManager instance;

    /// <summary>
    /// The subject plugins list.
    /// </summary>
    private List<ISubjectPlugin> subjectPlugins;

    private List<IPlugIn> plugIns = new List<IPlugIn>();

    /// <summary>
    /// The subjects.
    /// </summary>
    private readonly IList<Subject> subjects = new List<Subject>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginsManager"/> class.
    /// </summary>
    public PluginsManager()
    {
      this.InitializePlugInTypes();
    }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <remarks>
    /// Getter of this property is thread-safe.
    /// </remarks>
    public static PluginsManager Instance
    {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => instance ?? (instance = new PluginsManager());
    }

    /// <summary>
    /// Gets the read only list of Subject Plugins.
    /// </summary>
    public IReadOnlyList<ISubjectPlugin> SubjectPlugins
    {
      get
      {
        if(this.subjectPlugins != null)
        {
          return this.subjectPlugins.AsReadOnly();
        }

        this.subjectPlugins = new List<ISubjectPlugin>();
        foreach (var plugIn in this.plugIns.OfType<ISubjectPlugin>())
        {
          if (plugIn.SubjectType == null)
          {
            continue;
          }

          var typeDesc = plugIn.SubjectTypeAttribute;
          if (typeDesc == null)
          {
            continue;
          }

          this.subjectPlugins.Add(plugIn);
        }

        return this.subjectPlugins.AsReadOnly();
      }
    }

    public IReadOnlyList<IPlugIn> PlugIns => this.plugIns.AsReadOnly();

    /// <summary>
    /// Gets the Plugin instance of a Subject Plugin.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>The Plugin instance.</returns>
    public ISubjectPlugin GetPlugin(Subject subject)
    {
      return this.SubjectPlugins.FirstOrDefault(sp => sp.SubjectType == subject.GetType());
    }

    /// <summary>
    /// Gets the Plugin instance of a Subject Configuration.
    /// </summary>
    /// <param name="subjectConfiguration">The subject configuration.</param>
    /// <returns>The Plugin instance.</returns>
    public ISubjectPlugin GetPlugin(SubjectConfiguration subjectConfiguration)
    {
      return this.SubjectPlugins.FirstOrDefault(sp => sp.GetType().FullName == subjectConfiguration.PluginTypeName);
    }

    /// <summary>
    /// Creates the new subject.
    /// </summary>
    /// <param name="subjectConfiguration">The subject configuration.</param>
    /// <returns>Creates new subject with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {subjectConfiguration.TypeName}</exception>
    public Subject CreateNewSubject(SubjectConfiguration subjectConfiguration)
    {
      var plugin = this.SubjectPlugins.FirstOrDefault(p => p.GetType().FullName == subjectConfiguration.PluginTypeName);
      if (plugin == null)
      {
        throw new InvalidOperationException($"Couldn't find plugin for a type: {subjectConfiguration.PluginTypeName}");
      }

      var subject = plugin.CreateNew(subjectConfiguration);
      this.subjects.Add(subject);
      return subject;
    }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <param name="subjectConfiguration">The subject configuration.</param>
    /// <returns>A new subject with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {subjectConfiguration.TypeName}</exception>
    public Subject GetSubject(SubjectConfiguration subjectConfiguration)
    {
      var subject = this.subjects.FirstOrDefault(s => s.SubjectConfiguration.Identifier == subjectConfiguration.Identifier);
      if (subject != null)
      {
        return subject;
      }

      var plugin = this.SubjectPlugins.FirstOrDefault(p => p.GetType().FullName == subjectConfiguration.PluginTypeName);
      if (plugin == null)
      {
        throw new InvalidOperationException($"Couldn't find plugin for a type: {subjectConfiguration.PluginTypeName}");
      }

      return plugin.CreateNew(subjectConfiguration);
    }

    /// <summary>
    /// Initializes the Plugin types.
    /// </summary>
    private void InitializePlugInTypes()
    {
      var path = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory);
      var plugInPath = Path.Combine(path, "PlugIns");
      if (Directory.Exists(plugInPath))
      {
        var found = PluginFinder.FindAllPlugins(Directory.EnumerateFiles(plugInPath,"*.dll").ToArray());
        this.plugIns = new List<IPlugIn>();
        foreach (var plugin in found)
        {
          this.plugIns.Add(plugin);
        }
      }
    }
  }
}