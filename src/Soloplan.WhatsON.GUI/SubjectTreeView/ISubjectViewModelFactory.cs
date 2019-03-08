// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ISubjectViewModelFactory.cs" company="Soloplan GmbH">
// //   Copyright (c) Soloplan GmbH. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;

  public interface ISubjectViewModelFactory
  {
    Type SubjectType { get; }
    SubjectViewModel CreateViewModel { get; }
  }
}