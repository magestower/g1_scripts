using UnityEngine;
using UnityEngine.UI;



namespace G1
{
    /// <summary>
    /// RawImage 의 SetNativeSize() 결과를 기준 크기(1배)로 삼아
    /// Width / Height 를 배수로 조절합니다.
    /// [ExecuteAlways] 로 에디터에서 Inspector 수정 시 즉시 반영됩니다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class BackgroundScaler : MonoBehaviour
    {
        [Header("배경 크기 배수 (1 = 텍스처 원본 크기, 2 = 2배 확대 등)")]
        [SerializeField, Min(0.1f)] private float sizeMultiplier = 1f;

        private RectTransform rectTransform;
        private RawImage rawImage;

        /// <summary>
        /// Awake: 컴포넌트 캐싱 및 초기 크기 적용
        /// </summary>
        private void Awake()
        {
            CacheComponents();
            ApplyScale();
        }

        /// <summary>
        /// Inspector 값 변경 시 에디터에서도 즉시 크기 반영 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            CacheComponents();
    #if UNITY_EDITOR
            // OnValidate 는 레이아웃 갱신 이전에 호출되므로 한 프레임 뒤에 적용
            UnityEditor.EditorApplication.delayCall -= ApplyScale;
            UnityEditor.EditorApplication.delayCall += ApplyScale;
    #endif
        }

        /// <summary>
        /// 필요한 컴포넌트를 캐싱합니다.
        /// </summary>
        private void CacheComponents()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rawImage == null)      rawImage      = GetComponent<RawImage>();
        }

        /// <summary>fjrk q
        /// SetNativeSize() 로 텍스처 원본 크기를 sizeDelta 에 설정한 뒤
        /// sizeMultiplier 배수를 곱해 최종 Width / Height 를 적용합니다.
        /// 앵커·피벗은 최초 1회만 설정하고, anchoredPosition 은 건드리지 않습니다.
        /// (위치 관리는 BackgroundParallax 가 전담합니다.)
        /// </summary>
        private void ApplyScale()
        {
            if (rectTransform == null || rawImage == null) return;

            // 텍스처가 없으면 처리 중단
            if (rawImage.texture == null) return;

            // 중앙 앵커 + 중앙 피벗 고정 (위치는 변경하지 않음)
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot     = new Vector2(0.5f, 0.5f);

            // SetNativeSize() 로 텍스처 원본 해상도를 sizeDelta 에 반영
            rawImage.SetNativeSize();
            Vector2 nativeSize = rectTransform.sizeDelta;

            // 원본 크기 × 배수 → sizeDelta (Width / Height) 적용
            rectTransform.sizeDelta = nativeSize * sizeMultiplier;
        }
    }
}
