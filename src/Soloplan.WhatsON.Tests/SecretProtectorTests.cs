// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecretProtectorTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.IO;
  using System.Security.Cryptography;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;

  [TestFixture]
  public class SecretProtectorTests
  {
    private string directory = string.Empty;

    [SetUp]
    public void SetUp()
    {
      this.directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "secrets-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(this.directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.directory, true);

    [Test]
    public void TokensUsePersistentRandomKeysAndUniqueNonces()
    {
      var keyFile = Path.Combine(this.directory, "key");
      var protector = new SecretProtector(keyFile);
      var first = protector.Encrypt("fake-api-token");
      var second = protector.Encrypt("fake-api-token");
      Assert.That(first, Does.Not.Contain("fake-api-token"));
      Assert.That(first, Is.Not.EqualTo(second));
      Assert.That(File.ReadAllBytes(keyFile), Has.Length.EqualTo(32));
      Assert.That(new SecretProtector(keyFile).Decrypt(first), Is.EqualTo("fake-api-token"));
      Assert.That(protector.Decrypt(second), Is.EqualTo("fake-api-token"));
      Assert.That(protector.Decrypt(protector.Encrypt(string.Empty)), Is.Empty);
    }

    [Test]
    public void CorruptionAndForeignKeysAreRejected()
    {
      var protector = new SecretProtector(Path.Combine(this.directory, "key"));
      var encrypted = protector.Encrypt("fake-api-token");
      var other = new SecretProtector(Path.Combine(this.directory, "other"));
      other.Encrypt("other-token");
      Assert.Throws<AuthenticationTagMismatchException>(() => other.Decrypt(encrypted));
      var payload = Convert.FromBase64String(encrypted.Split(':')[1]);
      payload[payload.Length - 1] ^= 1;
      Assert.Throws<AuthenticationTagMismatchException>(() => protector.Decrypt("aes-gcm-v1:" + Convert.ToBase64String(payload)));
      Assert.Throws<CryptographicException>(() => protector.Decrypt("plaintext"));
    }

    [Test]
    public void MissingKeyDoesNotSilentlyGenerateAReplacement()
    {
      var keyFile = Path.Combine(this.directory, "key");
      var protector = new SecretProtector(keyFile);
      var encrypted = protector.Encrypt("fake-api-token");
      File.Delete(keyFile);
      Assert.Throws<FileNotFoundException>(() => protector.Decrypt(encrypted));
      Assert.That(File.Exists(keyFile), Is.False);
    }

    [Test]
    public async Task ConcurrentFirstUseKeepsOneCompleteKey()
    {
      var keyFile = Path.Combine(this.directory, "key");
      var tokens = await Task.WhenAll(
        Task.Run(() => new SecretProtector(keyFile).Encrypt("one")),
        Task.Run(() => new SecretProtector(keyFile).Encrypt("two")));
      var protector = new SecretProtector(keyFile);
      Assert.That(protector.Decrypt(tokens[0]), Is.EqualTo("one"));
      Assert.That(protector.Decrypt(tokens[1]), Is.EqualTo("two"));
      Assert.That(Directory.GetFiles(this.directory), Has.Length.EqualTo(1));
    }
  }
}
