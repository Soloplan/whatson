// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Localizer.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.Globalization;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  public static class Localizer
  {
    public static void ApplyLanguage(ApplicationLanguage language)
    {
      var culture = language switch
      {
        ApplicationLanguage.System when CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "en" or "de" => CultureInfo.CurrentUICulture,
        ApplicationLanguage.German => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("en-US"),
      };
      CultureInfo.DefaultThreadCurrentCulture = culture;
      CultureInfo.DefaultThreadCurrentUICulture = culture;
      CultureInfo.CurrentCulture = culture;
      CultureInfo.CurrentUICulture = culture;
    }

    public static string DeleteGroup(string groupName) => string.Format(CultureInfo.CurrentCulture, Strings.DeleteGroupFormat, groupName);

    public static string DeleteNamedProject(string projectName) => string.Format(CultureInfo.CurrentCulture, Strings.DeleteNamedProjectFormat, projectName);

    public static string GetStateName(ObservationState state)
    {
      return state switch
      {
        ObservationState.Success => Strings.Success,
        ObservationState.Failure => Strings.Failure,
        ObservationState.Unstable => Strings.Unstable,
        ObservationState.Running => Strings.Running,
        _ => Strings.Unknown,
      };
    }

    public static string ImportFailed(string filePath, string errorMessage) => string.Format(CultureInfo.CurrentCulture, Strings.ImportFailedFormat, filePath, errorMessage);

    public static string MissingConnectorData(Guid identifier, string name, string description, string expectedConnectorType) => string.Format(CultureInfo.CurrentCulture, Strings.MissingConnectorDataFormat, identifier, name, description, expectedConnectorType);

    public static string NotificationSettings(string notifications) => string.Format(CultureInfo.CurrentCulture, Strings.NotificationSettingsFormat, notifications);

    public static string ProjectError(string errorMessage) => string.Format(CultureInfo.CurrentCulture, Strings.ProjectErrorFormat, errorMessage);

    public static string ProjectImportFailed(string filePath, string errorMessage) => string.Format(CultureInfo.CurrentCulture, Strings.ProjectImportFailedFormat, filePath, errorMessage);

    public static string ProjectNotification(string projectName) => string.Format(CultureInfo.CurrentCulture, Strings.ProjectNotificationFormat, projectName);
  }
}
