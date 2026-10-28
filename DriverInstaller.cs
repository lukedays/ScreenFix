using System.Diagnostics;

namespace VncKeyBridge;

// Runs the signed Interception installer embedded in this executable.
static class DriverInstaller
{
    const string ResourceName = "install-interception.exe";

    public static int Run(string installerArgument)
    {
        string directory = Directory.CreateTempSubdirectory("VncKeyBridge").FullName;
        string installerPath = Path.Combine(directory, ResourceName);
        try
        {
            ExtractInstaller(installerPath);
            using var process = Process.Start(new ProcessStartInfo(installerPath, installerArgument) { UseShellExecute = false })!;
            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    static void ExtractInstaller(string installerPath)
    {
        using var resource = typeof(DriverInstaller).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded Interception installer is missing.");
        using var file = File.Create(installerPath);
        resource.CopyTo(file);
    }
}
