
#if UNITY_EDITOR
using UnityEditor;

namespace NekoLegends
{
    public class CelShaderInspectorOutline: ShaderGUIBase
    {
       
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowMainSection(materialEditor, properties);
            ShowAdvanceSettings(materialEditor, properties);
        }

        protected void ShowAdvanceSettings(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            MaterialProperty outlineAutoShrink = FindProperty("_OutlineAutoShrink", properties);
            MaterialProperty _OutlineShrinkFactor = FindProperty("_OutlineShrinkFactor", properties);
            MaterialProperty _OutlineInitialSize = FindProperty("_OutlineInitialSize", properties);
            MaterialProperty _OutlineGrowthShrinkFactor = FindProperty("_OutlineGrowthShrinkFactor", properties);
            MaterialProperty _OutlineGrowthShrinkSensitivity = FindProperty("_OutlineGrowthShrinkSensitivity", properties);
            MaterialProperty _OutlineMaxDistance = FindProperty("_OutlineMaxDistance", properties);

            bool showAdvanceSettings = EditorPrefs.GetBool("CelShaderInspectorOutline_showAdvanceSettings", false);
            showAdvanceSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvanceSettings, "Advance Settings");
            if (showAdvanceSettings)
            {

                if (outlineAutoShrink != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool value = EditorGUILayout.Toggle("Enable Advanced Settings", outlineAutoShrink.floatValue > 0.5f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        outlineAutoShrink.floatValue = value ? 1.0f : 0.0f;
                    }
                }

                materialEditor.ShaderProperty(_OutlineInitialSize, _OutlineInitialSize.displayName);
                materialEditor.ShaderProperty(_OutlineShrinkFactor, _OutlineShrinkFactor.displayName);
                materialEditor.ShaderProperty(_OutlineGrowthShrinkFactor, _OutlineGrowthShrinkFactor.displayName);
                materialEditor.ShaderProperty(_OutlineGrowthShrinkSensitivity, _OutlineGrowthShrinkSensitivity.displayName);
                materialEditor.ShaderProperty(_OutlineMaxDistance, _OutlineMaxDistance.displayName);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("CelShaderInspectorOutline_showAdvanceSettings", showAdvanceSettings);

        }

        protected void ShowMainSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            ShowLogo();
            MaterialProperty _OutlineColor = FindProperty("_OutlineColor", properties);
            MaterialProperty _OutlineSize = FindProperty("_OutlineSize", properties);
            MaterialProperty _OutlineMultiplier = FindProperty("_OutlineMultiplier", properties);
            MaterialProperty _OutlineGrowth = FindProperty("_OutlineGrowth", properties);


            materialEditor.ShaderProperty(_OutlineColor, _OutlineColor.displayName);
            materialEditor.ShaderProperty(_OutlineSize, _OutlineSize.displayName);
            materialEditor.ShaderProperty(_OutlineMultiplier, _OutlineMultiplier.displayName);
            materialEditor.ShaderProperty(_OutlineGrowth, _OutlineGrowth.displayName);
        

        }


    }

}

#endif
