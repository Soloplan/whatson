// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecretConfigControlTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.IO;
  using System.Threading;
  using System.Windows.Controls;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class SecretConfigControlTests
  {
    [Test]
    public void PasswordEditorKeepsOnlyCiphertextInTheConfiguration()
    {
      var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "token-editor-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(directory);
      try
      {
        var secrets = new SecretProtector(Path.Combine(directory, "key"));
        var item = new ConfigurationItem("token") { Value = secrets.Encrypt("previous-token") };
        var original = item.Value;
        var builder = new SecretConfigControlBuilder(secrets);
        var attribute = new ConfigurationItemAttribute("token", typeof(SecretProtector));
        var password = (PasswordBox)builder.GetControl(item, attribute);
        Assert.That(password.Password, Is.EqualTo("previous-token"));
        Assert.That(item.Value, Is.EqualTo(original), "Opening settings must not rewrite secrets.");
        password.Password = "replacement-token";
        Assert.That(item.Value, Does.Not.Contain("replacement-token"));
        Assert.That(secrets.Decrypt(item.Value), Is.EqualTo("replacement-token"));
        password.Clear();
        Assert.That(item.Value, Is.Empty);
      }
      finally
      {
        Directory.Delete(directory, true);
      }
    }

    [Test]
    public void UnreadableStoredTokenIsPreservedUntilTheUserEditsIt()
    {
      var item = new ConfigurationItem("token") { Value = "invalid-encrypted-token" };
      var password = (PasswordBox)new SecretConfigControlBuilder().GetControl(item, new ConfigurationItemAttribute("token", typeof(SecretProtector)));
      Assert.That(password.Password, Is.Empty);
      Assert.That(password.ToolTip, Is.Not.Null);
      Assert.That(item.Value, Is.EqualTo("invalid-encrypted-token"));
    }
  }
}
