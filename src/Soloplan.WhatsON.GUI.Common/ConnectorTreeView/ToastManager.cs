using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  /// <summary>
  /// Collects information about toasts to be notified and handles their diplay and updates.
  /// </summary>
  public class ToastManager
  {
    private string GroupDefault = "builds";
    private string TagPrefix = "SoloplanWhatsONToast:";

    private class ToastInfo
    {
      public uint tag;
      public Guid guid;
      public uint sequence;
      public string group;
    }

    private Collection<ToastInfo> toasts;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ToastManager()
    {
      toasts = new Collection<ToastInfo>();
    }

    /// <summary>
    /// Checks if tag is taken.
    /// </summary>
    /// <param name="tag">Tag</param>
    /// <returns>true if tag is taken.</returns>
    private bool IsTagTaken(uint tag)
    {
      foreach (var toastTag in this.toasts)
      {
        if (tag == toastTag.tag)
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Finds first free tag for toasts.
    /// </summary>
    /// <returns></returns>
    private uint GetFirstFreeTag()
    {
      uint tag = 0;
      while (this.IsTagTaken(tag))
      {
        tag = tag + 1;
      }

      return tag;
    }

    /// <summary>
    /// Displays given toast notification for given connector.
    /// </summary>
    /// <param name="connectorViewModel">Connector that notification concerns.</param>
    /// <param name="toast">Toast of the connector.</param>
    public void DisplayAndRegisterNewToast(ConnectorViewModel connectorViewModel, ToastNotification toast)
    {
      var toastNotifier = ToastNotificationManager.CreateToastNotifier();
      uint tag = this.GetFirstFreeTag();
      toast.Tag = this.TagPrefix + tag.ToString();
      toast.Group = this.GroupDefault;
      this.toasts.Add(new ToastInfo { tag = tag, guid = connectorViewModel.Identifier, sequence = 0, group = GroupDefault });
      toastNotifier.Show(toast);
    }

    /// <summary>
    /// Handles a notification update for the connector.
    /// </summary>
    /// <param name="connectorViewModel">Connector that has a toast needing an update.</param>
    public void UpdateConnectorToast(ConnectorViewModel connectorViewModel)
    {
      foreach (var item in this.toasts.ToList())
      {
        if (item.guid == connectorViewModel.Identifier)
        {
          var result = ToastNotificationManager.CreateToastNotifier().Update(connectorViewModel.CreateNotificationsDataUpdate(item.tag, item.sequence, item.group), this.TagPrefix + item.tag.ToString(), item.group);
          if (result == NotificationUpdateResult.NotificationNotFound)
          {
            this.toasts.Remove(item);
          }
        }
      }
    }
  }
}
