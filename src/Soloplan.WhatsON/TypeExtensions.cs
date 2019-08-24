// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;

  /// <summary>
  /// The extensions for a <see cref="Type"/>.
  /// </summary>
  public static class TypeExtensions
  {
    /// <summary>
    /// Gets all the parent types.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The enumerable with parent types.</returns>
    public static IEnumerable<Type> GetParentTypes(this Type type)
    {
      if (type == null)
      {
        yield break;
      }

      foreach (var interface_ in type.GetInterfaces())
      {
        yield return interface_;
      }

      var currentBaseType = type.BaseType;
      while (currentBaseType != null)
      {
        yield return currentBaseType;
        currentBaseType = currentBaseType.BaseType;
      }
    }
  }
}