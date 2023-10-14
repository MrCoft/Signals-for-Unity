using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CliWrap;

Console.WriteLine("Publishing NuGet package...");
Console.WriteLine(Assembly.GetEntryAssembly()!.Location);

var solutionDir = Path.GetFullPath(Path.Combine(
    Assembly.GetEntryAssembly()!.Location,
    "..", "..", "..", "..", ".."));

var packageJson = File.ReadAllText(Path.Join(solutionDir, "../Signals Unity project/Assets/Signals/package.json"));
var version = Regex.Match(packageJson, "\"version\": \"(.+?)\"").Groups[1].Value;
Console.WriteLine("Version: " + version);

var repoRoot = Path.Join(solutionDir, "..");
Directory.SetCurrentDirectory(repoRoot);

var envContent = File.ReadAllText(".env");
var envLine = Regex.Split(envContent, "\r\n|\r|\n").FirstOrDefault(line => line.StartsWith("NUGET_API_KEY"));
var nugetApiKey = envLine.Split("=")[1].Trim();
Console.WriteLine($"NuGet API Key loaded: {nugetApiKey.Substring(0, 4)}...{nugetApiKey.Substring(nugetApiKey.Length - 4)}");

Directory.SetCurrentDirectory("Build scripts solution/NugetProject");
var csproj = File.ReadAllText("Coft.Signals.csproj");
csproj = Regex.Replace(csproj, "<Version>(.+?)</Version>", $"<Version>{version}</Version>");
Console.WriteLine("Updated version in Coft.Signals.csproj");
File.WriteAllText("Coft.Signals.csproj", csproj);

Console.WriteLine("Running dotnet build...");
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();
await (Cli.Wrap("dotnet").WithArguments(new[]
{
    "build",
    "--configuration",
    "Release",
}) | (stdOut, stdErr)).ExecuteAsync();

Directory.SetCurrentDirectory("bin/release");
await (Cli.Wrap("dotnet").WithArguments(new[]
{
    "nuget",
    "push",
    $"Coft.Signals.{version}.nupkg",
    "--api-key",
    nugetApiKey,
    "--source",
    "https://api.nuget.org/v3/index.json",
    "--skip-duplicate",
}) | (stdOut, stdErr)).ExecuteAsync();
// NOTE: this also pushes .snupkg
