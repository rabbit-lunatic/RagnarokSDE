using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Collections.Generic;

namespace SDE
{
    public class GRFEditorMain
    {
        private static readonly Dictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _assemblyCacheLock = new object();

        private static readonly string[] _registeredAssemblies = new string[] {
            "ErrorManager",
            "ICSharpCode.AvalonEdit",
            "GRF",
            "TokeiLibrary",
            "PaletteRecolorer",
            "Be.Windows.Forms.HexBox",
            "zlib.net",
            "Utilities",
            "cps",
            "Encryption",
            "Gif.Components",
            "ColorPicker",
            "GrfMenuHandler64",
            "GrfMenuHandler32",
            "msvcp100",
            "msvcr100",
            "ActImaging",
            "Database",
            "Lua",
            "XDMessaging",
            "ErrorManager",
            "GrfToWpfBridge",
            "System.Threading",
        };

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, arguments) => {
                AssemblyName assemblyName = new AssemblyName(arguments.Name);

                if (assemblyName.Name.EndsWith(".resources"))
                    return null;

                lock (_assemblyCacheLock)
                {
                    Assembly loadedAssembly = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .FirstOrDefault(p =>
                            !p.IsDynamic &&
                            String.Equals(p.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

                    if (loadedAssembly != null)
                        return loadedAssembly;

                    Assembly cachedAssembly;
                    if (_assemblyCache.TryGetValue(assemblyName.Name, out cachedAssembly))
                        return cachedAssembly;

                    string resourceName = "SDE.Files." + assemblyName.Name + ".dll";

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            byte[] assemblyData = new byte[stream.Length];
                            stream.Read(assemblyData, 0, assemblyData.Length);

                            Assembly assembly = Assembly.Load(assemblyData);
                            _assemblyCache[assemblyName.Name] = assembly;
                            return assembly;
                        }
                    }

                    string compressedResourceName = "SDE.Files.Compressed." + assemblyName.Name + ".dll";

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(compressedResourceName))
                    {
                        if (stream != null)
                        {
                            byte[] assemblyData = new byte[stream.Length];
                            stream.Read(assemblyData, 0, assemblyData.Length);

                            byte[] size = Decompress(assemblyData);
                            Assembly assembly = Assembly.Load(size);
                            _assemblyCache[assemblyName.Name] = assembly;
                            return assembly;
                        }
                    }

                    if (_registeredAssemblies.ToList().Contains(assemblyName.Name))
                    {
                        MessageBox.Show("Failed to load assembly : " + resourceName + "\r\n\r\nThe application will now shutdown.", "Assembly loader");
                        Process.GetCurrentProcess().Kill();
                    }

                    return null;
                }
            };


            Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

            var app = new App();
            app.StartupUri = new Uri("View\\SdeEditor.xaml", UriKind.Relative);
            //app.StartupUri = new Uri("WPF\\TestTabs.xaml", UriKind.Relative);
            app.Run();
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream memStream = new MemoryStream(data))
            using (GZipStream stream = new GZipStream(memStream, CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return memory.ToArray();
            }
        }
    }
}
