// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Project.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System.Collections.Generic;
  using System.Xml.Serialization;

  public enum CcBuildStatus
  {
    Success,
    Failure,
    Exception,
    Unknown,
    Cancelled,
  }

  [System.SerializableAttribute]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [XmlType(AnonymousType = true)]
  public class CruiseControlJob
  {
    [XmlArrayItem("message", IsNullable = false)]
    public List<Message> Messages { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("category")]
    public string Category { get; set; }

    [XmlAttribute("activity")]
    public string Activity { get; set; }

    [XmlAttribute("lastBuildStatus")]
    public CcBuildStatus LastBuildStatus { get; set; }

    [XmlAttribute("lastBuildLabel")]
    public string LastBuildLabel { get; set; }

    [XmlAttribute("lastBuildTime")]
    public System.DateTime LastBuildTime { get; set; }

    [XmlAttribute("nextBuildTime")]
    public System.DateTime NextBuildTime { get; set; }

    [XmlAttribute("webUrl")]
    public string WebUrl { get; set; }

    [XmlAttribute("CurrentMessage")]
    public string CurrentMessage { get; set; }

    [XmlAttribute("BuildStage")]
    public string BuildStage { get; set; }

    [XmlAttribute("serverName")]
    public string ServerName { get; set; }

    [XmlAttribute("description")]
    public string Description { get; set; }
  }
}