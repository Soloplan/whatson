//-----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Soloplan GmbH">
//     Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Soloplan.WhatsON.GUI")]
[assembly: AssemblyDescription("The library containing the GUI for the whatsON build and infrastructure monitoring tool.")]

#if UNSIGNED
[assembly: InternalsVisibleTo("Soloplan.WhatsON.GUI.Tests")]
#else
[assembly: InternalsVisibleTo("Soloplan.WhatsON.GUI.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7fac7bcdf888b6c2c50793972e808cc11524b3cb9da10419b0ede52b0a21bc2dd112e412bf62b2d50681b5b48ff62ceb203383578677d402e863e3e22493a95e46c7f26ab36314d9075b542745647e3d542abea30d454b6f5ea5c31621821ccb850579d64f34dd08937428f5e4c4887e8dd9d1777f01afee9e790924323d0b7")]
#endif

