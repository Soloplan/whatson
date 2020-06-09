using NLog.Internal;
using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Soloplan.WhatsON.Model;
using System.IO;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  public class ToastGenerator
  {
    public ToastGenerator()
    {

    }

    private ToastContent BuildEmptyToast()
    {
      var content = new Microsoft.Toolkit.Uwp.Notifications.ToastContent()
      {
        Visual = new Microsoft.Toolkit.Uwp.Notifications.ToastVisual()
        {
          BindingGeneric = new ToastBindingGeneric()
          {
            Children =
            {
              new AdaptiveText()
              {
                Text = "Empty Notification",
              },
            },
          },
        },
      };

      return content;
    }

    private string GetIconPath(StatusViewModel statusViewModel)
    {
      string result = string.Empty;
      string imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
      if (statusViewModel.State == ObservationState.Success)
      {
        result = imagesPath + "\\check-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Failure)
      {
        result = imagesPath + "\\close-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Unstable)
      {
        result = imagesPath + "\\alert-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Running)
      {
        result = imagesPath + "\\circle.png";
      }
      else if (statusViewModel.State == ObservationState.Unknown)
      {
        result = imagesPath + "\\minus-circle.png";
      }

      return result;
    }

    private ToastContent BuildToast(ConnectorViewModel connectorViewModel, ConnectorGroupViewModel connectorGroupViewModel = null)
    {
      var title = new AdaptiveText() { Text = connectorViewModel.Name };
      var container = new AdaptiveGroup();
      var subContainer = new AdaptiveSubgroup();
      var groupText = new AdaptiveText();
      var statusText = new AdaptiveText();
      var content = new Microsoft.Toolkit.Uwp.Notifications.ToastContent()
      {
        Visual = new Microsoft.Toolkit.Uwp.Notifications.ToastVisual()
        {
          BindingGeneric = new ToastBindingGeneric()
          {
            AppLogoOverride = new ToastGenericAppLogo()
            {
              Source = this.GetIconPath(connectorViewModel.CurrentStatus),
            },
          },
        },
      };
      title.Text = connectorViewModel.Name;
      if (connectorGroupViewModel != null)
      {
        if (connectorGroupViewModel.GroupName != string.Empty)
        {
          groupText.Text = "Group: " + connectorGroupViewModel.GroupName;
          groupText.HintStyle = AdaptiveTextStyle.Base;
        }
      }

      statusText.Text = "Status: " + connectorViewModel.CurrentStatus.State;
      statusText.HintStyle = AdaptiveTextStyle.Base;
      subContainer.Children.Add(statusText);
      container.Children.Add(subContainer);
      content.Visual.BindingGeneric.Children.Add(title);
      content.Visual.BindingGeneric.Children.Add(groupText);
      content.Visual.BindingGeneric.Children.Add(container);
      return content;
    }

    public ToastContent GenerateToastContent(ConnectorViewModel connectorViewModel = null, ConnectorGroupViewModel connectorGroupViewModel = null)
    {
      ToastContent content;
      if (connectorViewModel == null)
      {
        content = this.BuildEmptyToast();
      }
      else
      {
        content = this.BuildToast(connectorViewModel, connectorGroupViewModel);
      }

      return content;
    }
  }
}
