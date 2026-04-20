using UnityEngine;
using UnityEditor;

public class SkyboxSetupTool
{
    [MenuItem("Tools/Setup Skybox Night 01")]
    public static void SetupSkybox()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Skybox_Night_01.mat");
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/skybox_night_01.png");

        if (mat == null) { Debug.LogError("Material not found."); return; }
        if (tex == null) { Debug.LogError("Texture not found."); return; }

        mat.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("Skybox texture assigned successfully.");
    }
}
