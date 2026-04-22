using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace G1.Editor
{
    /// <summary>
    /// dokkaebi 프리팹의 MonsterHpBar 필드(fillImage, barRoot)를 자동 연결하는 에디터 유틸리티.
    /// </summary>
    public static class MonsterHpBarSetup
    {
        [MenuItem("G1/Setup/MonsterHpBar 필드 연결")]
        public static void SetupHpBar()
        {
            const string prefabPath = "Assets/Prefabs/Monster/dokkaebi.prefab";

            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            GameObject root = scope.prefabContentsRoot;

            Transform hpBarCanvas = root.transform.Find("HpBarCanvas");
            if (hpBarCanvas == null)
            {
                Debug.LogError("[MonsterHpBarSetup] HpBarCanvas를 찾을 수 없습니다.");
                return;
            }

            MonsterHpBar hpBar = hpBarCanvas.GetComponent<MonsterHpBar>();
            if (hpBar == null)
            {
                Debug.LogError("[MonsterHpBarSetup] MonsterHpBar 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // barRoot: HpBarCanvas 자체를 루트로 사용
            SerializedObject so = new SerializedObject(hpBar);
            SerializedProperty barRootProp = so.FindProperty("barRoot");
            barRootProp.objectReferenceValue = hpBarCanvas.gameObject;

            // fillImage: HpBarCanvas/Fill 의 Image 컴포넌트
            Transform fillTransform = hpBarCanvas.Find("Fill");
            if (fillTransform != null)
            {
                Image fillImage = fillTransform.GetComponent<Image>();
                SerializedProperty fillProp = so.FindProperty("fillImage");
                fillProp.objectReferenceValue = fillImage;

                // Fill 이미지 타입을 Filled로 설정
                if (fillImage != null)
                {
                    SerializedObject imgSo = new SerializedObject(fillImage);
                    imgSo.FindProperty("m_Type").enumValueIndex = (int)Image.Type.Filled;
                    imgSo.FindProperty("m_FillMethod").enumValueIndex = (int)Image.FillMethod.Horizontal;
                    imgSo.FindProperty("m_FillAmount").floatValue = 1f;
                    imgSo.ApplyModifiedProperties();
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log("[MonsterHpBarSetup] dokkaebi 프리팹 MonsterHpBar 필드 연결 완료.");
        }
    }
}
