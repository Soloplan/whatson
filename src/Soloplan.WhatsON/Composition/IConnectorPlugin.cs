namespace Soloplan.WhatsON.Composition
{
  using System;
  using Soloplan.WhatsON.Configuration;

  public interface IConnectorPlugin : IPlugIn
  {
    Type ConnectorType { get; }

    ConnectorTypeAttribute ConnectorTypeAttribute { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin supports wizards.
    /// </summary>
    bool SupportsWizard { get; }

    Connector CreateNew(ConnectorConfiguration configuration);
  }
}