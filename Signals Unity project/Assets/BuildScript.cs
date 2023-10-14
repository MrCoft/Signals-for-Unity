using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BuildScript : MonoBehaviour
{
    [MenuItem("Build/Publish NuGet")]
    private static void PublishNuget()
    {
        var oldCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Debug.Log("Publishing NuGet package...");

            var packageJson = File.ReadAllText(Path.Join(Application.dataPath, "Signals/package.json"));
            var version = Regex.Match(packageJson, "\"version\": \"(.+?)\"").Groups[1].Value;
            Debug.Log("Version: " + version);
        
            var repoRoot = Path.Join(Application.dataPath, "../..");
            Directory.SetCurrentDirectory(repoRoot);

            var envContent = File.ReadAllText(".env");
            var envLine = Regex.Split(envContent, "\r\n|\r|\n").FirstOrDefault(line => line.StartsWith("NUGET_API_KEY"));
            var nugetApiKey = envLine.Split("=")[1].Trim();
            Debug.Log($"NuGet API Key loaded: {nugetApiKey.Substring(0, 2)}..");
            
            Directory.SetCurrentDirectory("Signals NuGet project");
            var csproj = File.ReadAllText("Coft.Signals.csproj");
            csproj = Regex.Replace(csproj, "<Version>(.+?)</Version>", $"<Version>{version}</Version>");
            Debug.Log("Updated version in Coft.Signals.csproj");
            File.WriteAllText("Coft.Signals.csproj", csproj);
            
            Debug.Log("Running dotnet build...");
            var startInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = "build"
            };
            Process myProcess = new Process
            {
                StartInfo = startInfo
            };
            myProcess.Start();
            myProcess.WaitForExit();
            var output = myProcess.StandardOutput.ReadToEnd();
            Debug.Log(output);
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
        }
        
        // dotnet build
        // go to bin/release
        // pick Coft.Signals.<v>.nupkg and snupkg
        // dotnet nuget push Contoso.08.28.22.001.Test.1.0.0.nupkg --api-key qz2jga8pl3dvn2akksyquwcs9ygggg4exypy3bhxy6w6x6 --source https://api.nuget.org/v3/index.json
        // read API key from .env file
        // nuget SetApiKey Your-API-Key
        // dotnet nuget push .snupkg

        // string targetDir = $"{TargetDir}/{AppName} Windows/{AppName}.exe";
        // GenericBuild(Scenes, targetDir, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.StrictMode);
    }
}
// private string ExecuteProcessTerminal(string argument)
// {
//     try
//     {
//         
//  
//         return output;
//     }
//     catch (Exception e)
//     {
//         print(e);
//         return null;
//     }
// }
