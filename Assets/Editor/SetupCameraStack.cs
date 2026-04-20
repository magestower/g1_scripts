#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;


/// <summary>
/// Main Camera의 URP Camera Stack에 UI Camera를 등록하는 에디터 유틸리티입니다.
/// Tools/Setup Camera Stack 메뉴에서 실행합니다.
/// </summary>
public static class SetupCameraStack
{
    [MenuItem("Tools/Setup Camera Stack")]
    public static void Setup()
    {
        // Main Camera 탐색
        Camera mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (mainCam == null)
        {
            Debug.LogError("[SetupCameraStack] 'Main Camera'를 찾을 수 없습니다.");
            return;
        }

        // UI Camera 탐색
        Camera uiCam = GameObject.Find("UI Camera")?.GetComponent<Camera>();
        if (uiCam == null)
        {
            Debug.LogError("[SetupCameraStack] 'UI Camera'를 찾을 수 없습니다.");
            return;
        }

        // UniversalAdditionalCameraData 가져오기
        var mainData = mainCam.GetUniversalAdditionalCameraData();
        if (mainData == null)
        {
            Debug.LogError("[SetupCameraStack] Main Camera에 UniversalAdditionalCameraData가 없습니다.");
            return;
        }

        // 중복 등록 방지
        if (mainData.cameraStack.Contains(uiCam))
        {
            Debug.Log("[SetupCameraStack] UI Camera가 이미 Stack에 등록되어 있습니다.");
            return;
        }

        // Undo 등록
        Undo.RecordObject(mainData, "Add UI Camera to Stack");

        // Stack에 UI Camera 추가
        mainData.cameraStack.Add(uiCam);

        // 씬 변경 표시
        EditorUtility.SetDirty(mainData);

        Debug.Log("[SetupCameraStack] UI Camera를 Main Camera Stack에 등록했습니다.");
    }
}
#endif
