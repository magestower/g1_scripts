#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// RingExpandEffect 에디터 전용 파트.
    /// 에디터 모드에서 ContextMenu를 통한 미리보기 재생/중단을 담당한다.
    /// </summary>
    public partial class RingExpandEffect
    {
        private double editorStartTime;
        private bool   editorPlaying;

        /// <summary>에디터에서 현재 위치로 이펙트를 미리보기 재생한다. 플레이 모드면 런타임 Play()로 대체.</summary>
        [ContextMenu("테스트 재생 (에디터)")]
        private void PlayAtCurrentPositionEditor()
        {
            if (Application.isPlaying)
            {
                Play(transform.position);
                return;
            }

            InitMat();
            EditorStartPreview();
        }

        /// <summary>에디터 모드 미리보기 시작. 불변 셰이더 값 초기화 후 EditorApplication.update 구독.</summary>
        private void EditorStartPreview()
        {
            EditorStopPreview();
            ApplyStaticShaderProps();
            editorStartTime = EditorApplication.timeSinceStartup;
            editorPlaying   = true;
            EditorApplication.update += EditorUpdatePreview;
        }

        /// <summary>에디터 미리보기 매 프레임 갱신 콜백.</summary>
        private void EditorUpdatePreview()
        {
            if (!editorPlaying || this == null)
            {
                EditorStopPreview();
                return;
            }

            float t = (float)((EditorApplication.timeSinceStartup - editorStartTime) / duration);
            if (t >= 1f)
            {
                ApplyAtTime(0f);
                EditorStopPreview();
                SceneView.RepaintAll();
                return;
            }

            ApplyAtTime(t);
            SceneView.RepaintAll();
        }

        /// <summary>에디터 미리보기 구독 해제. editorPlaying 여부와 무관하게 항상 해제 (중복 -= 안전).</summary>
        private void EditorStopPreview()
        {
            EditorApplication.update -= EditorUpdatePreview;
            editorPlaying = false;
        }
    }
}
#endif
