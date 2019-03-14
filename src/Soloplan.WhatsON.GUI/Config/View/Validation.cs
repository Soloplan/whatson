// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Validator.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.Collections.Generic;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Data;
  using System.Windows.Media;

  /// <summary>
  /// The validation helper.
  /// </summary>
  public static class Validation
  {
    /// <summary>
    /// The properties reflection cache.
    /// </summary>
    private static readonly Dictionary<Type, List<DependencyProperty>> propertiesReflectionCache = new Dictionary<Type, List<DependencyProperty>>();

    /// <summary>
    /// Checks all the validation rules associated with objects, forces the binding to execute all their validation rules.
    /// </summary>
    /// <param name="parent">The parent <see cref="DependencyObject"/>.</param>
    /// <returns>True if valid.</returns>
    public static bool IsValid(DependencyObject parent)
    {
      // parent properties validation
      var valid = true;

      // get the list of all the dependency properties, we can use a level of caching to avoid to use reflection
      // more than one time for each object
      foreach (var dp in GetDepenedencyProperties(parent.GetType()))
      {
        if (!BindingOperations.IsDataBound(parent, dp))
        {
          continue;
        }

        var binding = BindingOperations.GetBinding(parent, dp);
        if ((binding == null) || (binding.ValidationRules.Count <= 0))
        {
          continue;
        }

        var expression = BindingOperations.GetBindingExpression(parent, dp);
        if (expression == null)
        {
          continue;
        }

        switch (binding.Mode)
        {
          case BindingMode.OneTime:
          case BindingMode.OneWay:
            expression.UpdateTarget();
            break;
          default:
            expression.UpdateSource();
            break;
        }

        if (expression.HasError)
        {
          valid = false;
        }
      }

      // Children properties validation
      for (var i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (!IsValid(child))
        {
          valid = false;
        }
      }

      return valid;
    }

    /// <summary>
    /// Gets the <see cref="DependencyProperty"/>s.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The list of <see cref="DependencyProperty"/>s</returns>
    private static IEnumerable<DependencyProperty> GetDepenedencyProperties(Type type)
    {
      if (propertiesReflectionCache.ContainsKey(type))
      {
        return propertiesReflectionCache[type];
      }

      var properties = type.GetFields(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.FlattenHierarchy);

      // we cycle and store only the dependency properties
      var dps = new List<DependencyProperty>();
      foreach (var field in properties)
      {
        if (field.FieldType == typeof(DependencyProperty))
        {
          dps.Add((DependencyProperty)field.GetValue(null));
        }
      }

      propertiesReflectionCache.Add(type, dps);
      return dps;
    }
  }
}