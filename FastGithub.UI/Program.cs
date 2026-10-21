using System;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace FastGithub.UI
{
    internal static class Program
    {
        internal static bool PreviewMode => Environment.GetEnvironmentVariable("FASTGITHUB_UI_PREVIEW") == "1";
        internal static bool IsClosing { get; set; }

        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            using var mutex = new Mutex(true, "Global\\FastGithub.UI", out var firstInstance);
            if (!firstInstance) return;
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.Run(new MainWindow());
        }

        private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == null || name.EndsWith(".resources")) return null;
            var resource = Application.GetResourceStream(new Uri($"Resource/{name}.dll", UriKind.Relative));
            if (resource == null) return null;
            using var stream = resource.Stream;
            var buffer = new byte[stream.Length];
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read == 0) return null;
                offset += read;
            }
            return Assembly.Load(buffer);
        }
    }
}
