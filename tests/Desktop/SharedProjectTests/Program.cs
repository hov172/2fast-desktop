// Project2FA.Shared is a shared project (.shproj): every head — the Uno desktop
// head and the legacy UWP head — compiles the file list in Project2FA.Shared.projitems,
// not the directory. A .cs file that exists on disk but is missing from the manifest
// compiles in NO head and fails silently; an entry whose case does not match the file
// works on Windows and macOS but breaks on any case-sensitive filesystem.
//
// The UWP head cannot be built on a machine without the UAP SDK, so this check exists
// to catch the manifest class of breakage there. It is not a substitute for building it.
using System.Text.RegularExpressions;

string root = AppContext.BaseDirectory;
while (!Directory.Exists(Path.Combine(root, "Project2FA.Shared")))
{
    var parent = Directory.GetParent(root) ?? throw new Exception("FAIL: repository root not found");
    root = parent.FullName;
}
string shared = Path.Combine(root, "Project2FA.Shared");
string projitems = Path.Combine(shared, "Project2FA.Shared.projitems");
if (!File.Exists(projitems)) throw new Exception("FAIL: Project2FA.Shared.projitems is missing");

var declared = Regex.Matches(File.ReadAllText(projitems), @"<Compile Include=""\$\(MSBuildThisFileDirectory\)([^""]+)""")
    .Select(match => match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar))
    .ToList();

var onDisk = Directory.EnumerateFiles(shared, "*.cs", SearchOption.AllDirectories)
    .Select(path => Path.GetRelativePath(shared, path))
    .Where(path => !path.StartsWith("bin" + Path.DirectorySeparatorChar) && !path.StartsWith("obj" + Path.DirectorySeparatorChar))
    .ToList();

var problems = new List<string>();

// Ordinal comparison on purpose: a case-only mismatch is the bug being looked for.
var declaredSet = new HashSet<string>(declared, StringComparer.Ordinal);
foreach (string file in onDisk.Where(file => !declaredSet.Contains(file)))
{
    string? nearMiss = declared.FirstOrDefault(entry => string.Equals(entry, file, StringComparison.OrdinalIgnoreCase));
    problems.Add(nearMiss is null
        ? $"on disk but not in the manifest, so it compiles in no head: {file}"
        : $"case mismatch — manifest says '{nearMiss}', disk has '{file}'; breaks on a case-sensitive filesystem");
}

var diskSet = new HashSet<string>(onDisk, StringComparer.Ordinal);
foreach (string entry in declared.Where(entry => !diskSet.Contains(entry)))
{
    if (onDisk.Any(file => string.Equals(file, entry, StringComparison.OrdinalIgnoreCase))) continue; // already reported above
    problems.Add($"in the manifest but missing on disk, so every head fails to build: {entry}");
}

foreach (var duplicate in declared.GroupBy(entry => entry, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
    problems.Add($"listed more than once in the manifest: {duplicate.Key}");

if (problems.Count > 0)
{
    foreach (string problem in problems) Console.Error.WriteLine("FAIL: " + problem);
    throw new Exception($"Project2FA.Shared.projitems is out of sync with the directory ({problems.Count} problem(s)).");
}

Console.WriteLine($"Shared project manifest: {onDisk.Count} source files, all declared exactly once with matching case.");
