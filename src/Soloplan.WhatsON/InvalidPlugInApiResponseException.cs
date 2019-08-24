// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidPlugInApiResponseException.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;

  /// <summary>
  /// A response exception which should be called when plugin can't get data (typically deserialize) from a request.
  /// </summary>
  /// <seealso cref="System.Exception" />
  public class InvalidPlugInApiResponseException : Exception
  {
    public InvalidPlugInApiResponseException(string message, Exception innerException = null)
      : base(message, innerException)
    {
    }
  }
}