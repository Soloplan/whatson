// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.ViewModel
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using Soloplan.WhatsON.Localization;

  /// <summary>
  /// Model for editing group name.
  /// </summary>
  public partial class GroupViewModel : INotifyPropertyChanged, IDataErrorInfo
  {
    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets names of other groups.
    /// </summary>
    public IList<string> AlreadyUsedNames
    {
      get; set;
    }

    /// <summary>
    /// Gets error message.
    /// </summary>
    public string Error
    {
      get
      {
        if (this.AlreadyUsedNames.Contains(this.Name))
        {
          return Strings.GroupNameAlreadyUsed;
        }

        return string.Empty;
      }
    }

    /// <summary>
    /// Gets error for given column.
    /// </summary>
    /// <param name="columnName">The name of checked column.</param>
    /// <returns>Error text.</returns>
    public string this[string columnName]
    {
      get
      {
        if (columnName == nameof(this.Name))
        {
          return this.Error;
        }

        return string.Empty;
      }
    }
  }
}