using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace G1
{
    public static class CreateGlobalVolume
    {
        [MenuItem("G1/포스트 프로세싱/Global Volume + Bloom 생성")]
        public static void Create()
        {
            // 기존 Volume 확인
            Volume existing = Object.FindFirstObjectByType<Volume>();
            if (existing != null)
            {
                Debug.LogWarning("[CreateGlobalVolume] 씬에 이미 Volume이 존재합니다: " + existing.gameObject.name);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Global Volume 생성
            GameObject go = new GameObject("Global Volume");
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;

            // VolumeProfile 생성 후 에셋으로 저장하고 다시 로드해서 할당
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 1.5f;
            bloom.scatter.value = 0.7f;

            AssetDatabase.CreateAsset(profile, "Assets/Settings/GlobalVolumeProfile.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // SaveAssets 이후 에셋 참조를 다시 로드해야 씬 저장 시 참조가 유지됨
            VolumeProfile savedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/GlobalVolumeProfile.asset");
            volume.profile = savedProfile;

            Undo.RegisterCreatedObjectUndo(go, "Create Global Volume");
            Selection.activeGameObject = go;
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[CreateGlobalVolume] Global Volume + Bloom 생성 완료. Inspector에서 수치를 조정하세요.");
        }
    }
}
