// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecretProtector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;
  using System.IO;
  using System.Security.Cryptography;
  using System.Text;

  public sealed class SecretProtector
  {
    private const string Prefix = "aes-gcm-v1:";
    private readonly string keyFile;

    public SecretProtector(string keyFile)
    {
      this.keyFile = keyFile;
    }

    public static SecretProtector Instance { get; } = new SecretProtector(
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhatsOn", "secrets.key"));

    public string Encrypt(string plaintext)
    {
      if (plaintext.Length == 0)
      {
        return string.Empty;
      }

      var key = this.ReadKey(true);
      var data = Encoding.UTF8.GetBytes(plaintext);
      try
      {
        var payload = new byte[12 + 16 + data.Length];
        RandomNumberGenerator.Fill(payload.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(payload.AsSpan(0, 12), data, payload.AsSpan(28), payload.AsSpan(12, 16));
        return Prefix + Convert.ToBase64String(payload);
      }
      finally
      {
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(data);
      }
    }

    public string Decrypt(string? ciphertext)
    {
      if (string.IsNullOrEmpty(ciphertext))
      {
        return string.Empty;
      }

      if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
      {
        throw new CryptographicException("The stored token is not encrypted in a supported format.");
      }

      var payload = Convert.FromBase64String(ciphertext.Substring(Prefix.Length));
      if (payload.Length < 28)
      {
        throw new CryptographicException("The stored token is incomplete.");
      }

      var key = this.ReadKey(false);
      var data = new byte[payload.Length - 28];
      try
      {
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), data);
        return Encoding.UTF8.GetString(data);
      }
      finally
      {
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(data);
      }
    }

    private byte[] ReadKey(bool create)
    {
      if (create && !File.Exists(this.keyFile))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(this.keyFile)) ?? throw new IOException("Missing key directory."));
        var temporary = this.keyFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
          File.WriteAllBytes(temporary, key);
          try
          {
            // Publish only a complete key; another process may have created it first.
            File.Move(temporary, this.keyFile, false);
          }
          catch (IOException) when (File.Exists(this.keyFile))
          {
          }
        }
        finally
        {
          CryptographicOperations.ZeroMemory(key);
          File.Delete(temporary);
        }
      }

      var result = File.ReadAllBytes(this.keyFile);
      if (result.Length != 32)
      {
        throw new CryptographicException("The local encryption key is invalid.");
      }

      return result;
    }
  }
}
