// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlJobs.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System.Xml.Serialization;

  [System.SerializableAttribute]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [XmlType(AnonymousType = true)]
  [XmlRoot(ElementName= "Projects", Namespace = "", IsNullable = false)]
  public class CruiseControlJobs
  {
    [XmlElement("Project")]
    public CruiseControlJob[] CruiseControlProject { get; set; }
  }
}