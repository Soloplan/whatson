// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageList.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System.Xml.Serialization;

  [XmlRoot(ElementName= "messages", Namespace = "", IsNullable = false)]
  public class MessageList
  {
    [XmlElement("message")]
    public Message[] Messages { get; set; }

    [XmlIgnore]
    public Message[] MessagesSafe => this.Messages ?? new Message[0];
  }
}