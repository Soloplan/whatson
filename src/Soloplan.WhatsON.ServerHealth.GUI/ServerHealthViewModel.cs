// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerHealthViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.ServerHealth.GUI
{
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  /// <summary>
  /// View model for ServerHealth plugin.
  /// </summary>
  public class ServerHealthViewModel : ConnectorViewModel
  {
    /// <summary>
    /// Creates status viewmodel.
    /// </summary>
    /// <returns>Status viewmodel.</returns>
    protected override StatusViewModel GetViewModelForStatus()
    {
      return new ServerHealthStatusViewModel(this);
    }
  }
}
