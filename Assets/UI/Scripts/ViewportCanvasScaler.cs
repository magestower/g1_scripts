using UnityEngine;
using UnityEngine.UI;



namespace G1
{
    /// <summary>
    /// CameraAspectFitter의 OnViewportChanged 이벤트를 구독하여
    /// 실제 Camera Viewport(게임 영역) 크기에 맞게 CanvasScaler의 scaleFactor를 동적으로 조정합니다.
    /// 해상도가 바뀌어 viewport가 변경되더라도 UI가 항상 게임 영역 안에 적절한 크기로 표시됩니다.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ViewportCanvasScaler : MonoBehaviour
    {
        [Header("기준 해상도 (디자인 기준)")]
        [SerializeField] private float referenceWidth  = 1080f;
        [SerializeField] private float referenceHeight = 1920f;

        [Header("스케일 기준 축")]
        [Tooltip("Width: 가로 기준 / Height: 세로 기준 / MinFit: 작은 쪽 기준 (잘림 없음, 권장)")]
        [SerializeField] private ScaleAxis scaleAxis = ScaleAxis.MinFit;

        private CanvasScaler canvasScaler;

        /// <summary>
        /// 스케일 기준 축 옵션
        /// </summary>
        private enum ScaleAxis
        {
            Width,
            Height,
            MinFit
        }

        /// <summary>
        /// 컴포넌트 초기화 — ConstantPixelSize 모드로 전환하여 scaleFactor 직접 제어
        /// </summary>
        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        /// <summary>
        /// 활성화 시 이벤트 구독 및 즉시 적용
        /// </summary>
        private void OnEnable()
        {
            CameraAspectFitter.OnViewportChanged += OnViewportChanged;

            if (Camera.main != null)
                OnViewportChanged(Camera.main.rect);
        }

        /// <summary>
        /// 비활성화 시 이벤트 구독 해제
        /// </summary>
        private void OnDisable()
        {
            CameraAspectFitter.OnViewportChanged -= OnViewportChanged;
        }

        /// <summary>
        /// Viewport Rect 변경 시 scaleFactor를 재계산합니다.
        /// 실제 viewport 픽셀 크기 / 기준 해상도 비율로 스케일을 결정합니다.
        /// </summary>
        /// <param name="viewport">정규화된 Camera Viewport Rect (0~1)</param>
        private void OnViewportChanged(Rect viewport)
        {
            float viewportPixelW = Screen.width  * viewport.width;
            float viewportPixelH = Screen.height * viewport.height;

            float scaleW = viewportPixelW / referenceWidth;
            float scaleH = viewportPixelH / referenceHeight;

            float scale = scaleAxis switch
            {
                ScaleAxis.Width  => scaleW,
                ScaleAxis.Height => scaleH,
                _                => Mathf.Min(scaleW, scaleH)
            };

            canvasScaler.scaleFactor = scale;

            Debug.Log($"[ViewportCanvasScaler] scaleFactor: {scale:F4} " +
                      $"| viewport: {viewportPixelW:F0}x{viewportPixelH:F0} " +
                      $"| 기준: {referenceWidth}x{referenceHeight}");
        }
    }
}
