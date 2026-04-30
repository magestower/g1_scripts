#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class CelShaderInspectorGlass : ShaderGUIBase
    {
        // Foldout state keys (these persist per-editor session)
        private const string GlassSettingsKey = "CelShaderInspectorGlass_ShowGlassSettings";
        private const string TextureSettingsKey = "CelShaderInspectorGlass_ShowTextureSettings";
        private const string NormalsSettingsKey = "CelShaderInspectorGlass_ShowNormalsSettings";
        private const string FresnelSettingsKey = "CelShaderInspectorGlass_ShowFresnelSettings";

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowLogo();
            // You can optionally cast the target material if needed:
            Material material = materialEditor.target as Material;

            // Draw a header/logo if desired
            DrawHeader();

            // --- Glass Settings Section ---
            bool showGlassSettings = EditorPrefs.GetBool(GlassSettingsKey, true);
            showGlassSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGlassSettings, "Glass Settings");
            if (showGlassSettings)
            {
                MaterialProperty colorProp = FindProperty("_Color", properties);
                MaterialProperty transparencyProp = FindProperty("_Transparency", properties);
                MaterialProperty refractionProp = FindProperty("_Refraction", properties);

                materialEditor.ShaderProperty(colorProp, colorProp.displayName);
                materialEditor.ShaderProperty(transparencyProp, transparencyProp.displayName);
                materialEditor.ShaderProperty(refractionProp, refractionProp.displayName);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool(GlassSettingsKey, showGlassSettings);

            // --- Texture Settings Section ---
            bool showTextureSettings = EditorPrefs.GetBool(TextureSettingsKey, true);
            showTextureSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showTextureSettings, "Texture Settings");
            if (showTextureSettings)
            {
                MaterialProperty diffuseProp = FindProperty("_Diffuse", properties);
                MaterialProperty scaleProp = FindProperty("_Scale", properties);
                MaterialProperty cloudinessProp = FindProperty("_Cloudiness", properties);

                materialEditor.ShaderProperty(diffuseProp, diffuseProp.displayName);
                materialEditor.ShaderProperty(scaleProp, scaleProp.displayName);
                materialEditor.ShaderProperty(cloudinessProp, cloudinessProp.displayName);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool(TextureSettingsKey, showTextureSettings);

            // --- Normals Section ---
            bool showNormalsSettings = EditorPrefs.GetBool(NormalsSettingsKey, true);
            showNormalsSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showNormalsSettings, "Normals");
            if (showNormalsSettings)
            {
                MaterialProperty normalMapProp = FindProperty("_NormalMap", properties);
                MaterialProperty normalStrengthProp = FindProperty("_NormalStrength", properties);

                materialEditor.ShaderProperty(normalMapProp, normalMapProp.displayName);
                materialEditor.ShaderProperty(normalStrengthProp, normalStrengthProp.displayName);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool(NormalsSettingsKey, showNormalsSettings);

            // --- Fresnel Settings Section ---
            bool showFresnelSettings = EditorPrefs.GetBool(FresnelSettingsKey, true);
            showFresnelSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showFresnelSettings, "Fresnel");
            if (showFresnelSettings)
            {
                MaterialProperty fresnelStrengthProp = FindProperty("_FresnelStrength", properties);
                materialEditor.ShaderProperty(fresnelStrengthProp, fresnelStrengthProp.displayName);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool(FresnelSettingsKey, showFresnelSettings);
        }

        private void DrawHeader()
        {
            // Optionally draw a header or logo.
            // For example:
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Glass Shader Settings", headerStyle);
            EditorGUILayout.Space();
        }
    }
}
#endif
