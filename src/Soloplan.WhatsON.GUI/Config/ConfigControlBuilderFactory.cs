// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigControlBuilderFactory.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;

  public sealed class ConfigControlBuilderFactory
  {
    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static volatile ConfigControlBuilderFactory instance;

    /// <summary>
    /// Caches control builders that should be used by a given type.
    /// </summary>
    private readonly IList<KeyValuePair<Type, IConfigControlBuilder>> controlBuilderByType = new List<KeyValuePair<Type, IConfigControlBuilder>>();

    /// <summary>
    /// Indicates whether this control builder factory is already configured or not.
    /// </summary>
    private bool isConfigured;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <remarks>
    /// Getter of this property is thread-safe.
    /// </remarks>
    public static ConfigControlBuilderFactory Instance
    {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => instance ?? (instance = new ConfigControlBuilderFactory());
    }

    /// <summary>
    /// Registers the specified <paramref name="controlBuilder"/>for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type for which to use the specified controlBuilder.</param>
    /// <param name="controlBuilder">The control builder to use with <paramref name="type"/>.</param>
    public void RegisterControlBuilder(Type type, IConfigControlBuilder controlBuilder)
    {
      this.controlBuilderByType.Add(new KeyValuePair<Type, IConfigControlBuilder>(type, controlBuilder));
    }

    /// <summary>
    /// Gets the control builder for the specified <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type to get the control builder.</param>
    /// <param name="configurationItemKey">The configuration item key.</param>
    /// <returns>
    /// The control builder for this type or null if not found.
    /// </returns>
    internal IConfigControlBuilder GetControlBuilder(Type type, string configurationItemKey)
    {
      this.CheckConfiguration();
      var builder = this.controlBuilderByType.FirstOrDefault(b => b.Key == type && b.Value.SupportedConfigurationItemsKey == configurationItemKey);
      if (builder.Key == null)
      {
        builder = this.controlBuilderByType.FirstOrDefault(b => b.Key == type);
      }

      return builder.Key != null ? builder.Value : null;
    }

    /// <summary>
    /// Registers control builders for specific types.
    /// </summary>
    private void RegisterBuiltInTypeSpecificControlBuilders()
    {
      this.RegisterControlBuilder(typeof(string), new TextConfigControlBuilder());
      this.RegisterControlBuilder(typeof(int), new NumericConfigControlBuilder());
      this.RegisterControlBuilder(typeof(string), new CategoryComboBoxConfigControlBuilder());
      this.RegisterControlBuilder(typeof(bool), new CheckBoxBoxConfigControlBuilder());
    }

    /// <summary>
    /// Checks whether this control builder factory is already configured.
    /// </summary>
    private void CheckConfiguration()
    {
      if (!this.isConfigured)
      {
        this.RegisterBuiltInTypeSpecificControlBuilders();
        this.isConfigured = true;
      }
    }
  }
}