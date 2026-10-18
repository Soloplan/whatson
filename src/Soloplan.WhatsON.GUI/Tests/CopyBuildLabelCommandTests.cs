// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopyBuildLabelCommandTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Runtime.InteropServices;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  [Apartment(ApartmentState.STA)]
  public class CopyBuildLabelCommandTests
  {
    [Test]
    public void LockedClipboardDoesNotEscapeTheCopyCommand()
    {
      using var ready = new ManualResetEventSlim();
      using var release = new ManualResetEventSlim();
      var acquired = false;
      var holder = Task.Run(() =>
      {
        acquired = SpinWait.SpinUntil(() => OpenClipboard(IntPtr.Zero), TimeSpan.FromSeconds(5));
        ready.Set();
        if (acquired)
        {
          try
          {
            release.Wait(TimeSpan.FromSeconds(10));
          }
          finally
          {
            CloseClipboard();
          }
        }
      });

      try
      {
        Assert.That(ready.Wait(TimeSpan.FromSeconds(6)), Is.True);
        Assert.That(acquired, Is.True, "The helper must hold the clipboard before copying.");
        var status = new BuildStatusViewModel(new ConnectorViewModel());
        status.Update(new Status { BuildNumber = 77 });
        Assert.That(status.Label, Is.EqualTo("#77"));

        var command = new CopyBuildLabelCommand();
        Assert.DoesNotThrow(() => command.Execute(status));
        Assert.DoesNotThrow(() => command.Execute(null));
        Assert.DoesNotThrow(() => command.Execute(new object()));
        Assert.DoesNotThrow(() => command.Execute(new BuildStatusViewModel(new ConnectorViewModel())));
      }
      finally
      {
        release.Set();
        Assert.That(holder.Wait(TimeSpan.FromSeconds(5)), Is.True);
      }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();
  }
}
