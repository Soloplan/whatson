namespace Soloplan.WhatsON
{
  using System;

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