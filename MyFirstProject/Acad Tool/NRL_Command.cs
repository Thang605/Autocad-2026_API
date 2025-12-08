using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(Civil3DCsharp.NRL_Command))]

namespace Civil3DCsharp
{
    public class NRL_Command
    {
        // NRL command to reload this project itself
        [CommandMethod("NRL")]
        public static void NRL()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 1. Determine Project Path
                // Since this command is now inside the project we want to reload, we need to find the project root dynamically.
                // We assume the typical structure where the DLL is in bin\Debug\
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string projectDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyPath), "..", ".."));

                if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
                {
                    ed.WriteMessage($"\nCould not find project directory based on assembly location. Expected at: {projectDir}. *Cancel*");
                    return;
                }

                string csprojFile = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault();
                if (string.IsNullOrEmpty(csprojFile))
                {
                    ed.WriteMessage("\nCould not find .csproj file. Please ensure you are in the project root. *Cancel*");
                    return;
                }

                string projectName = Path.GetFileNameWithoutExtension(csprojFile);

                // Generate unique assembly name to avoid "Assembly already loaded" error
                string randomName = Path.GetRandomFileName().Replace(".", "");
                string uniqueAssemblyName = $"{projectName}_{randomName}";

                ed.WriteMessage($"\nReloading project: {projectName} (New Assembly: {uniqueAssemblyName})...");

                // 2. Run dotnet build with unique AssemblyName
                string configuration = "Debug";

                // We need to verify where 'dotnet' is executable or just run it if it's in PATH
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build -c {configuration} /p:AssemblyName={uniqueAssemblyName}",
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    StringBuilder outputBuilder = new StringBuilder();
                    StringBuilder errorBuilder = new StringBuilder();

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) outputBuilder.AppendLine(args.Data);
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) errorBuilder.AppendLine(args.Data);
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    string output = outputBuilder.ToString();
                    string error = errorBuilder.ToString();

                    if (process.ExitCode != 0)
                    {
                        ed.WriteMessage($"\nBuild Failed:\n{output}\n{error}");
                        return;
                    }
                }

                // 3. Locate Output DLL
                // The output will be in bin\Debug\ (or whatever configuration is set)
                // Note: The /p:OutputPath argument wasn't used, so it defaults to bin/Debug/net8.0-windows/ likely
                // We need to look carefully where it landed.

                // Let's try to find it in the standard output path
                // Depending on the project file, it might just be directly in bin\Debug or bin\Debug\netX.X-windows
                // We'll search recursively in bin\Debug
                string binBaseDir = Path.Combine(projectDir, "bin", configuration);

                var dllFiles = Directory.GetFiles(binBaseDir, $"{uniqueAssemblyName}.dll", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                if (dllFiles.Count == 0)
                {
                    ed.WriteMessage($"\nCould not find built DLL ({uniqueAssemblyName}.dll) in {binBaseDir}. *Cancel*");
                    return;
                }

                FileInfo sourceDll = dllFiles.First();

                // 4. Copy to NetReload directory (inside the target project's bin) to separate it
                string netReloadDir = Path.Combine(projectDir, "bin", "NetReload");
                if (!Directory.Exists(netReloadDir))
                {
                    Directory.CreateDirectory(netReloadDir);
                }

                string destDllPath = Path.Combine(netReloadDir, sourceDll.Name);
                string destPdbPath = Path.ChangeExtension(destDllPath, "pdb");

                File.Copy(sourceDll.FullName, destDllPath, true);

                string sourcePdb = Path.ChangeExtension(sourceDll.FullName, "pdb");
                if (File.Exists(sourcePdb))
                {
                    File.Copy(sourcePdb, destPdbPath, true);
                }

                // 5. Load the new DLL
                Assembly.LoadFrom(destDllPath);
                ed.WriteMessage($"\nNETRELOAD complete. Loaded: {uniqueAssemblyName}.dll");

                // 6. Clean up the original build artifacts to avoid cluttering the bin folder
                // (Optional, but good practice if the build system leaves them there)
                try
                {
                    File.Delete(sourceDll.FullName);
                    if (File.Exists(sourcePdb)) File.Delete(sourcePdb);
                }
                catch { /* Ignore */ }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }
    }
}
