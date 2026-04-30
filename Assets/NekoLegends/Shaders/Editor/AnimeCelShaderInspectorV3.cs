#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class AnimeCelShaderInspectorV3 : ShaderGUIBase
    {
        // Foldout states
        static bool showOutlines = true;
        static bool showMain = true;
        static bool showMainEmissive = false;
        static bool showSecondTex = false;
        static bool showSecondEmissive = false;
        static bool showMatcap = false;
        static bool showMatcapEmissive = false;
        static bool showFresnel = false;
        static bool showDissolve = false;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            ShowLogo();
            EditorGUILayout.Space();

            MaterialProperty Find(string n) => FindProperty(n, props, false);

            // Outlines
            showOutlines = EditorGUILayout.BeginFoldoutHeaderGroup(showOutlines, "Outlines");
            if (showOutlines)
            {
                materialEditor.ShaderProperty(Find("_use_outlines"), "Use Outlines");
                materialEditor.ShaderProperty(Find("_ASEOutlineWidth"), "Outline Width");
                materialEditor.ShaderProperty(Find("_ASEOutlineColor"), "Outline Color");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Main
            showMain = EditorGUILayout.BeginFoldoutHeaderGroup(showMain, "Main");
            if (showMain)
            {
                materialEditor.ShaderProperty(Find("_MainTex"), "Main Texture");
                materialEditor.ShaderProperty(Find("_NormalMap"), "Normal Map");
                materialEditor.ShaderProperty(Find("_Cutoff"), "Mask Alpha Clip Cutoff");
                materialEditor.ShaderProperty(Find("_Cel_Shader_Offset"), "Cel Shader Offset");
                materialEditor.ShaderProperty(Find("_Cel_Ramp_Smoothness"), "Cel Ramp Smoothness");
                materialEditor.ShaderProperty(Find("_light"), "Light Color");
                materialEditor.ShaderProperty(Find("_dark"), "Dark Color");
                materialEditor.ShaderProperty(Find("_Contrast"), "Contrast");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Main Emissive
            showMainEmissive = EditorGUILayout.BeginFoldoutHeaderGroup(showMainEmissive, "Main Emissive");
            if (showMainEmissive)
            {
                materialEditor.ShaderProperty(Find("_use_main_emissive"), "Use Main Emissive");
                materialEditor.ShaderProperty(Find("_Main_Emissive_Tex"), "Main Emissive Texture");
                materialEditor.ShaderProperty(Find("_Main_Emissve_color"), "Main Emissive Color");
                materialEditor.ShaderProperty(Find("_Main_Emissve_power"), "Main Emissive Power");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Second Texture
            showSecondTex = EditorGUILayout.BeginFoldoutHeaderGroup(showSecondTex, "Second Texture");
            if (showSecondTex)
            {
                materialEditor.ShaderProperty(Find("_use_second_tex"), "Use Second Texture");
                materialEditor.ShaderProperty(Find("_BuffTex"), "Second Texture");
                materialEditor.ShaderProperty(Find("_BuffTex_switch"), "Second Switch Mask");
                materialEditor.ShaderProperty(Find("_BuffTex_switch_edge_hardness"), "Second Switch Edge Hardness");
                materialEditor.ShaderProperty(Find("_BuffTex_switch_dissolve"), "Second Switch Dissolve");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Second Emissive
            showSecondEmissive = EditorGUILayout.BeginFoldoutHeaderGroup(showSecondEmissive, "Second Emissive");
            if (showSecondEmissive)
            {
                materialEditor.ShaderProperty(Find("_use_second_emissive"), "Use Second Emissive");
                materialEditor.ShaderProperty(Find("_Second_Emissive_Tex"), "Second Emissive Texture");
                materialEditor.ShaderProperty(Find("_Second_Emissve_color"), "Second Emissive Color");
                materialEditor.ShaderProperty(Find("_Second_Emissve_power"), "Second Emissive Power");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Matcap
            showMatcap = EditorGUILayout.BeginFoldoutHeaderGroup(showMatcap, "Matcap");
            if (showMatcap)
            {
                materialEditor.ShaderProperty(Find("_use_matcat"),          "Use Mat Cap");
                materialEditor.ShaderProperty(Find("_matcap"),              "Matcap Texture");
                materialEditor.ShaderProperty(Find("_special_buff_switch"), "Matcap Switch Mask");
                materialEditor.ShaderProperty(Find("_special_buff_switch_edge_hardness"),
                                                                "Matcap Switch Edge Hardness");
                materialEditor.ShaderProperty(Find("_special_buff_dissolve"), "Matcap Switch Dissolve");
                materialEditor.ShaderProperty(Find("_use_matcap_reflection"), "Matcap Reflection Mode");
                materialEditor.ShaderProperty(Find("_MatcapIntensity"),     "Matcap Intensity");

                // ─── Toggle instead of float field ───
                MaterialProperty objSpaceProp = Find("_MatcapObjectSpace");
                bool objSpace = Mathf.Abs(objSpaceProp.floatValue) > 0.5f;
                EditorGUI.BeginChangeCheck();
                objSpace = EditorGUILayout.Toggle("Object-Space Highlight", objSpace);
                if (EditorGUI.EndChangeCheck())
                    objSpaceProp.floatValue = objSpace ? 1f : 0f;

                materialEditor.ShaderProperty(Find("_use_matcap_animation"), "Animate Matcap Texture");
                materialEditor.ShaderProperty(Find("_matcap_animation_speed"), "Matcap Animation Speed");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();



            // Matcap Emissive
            showMatcapEmissive = EditorGUILayout.BeginFoldoutHeaderGroup(showMatcapEmissive, "Matcap Emissive");
            if (showMatcapEmissive)
            {
                materialEditor.ShaderProperty(Find("_use_matcap_emissive"), "Use Matcap Emissive");
                materialEditor.ShaderProperty(Find("_Matcap_Emissive_Tex"), "Matcap Emissive Texture");
                materialEditor.ShaderProperty(Find("_Matcap_Emissve_color"), "Matcap Emissive Color");
                materialEditor.ShaderProperty(Find("_Matcap_Emissve_power"), "Matcap Emissive Power");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Fresnel
            showFresnel = EditorGUILayout.BeginFoldoutHeaderGroup(showFresnel, "Fresnel");
            if (showFresnel)
            {
                materialEditor.ShaderProperty(Find("_use_frensel"), "Use Fresnel");
                materialEditor.ShaderProperty(Find("_frensel_range"), "Fresnel Range");
                materialEditor.ShaderProperty(Find("_frensel_hard"), "Fresnel Hardness");
                materialEditor.ShaderProperty(Find("_frensel_power"), "Fresnel Power");
                materialEditor.ShaderProperty(Find("_frensel_color"), "Fresnel Color");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Dissolve
            showDissolve = EditorGUILayout.BeginFoldoutHeaderGroup(showDissolve, "Dissolve");
            if (showDissolve)
            {
                materialEditor.ShaderProperty(Find("_use_dissolve"), "Use Dissolve");
                materialEditor.ShaderProperty(Find("_dissolve"), "Dissolve Texture");
                materialEditor.ShaderProperty(Find("_dissolve_edge_dissolve"), "Dissolve Edge Dissolve");
                materialEditor.ShaderProperty(Find("_edge_width"), "Edge Width");
                materialEditor.ShaderProperty(Find("_edge_clip"), "Edge Clip");
                materialEditor.ShaderProperty(Find("_dissolve_Emissve_color"), "Dissolve Emissive Color");
                materialEditor.ShaderProperty(Find("_dissolve_Emissve_power"), "Dissolve Emissive Power");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);
        }
    }
}
#endif