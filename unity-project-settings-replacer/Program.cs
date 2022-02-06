using System.Text.RegularExpressions;

const string PathArg = "path=";
const string SetsArg = "sets=";
const string UnSetsArg = "unsets=";

if (args.Length < 2) {
	Console.WriteLine("Usage: unity-project-settings-replacer path=[projectSettingsPath] sets=[defineSets] unsets=[defineUnSets]");
	Console.WriteLine();
	Console.WriteLine("Where:");
	Console.WriteLine("  projectSettingsPath is the path to the ProjectSettings.asset file (in the ProjectSettings folder of your Unity project)");
	Console.WriteLine("  defineSets is a CSV string of scripting define symbols to add to each platform");
	Console.WriteLine("  defineUnSets is a CSV string of scripting define symbols to remove from each platform");
	Console.WriteLine();
	Console.WriteLine("Note: Only one of sets or unsets is required");
	Console.WriteLine();
	return;
}

string path = null;
string setsCsv = null;
string unSetsCsv = null;

foreach (var arg in args) {
	var pathIndex = arg.IndexOf(PathArg, StringComparison.OrdinalIgnoreCase);
	var setsIndex = arg.IndexOf(SetsArg, StringComparison.OrdinalIgnoreCase);
	var unsetsIndex = arg.IndexOf(UnSetsArg, StringComparison.OrdinalIgnoreCase);
	
	if (pathIndex == 0) {
		path = arg.Substring(PathArg.Length);
	}
	else if (setsIndex == 0) {
		setsCsv = arg.Substring(SetsArg.Length);
	}
	else if (unsetsIndex == 0) {
		unSetsCsv = arg.Substring(UnSetsArg.Length);
	}
}

var error = false;
if (string.IsNullOrEmpty(path)) {
	Console.WriteLine("Err: Missing path argument");
	error = true;
}

if (string.IsNullOrEmpty(setsCsv) && string.IsNullOrEmpty(unSetsCsv)) {
	Console.WriteLine("Err: Missing sets or unsets argument. You must supply at least one of them");
	error = true;
}

if (!File.Exists(path)) {
	Console.WriteLine($"Err: Can't find file: {path}");
	error = true;
}

if (error) {
	return;
}

var sets = new HashSet<string>();
var unSets = new HashSet<string>();

if (!string.IsNullOrEmpty(setsCsv)) {
	foreach (var set in setsCsv.Split(',')) {
		sets.Add(set);
	}
}

if (!string.IsNullOrEmpty(unSetsCsv)) {
	foreach (var unSet in unSetsCsv.Split(',')) {
		unSets.Add(unSet);
	}
}

foreach (var set in sets) {
	Console.WriteLine($"Adding '{set}' define");
}

foreach (var unSet in unSets) {
	Console.WriteLine($"Removing '{unSet}' define");
}

var scriptingDefinesMatcher = new Regex(@"scriptingDefineSymbols:\n(\s+\d+:(.+|\n))+");
var platformScriptingDefineMatcher = new Regex(@"([^\S\r\n]+\d+):(.+|\n)");

var projectSettings = File.ReadAllText(path);

var matchFound = false;

projectSettings = scriptingDefinesMatcher.Replace(projectSettings, match => {
	if (matchFound) {
		Console.WriteLine("Err: Found multiple scripting define sections. Is this a valid project settings asset?");
		error = true;
		return null;
	}
	
	matchFound = true;
	
	var scriptingDefines = match.Groups[0].Value;
	scriptingDefines = platformScriptingDefineMatcher.Replace(scriptingDefines, match => {
		var platformDefinesString = match.Groups[2].Value.Replace(" ", string.Empty);
		var platformDefines = new HashSet<string>(platformDefinesString.Split(';'));
		foreach(var unset in unSets) {
			platformDefines.Remove(unset);
		}
		foreach(var set in sets) {
			platformDefines.Add(set);
		}
		platformDefinesString = string.Join(";", platformDefines);
		return $"{match.Groups[1].Value}: {platformDefinesString}";
	});

	return scriptingDefines;
});

if (error) {
	if (!matchFound) {
		Console.WriteLine("Err: Can't find scripting defines section of project settings. Is this a valid project settings asset?");
		return;
	}
	return;
}

File.WriteAllText(path, projectSettings);