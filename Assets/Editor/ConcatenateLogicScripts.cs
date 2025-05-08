using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic; // System.Linq is not strictly needed for this version

public class ConcatenateLogicScriptsSingleFile : EditorWindow // Renamed class slightly for clarity
{
    // Removed: private const int MaxLinesPerFile = 2000;
    private const string ExportFilePath = "Assets/AllLogicScriptsCombined.txt"; // Define the single output file

    [MenuItem("Tools/Export Logic Scripts (Single File)")] // Updated menu item name for clarity
    public static void ExportLogicScripts()
    {
        string logicFolderPath = Path.Combine(Application.dataPath, "Logic");

        if (!Directory.Exists(logicFolderPath))
        {
            Debug.LogWarning("The folder 'Assets/Logic' does not exist.");
            return;
        }

        string[] scriptPaths = Directory.GetFiles(logicFolderPath, "*.cs", SearchOption.AllDirectories);
        if (scriptPaths.Length == 0)
        {
            Debug.LogWarning("No C# scripts found in 'Assets/Logic'.");
            return;
        }

        List<string> allLines = new List<string>(); // This list will hold all lines from all scripts
        int totalScriptsExported = 0;

        foreach (string absolutePath in scriptPaths)
        {
            string[] scriptLines = File.ReadAllLines(absolutePath);

            // Add breadcrumb to identify the script's origin
            string relativePath = "Assets" + absolutePath.Replace(Application.dataPath, "").Replace("\\", "/");
            allLines.Add($"// --- Start of script: {relativePath} ---");
            allLines.AddRange(scriptLines);
            allLines.Add($"// --- End of script: {relativePath} ---");
            allLines.Add(""); // Add a blank line for better readability between scripts
            totalScriptsExported++;
        }

        // Write all accumulated lines to the single file
        if (allLines.Count > 0)
        {
            WriteCombinedFile(allLines);
        }
        else
        {
            Debug.Log("No lines to write (this should not happen if scripts were found).");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Exported {totalScriptsExported} scripts to '{ExportFilePath}'. Total lines: {allLines.Count}.");
    }

    private static void WriteCombinedFile(List<string> lines)
    {
        // The 'index' parameter is no longer needed
        File.WriteAllLines(ExportFilePath, lines, Encoding.UTF8);
        Debug.Log($"Wrote {lines.Count} lines to {ExportFilePath}");
    }
}