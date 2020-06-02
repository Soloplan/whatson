using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Soloplan.WhatsON.GUI
{
  public class ToastGenerator
  {
    public ToastGenerator()
    {

    }

    public void GenerateToast()
    {
      string title = "The current time is";
      string timeString = $"{DateTime.Now:HH:mm:ss}";

      string toastXmlString =
      $@"<toast><visual>
            <binding template='ToastGeneric'>
            <text>{title}</text>
            <text>{timeString}</text>
            </binding>
        </visual></toast>";

      var xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(toastXmlString);

      var toastNotification = new ToastNotification(xmlDoc);

      var toastNotifier = ToastNotificationManager.CreateToastNotifier();
      toastNotifier.Show(toastNotification);
    }
  }
}
