// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlTreViewPresentationPlugIn.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using System;
  using System.Xml;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;

  public class CruiseControlTreViewPresentationPlugIn : ITreeViewPresentationPlugIn
  {

    public Type SubjectType => typeof(CruiseControlProject);

    public SubjectViewModel CreateViewModel()
    {
      return new CruiseControlProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      return null;
    }
  }
}