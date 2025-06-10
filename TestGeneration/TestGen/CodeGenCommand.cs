using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace TestGen
{
    internal sealed class CodeGenCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("3c4f6930-2bf6-430f-83eb-916f15c43473");
        private readonly AsyncPackage package;

        private CodeGenCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CodeGenCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CodeGenCommand(package, commandService);
        }

        private static bool IsServerRunning()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = client.GetAsync("http://127.0.0.1:8000/docs").Result;
                    return result.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private static System.Diagnostics.Process serverProcess;

        private void StartServer()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string scriptPath = Path.Combine(assemblyDir, "model-server", "dist", "server", "server.exe");
            scriptPath = Path.GetFullPath(scriptPath);

            var psi = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            serverProcess = System.Diagnostics.Process.Start(psi);

            const int timeoutMilliseconds = 10000;
            const int pollInterval = 500;
            int waited = 0;

            while (!IsServerRunning())
            {
                System.Threading.Thread.Sleep(pollInterval);
                waited += pollInterval;
                if (waited >= timeoutMilliseconds)
                {
                    throw new TimeoutException("Failed to start server within the timeout period.");
                }
            }
        }

        public void StopServer()
        {
            try
            {
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    serverProcess.Kill(); 
                    serverProcess.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error stopping server: " + ex.Message);
            }
        }

        private string GetSelectedText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)ServiceProvider.GetServiceAsync(typeof(DTE)).Result;
            var doc = dte?.ActiveDocument;
            if (doc != null)
            {
                var selection = (TextSelection)doc.Selection;
                return selection.Text;
            }
            return null;
        }

        private async Task<string> GenerateFromModelAsync(string input)
        {
            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    inputs = new[] { input }
                };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://127.0.0.1:8000/generate", content);
                var responseString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(responseString))
                    return doc.RootElement.GetProperty("outputs")[0].GetString();
            }
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!IsServerRunning())
            {
                StartServer();
            }

            string selectedText = GetSelectedText();

            if (string.IsNullOrWhiteSpace(selectedText))
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "No text selected.",
                    "TestGen",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            try
            {
                string result = await GenerateFromModelAsync(selectedText);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var window = new OutputWindow(result);
                window.ShowDialog();

            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error generating text: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
