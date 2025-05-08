// Place this script in an "Editor" folder
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text; // For StringBuilder

public class ItemSpawnerGenerator : EditorWindow {

    private MonoScript targetItemDataScript = null;
    private MonoScript targetRuntimeStateScript = null; // Must implement ICloneableRuntimeState
    private string generatedClassName = "";
    private string saveFolderPath = "Assets/Logic/ItemSpawners";

    [MenuItem("Tools/Generate Item Spawner Script")]
    public static void ShowWindow() {
        GetWindow<ItemSpawnerGenerator>("Generate Item Spawner");
    }

    void OnGUI() {
        // --- UI Layout (As before) ---
        GUILayout.Label("Generate Derived Item Spawner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select ItemData script and its corresponding ICloneableRuntimeState script.", MessageType.Info);
        EditorGUILayout.Space();
        targetItemDataScript = EditorGUILayout.ObjectField("Item Data Script", targetItemDataScript, typeof(MonoScript), false) as MonoScript;
        targetRuntimeStateScript = EditorGUILayout.ObjectField("Runtime State Script", targetRuntimeStateScript, typeof(MonoScript), false) as MonoScript;
        EditorGUILayout.Space();
        if (targetItemDataScript != null && targetRuntimeStateScript != null) {
            if (string.IsNullOrEmpty(generatedClassName)) { generatedClassName = targetRuntimeStateScript.GetClass().Name.Replace("RuntimeState", "").Replace("State", "") + "ItemSpawner"; }
            generatedClassName = EditorGUILayout.TextField("Generated Class Name", generatedClassName);
        } else { generatedClassName = ""; }
        EditorGUILayout.Space();
        GUILayout.Label("Save Location:", EditorStyles.label);
        using (new EditorGUILayout.HorizontalScope()) { /* ... Save Path Browser ... */ }
        EditorGUILayout.Space();
        bool canGenerate = targetItemDataScript != null && targetRuntimeStateScript != null && !string.IsNullOrEmpty(generatedClassName) && !string.IsNullOrEmpty(saveFolderPath);
        EditorGUI.BeginDisabledGroup(!canGenerate);
        if (GUILayout.Button("Generate Script")) { GenerateSpawnerScript(); }
        EditorGUI.EndDisabledGroup();
        // --- End UI Layout ---
    }

    private void GenerateSpawnerScript() {
        // --- Validation ---
        if (targetItemDataScript == null || targetRuntimeStateScript == null) { /* ... Error Dialog ... */ return; }
        Type itemDataType = targetItemDataScript.GetClass();
        Type runtimeStateType = targetRuntimeStateScript.GetClass();
        if (itemDataType == null || !typeof(ItemData).IsAssignableFrom(itemDataType)) { /* ... Error Dialog ... */ return; }
        if (runtimeStateType == null || !typeof(ICloneableRuntimeState).IsAssignableFrom(runtimeStateType)) { EditorUtility.DisplayDialog("Error", $"Runtime State script '{targetRuntimeStateScript.name}' MUST implement ICloneableRuntimeState.", "OK"); return; }
        if (!Directory.Exists(saveFolderPath)) { /* ... Create Dir or Error ... */ }
        string filePath = Path.Combine(saveFolderPath, generatedClassName + ".cs").Replace("\\", "/");
        if (File.Exists(filePath)) { if (!EditorUtility.DisplayDialog("Warning", $"Overwrite '{generatedClassName}.cs'?", "Overwrite", "Cancel")) { return; } }
        // --- End Validation ---

        // --- Universal Code Generation Template ---
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Auto-generated spawner for {itemDataType.Name}. Configures the initial state");
        sb.AppendLine($"/// ({runtimeStateType.Name}) directly in the Inspector via the 'initialStateTemplate' field.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class {generatedClassName} : ItemSpawner {{"); // Inherit
        sb.AppendLine();
        sb.AppendLine($"    [Header(\"--- {runtimeStateType.Name} Initial State Template ---\")]");
        sb.AppendLine($"    [Tooltip(\"Configure the desired starting state. This exact state will be cloned.\")]");
        // Generate the specific state field using the selected type name
        sb.AppendLine($"    [SerializeField] private {runtimeStateType.Name} initialStateTemplate = new {runtimeStateType.Name}();");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// OVERRIDE: Creates the InventoryItem using the base itemToSpawn data");
        sb.AppendLine($"    /// and a CLONE of the initialStateTemplate.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    protected override InventoryItem GetInitialInventoryItem() {{");
        sb.AppendLine($"        if (itemToSpawn == null) {{ Debug.LogError($\"[{{gameObject.name}} {generatedClassName}] 'Item To Spawn' not assigned!\", this); return null; }}");
        // Use ItemData base type for check, specific type for context if needed by cloning (unlikely now)
        sb.AppendLine($"        if (!(itemToSpawn is {itemDataType.Name})) {{ Debug.LogError($\"[{{gameObject.name}} {generatedClassName}] Assigned 'Item To Spawn' is not {itemDataType.Name}!\", this); return null; }}");
        sb.AppendLine();
        sb.AppendLine($"        {runtimeStateType.Name} stateClone = null;");
        sb.AppendLine($"        if (initialStateTemplate != null) {{");
        sb.AppendLine($"            if (initialStateTemplate is ICloneableRuntimeState templateCloneable) {{"); // Use interface
        sb.AppendLine($"                 stateClone = templateCloneable.Clone() as {runtimeStateType.Name};"); // Call Clone() and cast
        sb.AppendLine($"                 if (stateClone == null) {{ Debug.LogError($\"[{{gameObject.name}} {generatedClassName}] Failed clone/cast! Check {runtimeStateType.Name}.Clone().\", this); }}");
        sb.AppendLine($"            }} else {{ Debug.LogError($\"[{{gameObject.name}} {generatedClassName}] Template does not implement ICloneableRuntimeState!\", this); }}");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        // Fallback to default if template null or clone failed");
        sb.AppendLine($"        if (stateClone == null) {{");
        sb.AppendLine($"            Debug.LogWarning($\"[{{gameObject.name}} {generatedClassName}] Using default state.\", this);");
        sb.AppendLine($"            stateClone = CreateDefaultStateForItem(itemToSpawn) as {runtimeStateType.Name};"); // Use base helper
        sb.AppendLine($"             if (stateClone == null) {{ Debug.LogError($\"[{{gameObject.name}} {generatedClassName}] Failed to create default state!\", this); return null; }}");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        return new InventoryItem(itemToSpawn, stateClone);"); // Create item with the cloned state
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>EDITOR VALIDATION</summary>");
        sb.AppendLine($"    protected override void OnValidate() {{");
        sb.AppendLine($"        base.OnValidate();"); // Basic null check for itemToSpawn
        sb.AppendLine($"        if (itemToSpawn != null && !(itemToSpawn is {itemDataType.Name})) {{ Debug.LogError($\"Assigned ItemData is NOT {itemDataType.Name}!\", this); }}");
        sb.AppendLine($"         if (initialStateTemplate == null && this.enabled) {{ Debug.LogWarning($\"'Initial State Template' is null. Default state will be used.\", this); }}");
        // Add check if state type implements ICloneableRuntimeState?
        sb.AppendLine($"         else if(initialStateTemplate != null && !(initialStateTemplate is ICloneableRuntimeState)) {{ Debug.LogError($\"Assigned Initial State Template ({runtimeStateType.Name}) MUST implement ICloneableRuntimeState!\", this); }}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");
        // --- End Template ---

        // --- Write File & Refresh ---
         try { File.WriteAllText(filePath, sb.ToString()); AssetDatabase.Refresh(); EditorUtility.DisplayDialog("Success", $"Generated '{generatedClassName}.cs'", "OK"); Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath); }
         catch (Exception e) { EditorUtility.DisplayDialog("Error", $"Failed write:\n{e.Message}", "OK"); }
        // --- End Write File ---
    }
}