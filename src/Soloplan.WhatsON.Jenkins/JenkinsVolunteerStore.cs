// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsVolunteerStore.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Security.Cryptography;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;

  internal sealed class JenkinsVolunteerStore
  {
    private readonly string directory;

    internal JenkinsVolunteerStore(string directory)
    {
      this.directory = directory;
    }

    internal static JenkinsVolunteerStore Instance { get; } = new JenkinsVolunteerStore(
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhatsOn", "volunteers"));

    internal async Task<IReadOnlyList<Claim>> ReadAsync(string jobUrl, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var claims = new List<Claim>();
      if (!Directory.Exists(this.directory))
      {
        return claims;
      }

      foreach (var file in Directory.EnumerateFiles(this.directory, JobKey(jobUrl) + "-*.json"))
      {
        var claim = JsonConvert.DeserializeObject<Claim>(await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false));
        if (claim == null || claim.JobUrl != jobUrl || claim.BuildNumber <= 0 || string.IsNullOrWhiteSpace(claim.Owner)
            || this.FileName(jobUrl, claim.BuildNumber) != file)
        {
          throw new InvalidDataException("Invalid saved Jenkins volunteer.");
        }

        claims.Add(claim);
      }

      return claims;
    }

    internal async Task SaveAsync(string jobUrl, int buildNumber, string owner, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Directory.CreateDirectory(this.directory);
      var file = this.FileName(jobUrl, buildNumber);
      var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
      try
      {
        await File.WriteAllTextAsync(temporary, JsonConvert.SerializeObject(new Claim(jobUrl, buildNumber, owner)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        File.Move(temporary, file, true);
      }
      finally
      {
        File.Delete(temporary);
      }
    }

    internal void Remove(string jobUrl, int buildNumber)
    {
      if (Directory.Exists(this.directory))
      {
        File.Delete(this.FileName(jobUrl, buildNumber));
      }
    }

    private static string JobKey(string jobUrl) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jobUrl)));

    private string FileName(string jobUrl, int buildNumber) =>
      Path.Combine(this.directory, JobKey(jobUrl) + "-" + buildNumber.ToString(CultureInfo.InvariantCulture) + ".json");

    internal sealed record Claim(string JobUrl, int BuildNumber, string Owner);
  }
}
