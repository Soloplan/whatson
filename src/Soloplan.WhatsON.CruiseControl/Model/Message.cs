// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Message.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System.Xml.Serialization;

  [System.SerializableAttribute]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [XmlType("message")]
  public class Message
  {
    [XmlAttributeAttribute("text")]
    public string Text { get; set; }

    [XmlAttributeAttribute("kind")]
    public MessageKind Kind { get; set; }
  }

  public enum MessageKind
  {
    [XmlEnum("NotDefined")]
    NotDefined = 0,

    [XmlEnum("Breakers")]
    Breakers = 1,

    [XmlEnum("Fixer")]
    Fixer = 2,

    [XmlEnum("FailingTasks")]
    FailingTasks = 3,

    [XmlEnum("BuildStatus")]
    BuildStatus = 4,

    [XmlEnum("BuildAbortedBy")]
    BuildAbortedBy = 5,
  }
}