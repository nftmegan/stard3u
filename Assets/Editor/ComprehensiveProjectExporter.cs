using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection; // For getting field values

public class ComprehensiveProjectExporter : EditorWindow
{
    private const string ExportFilePath = "Assets/FullProjectContext.txt";

    // Options for what to export
    private bool exportSceneHierarchy = true;
    private bool exportGameObjectDetails = true;
    private bool exportProjectSettings = true;
    private bool exportPrefabDetails = true;
    private bool exportScriptableObjectData = true;
    private bool exportLogicScripts = true; // Keep your original functionality

    private Vector2 scrollPosition;

    [MenuItem("Tools/Export Project Context to Text")]
    public static void ShowWindow()
    {
        GetWindow<ComprehensiveProjectExporter>("Project Exporter");
    }

    void OnGUI()
    {
        GUILayout.Label("Select information to export:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        exportLogicScripts = EditorGUILayout.Toggle("C# Logic Scripts (from Assets/Logic)", exportLogicScripts);
        exportSceneHierarchy = EditorGUILayout.Toggle("Active Scene Hierarchy", exportSceneHierarchy);
        exportGameObjectDetails = EditorGUILayout.Toggle("GameObject Components & Properties", exportGameObjectDetails);
        exportPrefabDetails = EditorGUILayout.Toggle("Prefab Details (from Assets/Prefabs)", exportPrefabDetails);
        exportScriptableObjectData = EditorGUILayout.Toggle("ScriptableObject Data", exportScriptableObjectData);
        exportProjectSettings = EditorGUILayout.Toggle("Project Settings (Tags, Layers, Input)", exportProjectSettings);


        EditorGUILayout.Space();

        if (GUILayout.Button("Export to Single Text File"))
        {
            ExportProjectData();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ExportProjectData()
    {
        StringBuilder sb = new StringBuilder();
        int totalThingsExported = 0; // General counter

        sb.AppendLine($"// --- Project Context Export ---");
        sb.AppendLine($"// Date: {System.DateTime.Now}");
        sb.AppendLine($"// Unity Version: {Application.unityVersion}");
        sb.AppendLine($"// Project Name: {PlayerSettings.productName}");
        sb.AppendLine($"// --- End Project Header ---");
        sb.AppendLine();

        if (exportLogicScripts)
        {
            AppendLogicScripts(sb, ref totalThingsExported);
        }

        if (exportSceneHierarchy || exportGameObjectDetails)
        {
            AppendSceneData(sb, ref totalThingsExported);
        }

        if (exportPrefabDetails)
        {
            AppendPrefabData(sb, ref totalThingsExported);
        }

        if (exportScriptableObjectData)
        {
            AppendScriptableObjectData(sb, ref totalThingsExported);
        }

        if (exportProjectSettings)
        {
            AppendProjectSettings(sb);
        }

        File.WriteAllText(ExportFilePath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"Exported project context to '{ExportFilePath}'. Processed approximately {totalThingsExported} items/scripts.");
        EditorUtility.DisplayDialog("Export Complete", $"Exported project context to '{ExportFilePath}'.\nSee console for details.", "OK");
    }

    private void AppendLogicScripts(StringBuilder sb, ref int scriptsExportedCount)
    {
        sb.AppendLine("// --- Start of C# Logic Scripts ---");
        string logicFolderPath = Path.Combine(Application.dataPath, "Logic");

        if (!Directory.Exists(logicFolderPath))
        {
            sb.AppendLine("// WARNING: The folder 'Assets/Logic' does not exist.");
            sb.AppendLine("// --- End of C# Logic Scripts ---");
            sb.AppendLine();
            return;
        }

        string[] scriptPaths = Directory.GetFiles(logicFolderPath, "*.cs", SearchOption.AllDirectories);
        if (scriptPaths.Length == 0)
        {
            sb.AppendLine("// WARNING: No C# scripts found in 'Assets/Logic'.");
            sb.AppendLine("// --- End of C# Logic Scripts ---");
            sb.AppendLine();
            return;
        }

        foreach (string absolutePath in scriptPaths)
        {
            string relativePath = "Assets" + absolutePath.Replace(Application.dataPath, "").Replace("\\", "/");
            sb.AppendLine($"// --- Start of script: {relativePath} ---");
            try
            {
                string[] scriptLines = File.ReadAllLines(absolutePath);
                foreach (string line in scriptLines)
                {
                    sb.AppendLine(line);
                }
            }
            catch (System.Exception e)
            {
                sb.AppendLine($"// ERROR Reading file {relativePath}: {e.Message}");
            }
            sb.AppendLine($"// --- End of script: {relativePath} ---");
            sb.AppendLine();
            scriptsExportedCount++;
        }
        sb.AppendLine("// --- End of C# Logic Scripts ---");
        sb.AppendLine();
    }

    private void AppendSceneData(StringBuilder sb, ref int gameObjectsProcessed)
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            sb.AppendLine("// --- Active Scene Data ---");
            sb.AppendLine("// No active scene loaded or valid.");
            sb.AppendLine("// --- End Active Scene Data ---");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"// --- Start of Active Scene Data: {activeScene.name} ({activeScene.path}) ---");
        GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
        foreach (GameObject go in rootGameObjects)
        {
            AppendGameObjectData(sb, go, 0, ref gameObjectsProcessed, "Scene");
        }
        sb.AppendLine("// --- End of Active Scene Data ---");
        sb.AppendLine();
    }

    private void AppendPrefabData(StringBuilder sb, ref int prefabsProcessed)
    {
        sb.AppendLine("// --- Start of Prefab Data ---");
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }); // Common prefab folder
        if (prefabGuids.Length == 0)
        {
             prefabGuids = AssetDatabase.FindAssets("t:Prefab"); // Search whole project if not in specific folder
        }


        if (prefabGuids.Length == 0)
        {
            sb.AppendLine("// No prefabs found in 'Assets/Prefabs' or project-wide.");
        }
        else
        {
            sb.AppendLine($"// Found {prefabGuids.Length} prefabs.");
        }


        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                sb.AppendLine($"// --- Prefab: {prefab.name} (Path: {path}) ---");
                AppendGameObjectData(sb, prefab, 0, ref prefabsProcessed, "Prefab");
                sb.AppendLine($"// --- End Prefab: {prefab.name} ---");
                sb.AppendLine();
            }
        }
        sb.AppendLine("// --- End of Prefab Data ---");
        sb.AppendLine();
    }


    private void AppendGameObjectData(StringBuilder sb, GameObject go, int indentLevel, ref int objectCount, string context)
    {
        objectCount++;
        string indent = new string(' ', indentLevel * 2);
        sb.AppendLine($"{indent}GameObject: {go.name} (Active: {go.activeSelf}, Tag: {go.tag}, Layer: {LayerMask.LayerToName(go.layer)})");

        if (exportGameObjectDetails) // Only dump components if option is selected
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    sb.AppendLine($"{indent}  Component: [Missing Script or Null Component]");
                    continue;
                }
                sb.AppendLine($"{indent}  Component: {component.GetType().FullName}");

                // Export public fields and serialized private fields for MonoBehaviour scripts
                if (component is MonoBehaviour)
                {
                    AppendMonobehaviourProperties(sb, component, indentLevel + 2);
                }
                // Could add special handling for other common components if needed
                // e.g., Transform, MeshRenderer, Light, Camera
                else if (component is Transform)
                {
                    Transform t = component as Transform;
                    sb.AppendLine($"{indent}    Position: {t.localPosition}");
                    sb.AppendLine($"{indent}    Rotation: {t.localEulerAngles}");
                    sb.AppendLine($"{indent}    Scale: {t.localScale}");
                }
            }
        }

        // Recursively process children
        foreach (Transform child in go.transform)
        {
            AppendGameObjectData(sb, child.gameObject, indentLevel + 1, ref objectCount, context);
        }
    }

    private void AppendMonobehaviourProperties(StringBuilder sb, Component component, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.GetIterator();

        // Skip "m_Script"
        if (prop.NextVisible(true))
        {
            while (prop.NextVisible(false)) // Iterate over visible properties
            {
                try
                {
                    string propValue = GetSerializedPropertyValue(prop);
                    sb.AppendLine($"{indent}    {prop.displayName}: {propValue} (Type: {prop.propertyType})");
                }
                catch (System.Exception ex)
                {
                    sb.AppendLine($"{indent}    {prop.displayName}: [Error getting value: {ex.Message}]");
                }
            }
        }
    }

    private string GetSerializedPropertyValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
            case SerializedPropertyType.Float: return prop.floatValue.ToString("F3"); // Format float
            case SerializedPropertyType.String: return $"\"{prop.stringValue}\""; // Add quotes
            case SerializedPropertyType.Color: return prop.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue != null ? $"{prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})" : "null";
            case SerializedPropertyType.LayerMask: return LayerMask.LayerToName(prop.intValue); // Needs proper conversion if used as mask
            case SerializedPropertyType.Enum: return prop.enumDisplayNames.Length > prop.enumValueIndex && prop.enumValueIndex >=0 ? prop.enumDisplayNames[prop.enumValueIndex] : prop.intValue.ToString() + " (Enum Invalid)";
            case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
            case SerializedPropertyType.Rect: return prop.rectValue.ToString();
            case SerializedPropertyType.ArraySize: return prop.arraySize.ToString();
            case SerializedPropertyType.Character: return $"'{System.Convert.ToChar(prop.intValue)}'";
            case SerializedPropertyType.AnimationCurve: return "AnimationCurve (data not shown)";
            case SerializedPropertyType.Bounds: return prop.boundsValue.ToString();
            case SerializedPropertyType.Gradient: return "Gradient (data not shown)";
            case SerializedPropertyType.Quaternion: return prop.quaternionValue.eulerAngles.ToString(); // Show as Euler
            case SerializedPropertyType.ExposedReference:
                 return prop.exposedReferenceValue != null ? $"{prop.exposedReferenceValue.name} ({prop.exposedReferenceValue.GetType().Name})" : "null (ExposedReference)";
            case SerializedPropertyType.FixedBufferSize: return prop.fixedBufferSize.ToString();
            // Add more types as needed
            default: return $"[Unhandled Type: {prop.propertyType}]";
        }
    }


    private void AppendScriptableObjectData(StringBuilder sb, ref int soProcessed)
    {
        sb.AppendLine("// --- Start of ScriptableObject Data ---");
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
        if (guids.Length == 0)
        {
            sb.AppendLine("// No ScriptableObjects found in the project.");
        }
        else
        {
             sb.AppendLine($"// Found {guids.Length} ScriptableObjects.");
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject soAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (soAsset != null)
            {
                soProcessed++;
                sb.AppendLine($"// --- ScriptableObject: {soAsset.name} (Type: {soAsset.GetType().FullName}, Path: {path}) ---");
                SerializedObject serObj = new SerializedObject(soAsset);
                SerializedProperty prop = serObj.GetIterator();
                
                // Skip "m_Script" for SOs as well
                if (prop.NextVisible(true)) 
                {
                    while (prop.NextVisible(false))
                    {
                        try
                        {
                            string propValue = GetSerializedPropertyValue(prop);
                            sb.AppendLine($"  {prop.displayName}: {propValue} (Type: {prop.propertyType})");
                        }
                        catch (System.Exception ex)
                        {
                             sb.AppendLine($"  {prop.displayName}: [Error getting value: {ex.Message}]");
                        }
                    }
                }
                sb.AppendLine($"// --- End ScriptableObject: {soAsset.name} ---");
                sb.AppendLine();
            }
        }
        sb.AppendLine("// --- End of ScriptableObject Data ---");
        sb.AppendLine();
    }


    private void AppendProjectSettings(StringBuilder sb)
    {
        sb.AppendLine("// --- Start of Project Settings ---");

        // Tags and Layers
        sb.AppendLine("// Tags:");
        foreach (string tag in UnityEditorInternal.InternalEditorUtility.tags)
        {
            sb.AppendLine($"//   - {tag}");
        }
        sb.AppendLine("// Layers:");
        for (int i = 0; i <= 31; i++) // Max 32 layers
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                sb.AppendLine($"//   - Layer {i}: {layerName}");
            }
        }
        sb.AppendLine();

        // Input Manager (Axes)
        sb.AppendLine("// Input Manager Axes:");
        try
        {
            SerializedObject inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProperty = inputManager.FindProperty("m_Axes");
            if (axesProperty != null && axesProperty.isArray)
            {
                for (int i = 0; i < axesProperty.arraySize; i++)
                {
                    SerializedProperty axis = axesProperty.GetArrayElementAtIndex(i);
                    string name = axis.FindPropertyRelative("m_Name").stringValue;
                    string descriptiveName = axis.FindPropertyRelative("descriptiveName").stringValue;
                    string negativeButton = axis.FindPropertyRelative("negativeButton").stringValue;
                    string positiveButton = axis.FindPropertyRelative("positiveButton").stringValue;
                    string altNegativeButton = axis.FindPropertyRelative("altNegativeButton").stringValue;
                    string altPositiveButton = axis.FindPropertyRelative("altPositiveButton").stringValue;
                    // Add more properties as needed (gravity, sensitivity, type, axis, joyNum etc.)
                    sb.AppendLine($"//   Axis: {name} (Desc: \"{descriptiveName}\", Neg: \"{negativeButton}\", Pos: \"{positiveButton}\", AltNeg: \"{altNegativeButton}\", AltPos: \"{altPositiveButton}\")");
                }
            }
            else
            {
                sb.AppendLine("// Could not read InputManager axes.");
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine($"// Error reading InputManager: {e.Message}");
        }
        sb.AppendLine();

        // Could add Physics settings, Time settings, etc.
        // Example: Physics Gravity
        sb.AppendLine($"// Physics Gravity: {Physics.gravity}");


        sb.AppendLine("// --- End of Project Settings ---");
        sb.AppendLine();
    }
}