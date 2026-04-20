#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


/// <summary>
/// 선택한 Quad의 스케일을 할당된 텍스처 비율에 맞게 자동 조정하는 에디터 유틸리티입니다.
/// Tools/Quad - Fit to Texture 메뉴에서 실행합니다.
/// </summary>
public static class QuadFitToTexture
{
    [MenuItem("Tools/Quad - Fit to Texture")]
    public static void FitToTexture()
    {
        // 선택된 GameObject 확인
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Quad Fit", "Quad 오브젝트를 먼저 선택하세요.", "확인");
            return;
        }

        // MeshRenderer에서 머티리얼 텍스처 추출
        MeshRenderer meshRenderer = selected.GetComponent<MeshRenderer>();
        if (meshRenderer == null || meshRenderer.sharedMaterial == null)
        {
            EditorUtility.DisplayDialog("Quad Fit", "MeshRenderer 또는 머티리얼이 없습니다.", "확인");
            return;
        }

        // 메인 텍스처 가져오기
        Texture tex = meshRenderer.sharedMaterial.mainTexture;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Quad Fit", "머티리얼에 텍스처가 할당되어 있지 않습니다.", "확인");
            return;
        }

        // 텍스처 해상도 기반 비율 계산
        float aspect = (float)tex.width / tex.height;

        // 현재 Z 스케일 유지, X/Y만 비율에 맞게 조정
        Vector3 currentScale = selected.transform.localScale;
        float height = currentScale.y; // Y 기준으로 X 계산
        float width  = height * aspect;

        // Undo 등록 (Ctrl+Z 복구 가능)
        Undo.RecordObject(selected.transform, "Quad Fit to Texture");

        selected.transform.localScale = new Vector3(width, height, currentScale.z);

        Debug.Log($"[QuadFitToTexture] '{selected.name}' 스케일 조정 완료 " +
                  $"| 텍스처: {tex.width}×{tex.height} " +
                  $"| 비율: {aspect:F3} " +
                  $"| 최종 스케일: ({width:F3}, {height:F3}, {currentScale.z:F3})");
    }

    /// <summary>
    /// Quad가 선택되어 있고 머티리얼에 텍스처가 있을 때만 메뉴를 활성화합니다.
    /// </summary>
    [MenuItem("Tools/Quad - Fit to Texture", true)]
    public static bool FitToTextureValidate()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return false;

        MeshRenderer mr = selected.GetComponent<MeshRenderer>();
        if (mr == null || mr.sharedMaterial == null) return false;

        return mr.sharedMaterial.mainTexture != null;
    }
}
#endif
