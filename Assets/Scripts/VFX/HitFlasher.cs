using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 시 모든 자식 렌더러를 흰색으로 번쩍이게 하는 컴포넌트.
    /// Neko Legends Cel Shader의 _light(라이트 색상)를 흰색으로 올리는 방식을 사용한다.
    /// MonsterBase.TakeDamage()에서 Flash()를 호출한다.
    /// </summary>
    public class HitFlasher : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────
        [Header("플래시 설정")]
        /// <summary>플래시 색상 (기본 흰색)</summary>
        [SerializeField] private Color flashColor = Color.white;
        /// <summary>플래시 지속 시간 (초)</summary>
        [SerializeField] private float flashDuration = 0.12f;

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        private RendererEntry[] entries;
        private Coroutine flashCoroutine;

        // Neko Legends Cel Shader 프로퍼티
        private static readonly int LightColorId    = Shader.PropertyToID("_light");
        // 폴백: 표준 URP/Built-in 프로퍼티
        private static readonly int BaseColorId     = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId         = Shader.PropertyToID("_Color");

        /// <summary>머티리얼 인스턴스와 원본 색상, 사용할 프로퍼티 ID를 묶는 구조체</summary>
        private struct RendererEntry
        {
            public Material mat;
            public Color    originalColor;
            public int      colorPropertyId;
        }

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>
        /// 자식 렌더러의 머티리얼 인스턴스를 캐싱한다.
        /// 셰이더 종류에 따라 _light → _BaseColor → _Color 순으로 프로퍼티를 선택한다.
        /// r.materials는 인스턴스를 반환하므로 별도 Instantiate 불필요.
        /// </summary>
        private void Awake()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            var list = new System.Collections.Generic.List<RendererEntry>();

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    int propId;
                    if      (mat.HasProperty(LightColorId)) propId = LightColorId;
                    else if (mat.HasProperty(BaseColorId))  propId = BaseColorId;
                    else if (mat.HasProperty(ColorId))      propId = ColorId;
                    else continue;

                    list.Add(new RendererEntry
                    {
                        mat             = mat,
                        originalColor   = mat.GetColor(propId),
                        colorPropertyId = propId
                    });
                }
            }

            entries = list.ToArray();
        }

        /// <summary>
        /// 오브젝트 비활성화 시 코루틴을 중단하고 색상을 원본으로 복원한다.
        /// SetActive(false) 또는 파괴 직전에 호출되므로 색상이 flashColor로 굳는 것을 방지한다.
        /// </summary>
        private void OnDisable()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            RestoreAllColor();
        }

        /// <summary>
        /// 오브젝트 파괴 시 머티리얼 인스턴스를 해제한다.
        /// r.materials로 생성된 인스턴스는 Unity가 자동으로 해제하지 않으므로 직접 파괴해야 한다.
        /// </summary>
        private void OnDestroy()
        {
            if (entries == null) return;
            foreach (RendererEntry entry in entries)
            {
                if (entry.mat != null)
                    Destroy(entry.mat);
            }
        }

        // ─────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────

        /// <summary>
        /// 피격 플래시를 재생한다. 이미 재생 중이면 처음부터 다시 시작한다.
        /// MonsterBase.TakeDamage()에서 호출된다.
        /// </summary>
        public void Flash()
        {
            if (entries == null || entries.Length == 0) return;
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>
        /// 색상을 즉시 flashColor로 변경한 뒤
        /// flashDuration 동안 원본 색상으로 선형 보간해 복원한다.
        /// </summary>
        private IEnumerator FlashRoutine()
        {
            SetAllColor(flashColor);

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                for (int i = 0; i < entries.Length; i++)
                    entries[i].mat.SetColor(entries[i].colorPropertyId,
                        Color.Lerp(flashColor, entries[i].originalColor, t));
                yield return null;
            }

            RestoreAllColor();
            flashCoroutine = null;
        }

        /// <summary>모든 머티리얼에 동일한 색상을 적용한다.</summary>
        private void SetAllColor(Color color)
        {
            for (int i = 0; i < entries.Length; i++)
                entries[i].mat.SetColor(entries[i].colorPropertyId, color);
        }

        /// <summary>모든 머티리얼을 원본 색상으로 복원한다.</summary>
        private void RestoreAllColor()
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
                entries[i].mat.SetColor(entries[i].colorPropertyId, entries[i].originalColor);
        }
    }
}


