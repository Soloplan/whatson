// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValueEventArgs.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;

  /// <summary>
  /// The event args which are able to provide a value access.
  /// </summary>
  /// <typeparam name="T">The type of the value.</typeparam>
  /// <seealso cref="System.EventArgs" />
  public class ValueEventArgs<T> : EventArgs
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueEventArgs{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    public ValueEventArgs(T value)
    {
      this.Value = value;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public T Value { get; }
  }
}