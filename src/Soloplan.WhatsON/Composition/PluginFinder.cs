namespace Soloplan.WhatsON.Composition
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Reflection;

  public static class PluginFinder
  {
    /// <summary>
    /// Finds all the <see cref="T:Soloplan.WhatsON.ISubjectPlugin"/>s that are provided by the specified assemblies.
    /// Note that this method only supports assemblies from the applications root directory,
    /// i.e. the plugin assembly must be located next to the application's executable
    /// </summary>
    /// <param name="assemblies">A list of assemblies that contain plugins.</param>
    /// <returns>An enumerator with all the found plugins.</returns>
    public static IEnumerable<IPlugIn> FindAllPlugins(params string[] assemblies)
    {
      foreach (var assemblyName in assemblies)
      {
        var absoluteName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
        if (!File.Exists(absoluteName))
        {
          continue;
        }

        var assembly = Assembly.LoadFile(absoluteName);
        foreach (var type in assembly.GetExportedTypes())
        {
          if (typeof(IPlugIn).IsAssignableFrom(type))
          {
            yield return Activator.CreateInstance(type) as IPlugIn;
          }
        }
      }
    }
  }
}
