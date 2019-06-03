namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Windows.Input;

  [Obsolete("This should be removed in favor of command binding. Not sure how though.")]
  public interface IHandleDoubleClick
  {
    void OnDoubleClick(object sender, MouseButtonEventArgs e);
  }
}