// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecretConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System;
  using System.IO;
  using System.Security.Cryptography;
  using System.Windows;
  using System.Windows.Automation;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Localization;

  public class SecretConfigControlBuilder : ConfigControlBuilder
  {
    private readonly SecretProtector secrets;

    public SecretConfigControlBuilder()
      : this(SecretProtector.Instance)
    {
    }

    internal SecretConfigControlBuilder(SecretProtector secrets)
    {
      this.secrets = secrets;
    }

    public override string? SupportedConfigurationItemsKey => null;

    public override FrameworkElement GetControlInternal(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var password = new PasswordBox { DataContext = configItem };
      HintAssist.SetHint(password, configItemAttribute.Caption);
      HintAssist.SetIsFloating(password, true);
      AutomationProperties.SetName(password, configItemAttribute.Caption);
      try
      {
        password.Password = this.secrets.Decrypt(configItem.Value);
      }
      catch (Exception exception) when (exception is CryptographicException or IOException or FormatException or UnauthorizedAccessException)
      {
        password.ToolTip = Strings.TokenCannotBeDecrypted;
        HintAssist.SetHint(password, Strings.TokenCannotBeDecrypted);
      }

      password.PasswordChanged += (_, _) =>
      {
        try
        {
          configItem.Value = this.secrets.Encrypt(password.Password);
          password.ToolTip = null;
          HintAssist.SetHint(password, configItemAttribute.Caption);
        }
        catch (Exception exception) when (exception is CryptographicException or IOException or UnauthorizedAccessException)
        {
          password.ToolTip = Strings.TokenCannotBeSaved;
          MessageBox.Show(Strings.TokenCannotBeSaved, configItemAttribute.Caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
      };
      return password;
    }
  }
}
