using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 몬스터 피격 시 데미지 수치를 월드 공간에 표시하는 플로팅 텍스트 오브젝트.
    /// DamagePopupPool에서 관리되며, Play() 호출 시 상승 + 페이드 아웃 애니메이션을 재생한다.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        /// <summary>데미지 수치를 표시할 TextMeshPro(3D) 컴포넌트</summary>
        [SerializeField] private TextMeshPro label;

        [Header("애니메이션 설정")]
        /// <summary>팝업 전체 재생 시간 (초)</summary>
        [SerializeField] private float riseDuration = 0.8f;

        /// <summary>상승 높이 (월드 단위)</summary>
        [SerializeField] private float riseDistance = 1.5f;

        /// <summary>페이드 아웃이 시작되는 시점 (0~1, 전체 재생 시간 대비 비율)</summary>
        [SerializeField] private float fadeStartRatio = 0.4f;

        [Header("스타일 설정")]
        /// <summary>크리티컬 폰트 크기</summary>
        [SerializeField] private float criticalFontSize = 4f;

        /// <summary>일반 데미지 폰트 크기 (크리티컬 대비 20%)</summary>
        [SerializeField] private float normalFontSize = 0.8f;

        // 크리티컬: 붉은색 본문 + 흰색 아웃라인
        private static readonly Color CriticalColor = new(0.9f, 0.1f, 0.1f, 1f);
        private static readonly Color CriticalOutline = Color.white;

        // 일반: 흰색 본문 + 검은색 아웃라인
        private static readonly Color NormalColor = Color.white;
        private static readonly Color NormalOutline = Color.black;

        private Camera mainCamera;

        /// <summary>이 팝업 전용 Material 인스턴스. Awake에서 1회 생성해 이후 재사용한다.</summary>
        private Material matInstance;

        /// <summary>Camera.main 참조를 캐싱하고 전용 Material 인스턴스를 생성한다.</summary>
        private void Awake()
        {
            mainCamera = Camera.main;
            if (label == null)
            {
                Debug.LogError("[DamagePopup] label(TextMeshPro) 필드가 연결되지 않았습니다.", this);
                return;
            }
            // fontMaterial은 접근마다 복제를 생성하므로 1회만 호출해 캐싱
            matInstance = label.fontMaterial;
            label.fontMaterial = matInstance;
        }

        /// <summary>Awake에서 생성한 Material 인스턴스를 해제한다.</summary>
        private void OnDestroy()
        {
            if (matInstance != null)
                Destroy(matInstance);
        }

        /// <summary>매 프레임 카메라를 향해 빌보드 회전한다.</summary>
        private void LateUpdate()
        {
            if (mainCamera == null) return;
            transform.rotation = mainCamera.transform.rotation;
        }

        /// <summary>
        /// 팝업을 지정 위치에서 재생한다. 애니메이션 완료 시 onComplete 콜백을 호출해 풀에 반납을 트리거한다.
        /// </summary>
        /// <param name="damage">표시할 데미지 수치</param>
        /// <param name="worldPos">몬스터 월드 위치 (목 본 기준)</param>
        /// <param name="isCritical">크리티컬 여부. 폰트 크기·색상·아웃라인에 반영된다.</param>
        /// <param name="onComplete">애니메이션 완료 시 호출할 콜백 (풀 반납용)</param>
        public void Play(int damage, Vector3 worldPos, bool isCritical, Action onComplete)
        {
            if (label == null) { onComplete?.Invoke(); return; }

            // 이전 코루틴이 남아있으면 중단하고 새로 시작
            StopAllCoroutines();

            transform.position = worldPos;
            label.text = damage.ToString();

            // 크리티컬/일반 스타일 적용
            if (isCritical)
            {
                label.fontSize = criticalFontSize;
                label.color = CriticalColor;
                ApplyOutline(CriticalOutline, 0.2f);
            }
            else
            {
                label.fontSize = normalFontSize;
                label.color = NormalColor;
                ApplyOutline(NormalOutline, 0.2f);
            }

            // label.color가 확정된 시점에 baseColor를 캡처해 코루틴에 전달
            StartCoroutine(AnimateCoroutine(label.color, onComplete));
        }

        /// <summary>
        /// fontMaterial 인스턴스에 아웃라인 색상과 두께를 적용한다.
        /// TMP 기본 Material에는 아웃라인이 꺼져 있으므로 런타임에 직접 설정한다.
        /// </summary>
        private void ApplyOutline(Color color, float width)
        {
            matInstance.SetColor("_OutlineColor", color);
            matInstance.SetFloat("_OutlineWidth", width);
        }

        /// <summary>
        /// 위로 상승하면서 서서히 투명해지는 코루틴.
        /// 완료 후 onComplete를 호출해 풀 반납을 요청한다.
        /// </summary>
        /// <param name="baseColor">페이드 아웃 기준 색상. Play()에서 확정된 시점에 전달받는다.</param>
        private IEnumerator AnimateCoroutine(Color baseColor, Action onComplete)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);

                // 위로 상승
                transform.position = startPos + Vector3.up * (riseDistance * t);

                // fadeStartRatio 이후부터 선형으로 알파 감소
                float fadeT = Mathf.InverseLerp(fadeStartRatio, 1f, t);
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - fadeT);

                yield return null;
            }

            // 마지막 프레임 오차로 알파가 완전히 0이 안 될 수 있으므로 명시적으로 설정
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            onComplete?.Invoke();
        }
    }
}
