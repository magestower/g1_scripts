using UnityEngine;



namespace G1
{
    /// <summary>
    /// Canvas (Screen Space - Overlay) 하위 SafeArea 컨테이너에 부착합니다.
    /// CameraAspectFitter의 OnViewportChanged 이벤트를 구독하여
    /// Camera Viewport Rect(게임 영역)에 맞게 앵커를 자동 조정합니다.
    /// 이 오브젝트 안에 UI를 배치하면 레터박스/필러박스를 침범하지 않습니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 활성화 시 이벤트 구독 및 현재 Camera viewport로 즉시 초기화
        /// </summary>
        private void OnEnable()
        {
            CameraAspectFitter.OnViewportChanged += ApplyViewport;

            // 현재 Main Camera의 viewport로 즉시 초기화
            if (Camera.main != null)
                ApplyViewport(Camera.main.rect);
        }

        /// <summary>
        /// 비활성화 시 이벤트 구독 해제 (메모리 누수 방지)
        /// </summary>
        private void OnDisable()
        {
            CameraAspectFitter.OnViewportChanged -= ApplyViewport;
        }

        /// <summary>
        /// Camera Viewport Rect를 Canvas 앵커 좌표로 변환하여 적용합니다.
        /// anchorMin/anchorMax를 viewport 범위로 설정하면
        /// 이 RectTransform이 정확히 게임 영역만을 커버합니다.
        /// </summary>
        /// <param name="viewport">정규화된 Camera Viewport Rect (0~1)</param>
        private void ApplyViewport(Rect viewport)
        {
            rectTransform.anchorMin = new Vector2(viewport.x, viewport.y);
            rectTransform.anchorMax = new Vector2(viewport.x + viewport.width,
                                                  viewport.y + viewport.height);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Debug.Log($"[SafeAreaFitter] '{name}' 적용 | " +
                      $"anchorMin: {rectTransform.anchorMin} | " +
                      $"anchorMax: {rectTransform.anchorMax}");
        }
    }
}
