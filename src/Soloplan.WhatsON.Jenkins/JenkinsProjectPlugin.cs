// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using NLog;

  public class JenkinsProjectPlugin : SubjectPlugin
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(SubjectConfiguration configuration)
    {
      log.Debug("Creating new JenkinsProject based on configuration {configuration}", new { Name = configuration.Name, Identifier = configuration.Identifier });
      var jenkinsProject = new JenkinsProject(configuration, new JenkinsApi());
      return jenkinsProject;
    }
  }
}
