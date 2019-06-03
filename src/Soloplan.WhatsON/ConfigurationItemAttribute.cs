// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItemAttribute.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;

  /// <summary>
  /// Used for marking configuration items for connectors.
  /// </summary>
  /// <seealso cref="System.Attribute" />
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class ConfigurationItemAttribute : Attribute
  {
    /// <summary>
    /// The caption.
    /// </summary>
    private string caption;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationItemAttribute"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    public ConfigurationItemAttribute(string key, Type type)
    {
      this.Key = key;
      this.Type = type;
      this.Optional = true;
    }

    /// <summary>
    /// Gets the key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="ConfigurationItemAttribute"/> is optional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the caption.
    /// </summary>
    public string Caption
    {
      get
      {
        if (this.caption == null)
        {
          return this.Key;
        }

        return this.caption;
      }

      set
      {
        this.caption = value;
      }
    }
  }
}
