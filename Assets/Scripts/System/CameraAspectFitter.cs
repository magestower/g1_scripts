using System;
using UnityEngine;



namespace G1
{
    /// <summary>
    /// 기준 해상도 비율을 유지하며 Camera Viewport Rect를 조정합니다.
    /// 화면 비율이 기준과 다를 경우 레터박스(상하) 또는 필러박스(좌우)로
    /// 빈 영역을 검정으로 채워 어색함을 방지합니다.
    /// Viewport 변경 시 OnViewportChanged 이벤트를 발행하여
    /// SafeAreaFitter 등 UI 컴포넌트가 게임 영역 안에 배치되도록 지원합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        /// <summary>
        /// Viewport Rect가 변경될 때 발행되는 이벤트입니다.
        /// 파라미터: 정규화된 Viewport Rect (0~1 범위).
        /// SafeAreaFitter 등 UI 컴포넌트가 이 이벤트를 구독해 게임 영역에 맞게 조정합니다.
        /// </summary>
        public static event Action<Rect> OnViewportChanged;

        [Header("기준 해상도")]
        [SerializeField] private float targetWidth  = 1080f;
        [SerializeField] private float targetHeight = 1920f;

        private Camera cam;
        private float  lastScreenWidth;
        private float  lastScreenHeight;

        /// <summary>
        /// 컴포넌트 초기화 및 첫 번째 비율 적용
        /// </summary>
        private void Awake()
        {
            cam = GetComponent<Camera>();
            // clearFlags는 건드리지 않음 — 스카이박스 등 인스펙터 설정 유지
        }

        /// <summary>
        /// 게임 시작 시 비율 적용
        /// </summary>
        private void Start()
        {
            ApplyAspectRatio();
            lastScreenWidth  = Screen.width;
            lastScreenHeight = Screen.height;
        }

        /// <summary>
        /// 화면 크기 변경(화면 회전 등) 감지 후 재적용
        /// </summary>
        private void Update()
        {
            // 화면 크기가 변경됐을 때만 재계산 (매 프레임 연산 방지)
            if (Mathf.Approximately(lastScreenWidth,  Screen.width) &&
                Mathf.Approximately(lastScreenHeight, Screen.height))
                return;

            ApplyAspectRatio();
            lastScreenWidth  = Screen.width;
            lastScreenHeight = Screen.height;
        }

        /// <summary>
        /// 현재 화면 비율과 기준 비율을 비교해 Camera Viewport Rect를 조정합니다.
        /// 기준보다 넓으면 필러박스(좌우 검정),
        /// 기준보다 높으면 레터박스(상하 검정)를 적용합니다.
        /// </summary>
        private void ApplyAspectRatio()
        {
            float targetAspect = targetWidth / targetHeight;
            float screenAspect = Screen.width / (float)Screen.height;

            // 비율 차이 비교 (허용 오차 0.001)
            if (Mathf.Abs(screenAspect - targetAspect) < 0.001f)
            {
                // 비율이 동일 → 전체 화면 사용
                var fullRect = new Rect(0f, 0f, 1f, 1f);
                cam.rect = fullRect;
                // 전체 화면일 때도 이벤트 발행하여 SafeAreaFitter 등 UI 갱신
                OnViewportChanged?.Invoke(fullRect);
                return;
            }

            Rect newRect;
            if (screenAspect > targetAspect)
            {
                // 화면이 기준보다 넓음 → 필러박스 (좌우 검정)
                float scaleWidth = targetAspect / screenAspect;
                float offsetX    = (1f - scaleWidth) * 0.5f;
                newRect = new Rect(offsetX, 0f, scaleWidth, 1f);

                Debug.Log($"[CameraAspectFitter] 필러박스 적용 | 화면: {Screen.width}×{Screen.height} " +
                          $"| scaleWidth: {scaleWidth:F4} | offsetX: {offsetX:F4}");
            }
            else
            {
                // 화면이 기준보다 좁음 → 레터박스 (상하 검정)
                float scaleHeight = screenAspect / targetAspect;
                float offsetY     = (1f - scaleHeight) * 0.5f;
                newRect = new Rect(0f, offsetY, 1f, scaleHeight);

                Debug.Log($"[CameraAspectFitter] 레터박스 적용 | 화면: {Screen.width}×{Screen.height} " +
                          $"| scaleHeight: {scaleHeight:F4} | offsetY: {offsetY:F4}");
            }

            cam.rect = newRect;

            // UI 컴포넌트에 변경된 Viewport 전파
            OnViewportChanged?.Invoke(newRect);
        }

    #if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 Inspector 값 변경 시 즉시 미리보기
        /// </summary>
        private void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null && Application.isPlaying)
                ApplyAspectRatio();
        }
    #endif
    }
}
