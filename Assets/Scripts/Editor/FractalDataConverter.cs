using UnityEngine;
using UnityEditor;
using System.IO;
using LSystem;

public class FractalDataConverter : EditorWindow
{
    [MenuItem("Tools/Fractal Studio/Convert JSONs to SO")]
    public static void ConvertJsonToSO()
    {
        string folderPath = "Assets/Data/SavedFractals";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[Fractal Converter] : {folderPath}.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folderPath });
        int convertedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path.EndsWith(".json"))
            {
                TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (jsonFile != null)
                {
                    LSystemData newData = ScriptableObject.CreateInstance<LSystemData>();

                    JsonUtility.FromJsonOverwrite(jsonFile.text, newData);

                    string assetPath = path.Replace(".json", ".asset");
                    AssetDatabase.CreateAsset(newData, assetPath);
                    convertedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Fractal Converter] :{convertedCount}");
    }
}