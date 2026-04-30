using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace G1
{
    public class ComponentCopier : EditorWindow
    {
        private GameObject source;
        private GameObject target;

        [MenuItem("Tools/ASH VEIL/Component Copier")]
        public static void ShowWindow()
        {
            // EditorWindow를 상속해야 GetWindow 사용 가능
            GetWindow<ComponentCopier>("Component Copier");
        }

        void OnGUI()
        {
            GUILayout.Label("컴포넌트 일괄 복사", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            source = (GameObject)EditorGUILayout.ObjectField(
                "복사 원본", source, typeof(GameObject), true);
            target = (GameObject)EditorGUILayout.ObjectField(
                "붙여넣을 대상", target, typeof(GameObject), true);

            EditorGUILayout.Space();

            GUI.enabled = source != null && target != null;
            if (GUILayout.Button("컴포넌트 복사 실행", GUILayout.Height(30)))
                CopyComponents();
            GUI.enabled = true;
        }

        void CopyComponents()
        {
            Component[] components = source.GetComponents<Component>();
            int copied = 0;
            int skipped = 0;

            Undo.RegisterFullObjectHierarchyUndo(target, "Copy Components");

            foreach (Component comp in components)
            {
                if (comp is Transform) continue;

                if (target.GetComponent(comp.GetType()) != null)
                {
                    Debug.Log($"[스킵] 이미 존재: {comp.GetType().Name}");
                    skipped++;
                    continue;
                }

                ComponentUtility.CopyComponent(comp);
                ComponentUtility.PasteComponentAsNew(target);
                Debug.Log($"[복사 완료] {comp.GetType().Name}");
                copied++;
            }

            Debug.Log($"=== 완료: {copied}개 복사 / {skipped}개 스킵 ===");
            Selection.activeGameObject = target;
            EditorUtility.SetDirty(target);
        }
    }
}
