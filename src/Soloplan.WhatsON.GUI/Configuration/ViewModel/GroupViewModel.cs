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

  /// <summary>
  /// Model for editing group name.
  /// </summary>
  public class GroupViewModel : NotifyPropertyChanged, IDataErrorInfo
  {
    /// <summary>
    /// Backing field for <see cref="Name"/>.
    /// </summary>
    private string name;

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string Name
    {
      get => this.name;
      set
      {
        this.name = value;
        this.OnPropertyChanged();
      }
    }

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
          return "The name is already used.";
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