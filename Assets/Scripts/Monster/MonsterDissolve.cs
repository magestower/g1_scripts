using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 몬스터 사망 시 디졸브 페이드아웃 연출을 담당하는 컴포넌트.
    /// 사망 시 Renderer의 머티리얼을 디졸브 머티리얼 인스턴스로 교체하고
    /// _DissolveAmount를 0→1로 애니메이션해 서서히 사라지는 효과를 연출한다.
    /// ResetState 시 원본 머티리얼로 복원한다.
    /// </summary>
    public class MonsterDissolve : MonoBehaviour
    {
        [Header("디졸브 셰이더")]
        /// <summary>G1/MonsterDissolve 셰이더가 적용된 머티리얼 템플릿 (인스펙터에서 할당)</summary>
        [SerializeField] private Material dissolveMaterialTemplate;

        [Header("디졸브 설정")]
        /// <summary>디졸브 완료까지 걸리는 시간 (초)</summary>
        [SerializeField] private float dissolveDuration = 1.5f;
        /// <summary>사망 후 디졸브 시작까지의 대기 시간 (초) — 쓰러지는 애니메이션 후 시작</summary>
        [SerializeField] private float dissolveDelay = 0.8f;

        // 캐릭터의 모든 Renderer와 각 원본 머티리얼 배열
        private Renderer[] renderers;
        private Material[][] originalMaterials;
        // 각 Renderer × 슬롯에 할당된 디졸브 머티리얼 인스턴스
        private Material[][] dissolveMaterials;

        private Coroutine dissolveCoroutine;

        // Shader 프로퍼티 해시 (GC 최적화)
        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        private static readonly int MainTexID        = Shader.PropertyToID("_MainTex");
        private static readonly int ColorID          = Shader.PropertyToID("_Color");

        /// <summary>Awake: 자식 포함 모든 Renderer와 원본 머티리얼을 캐싱한다.</summary>
        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterials;
        }

        /// <summary>
        /// 디졸브 연출을 시작한다. MonsterBase.Die()에서 호출한다.
        /// dissolveDelay 후 dissolveDuration 동안 _DissolveAmount 0→1 애니메이션.
        /// </summary>
        public void StartDissolve()
        {
            if (dissolveCoroutine != null)
                StopCoroutine(dissolveCoroutine);
            dissolveCoroutine = StartCoroutine(DissolveRoutine());
        }

        /// <summary>
        /// 디졸브를 즉시 중단하고 원본 머티리얼로 복원한다.
        /// MonsterBase.ResetState()에서 호출한다.
        /// </summary>
        public void ResetDissolve()
        {
            if (dissolveCoroutine != null)
            {
                StopCoroutine(dissolveCoroutine);
                dissolveCoroutine = null;
            }
            RestoreOriginalMaterials();
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>
        /// dissolveDelay 대기 → 디졸브 머티리얼로 교체 → dissolveDuration 동안 0→1 애니메이션.
        /// WaitForSecondsRealtime으로 HitStop timeScale 영향을 받지 않는다.
        /// </summary>
        private IEnumerator DissolveRoutine()
        {
            yield return new WaitForSecondsRealtime(dissolveDelay);

            SwapToDissolveMaterials();

            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.Clamp01(elapsed / dissolveDuration);
                SetDissolveAmount(amount);
                yield return null;
            }

            SetDissolveAmount(1f);
            dissolveCoroutine = null;
        }

        /// <summary>
        /// 모든 Renderer의 머티리얼을 디졸브 인스턴스로 교체한다.
        /// 원본 머티리얼의 _MainTex, _Color를 복사해 외관을 유지한다.
        /// </summary>
        private void SwapToDissolveMaterials()
        {
            if (dissolveMaterialTemplate == null)
            {
                Debug.LogWarning("[MonsterDissolve] dissolveMaterialTemplate이 할당되지 않았습니다.", this);
                return;
            }

            dissolveMaterials = new Material[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] originals = originalMaterials[i];
                Material[] dissolves = new Material[originals.Length];

                for (int j = 0; j < originals.Length; j++)
                {
                    Material inst = new(dissolveMaterialTemplate);
                    if (originals[j] != null)
                    {
                        if (originals[j].HasTexture(MainTexID))
                            inst.SetTexture(MainTexID, originals[j].GetTexture(MainTexID));
                        if (originals[j].HasColor(ColorID))
                            inst.SetColor(ColorID, originals[j].GetColor(ColorID));
                    }
                    inst.SetFloat(DissolveAmountID, 0f);
                    dissolves[j] = inst;
                }

                dissolveMaterials[i] = dissolves;
                renderers[i].materials = dissolves;
            }
        }

        /// <summary>모든 디졸브 머티리얼 인스턴스의 _DissolveAmount를 설정한다.</summary>
        private void SetDissolveAmount(float amount)
        {
            if (dissolveMaterials == null) return;
            for (int i = 0; i < dissolveMaterials.Length; i++)
                for (int j = 0; j < dissolveMaterials[i].Length; j++)
                    if (dissolveMaterials[i][j] != null)
                        dissolveMaterials[i][j].SetFloat(DissolveAmountID, amount);
        }

        /// <summary>모든 Renderer를 원본 머티리얼로 복원하고 생성한 인스턴스를 해제한다.</summary>
        private void RestoreOriginalMaterials()
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterials = originalMaterials[i];
            }

            if (dissolveMaterials != null)
            {
                for (int i = 0; i < dissolveMaterials.Length; i++)
                    for (int j = 0; j < dissolveMaterials[i].Length; j++)
                        if (dissolveMaterials[i][j] != null)
                            Destroy(dissolveMaterials[i][j]);
                dissolveMaterials = null;
            }
        }

        /// <summary>오브젝트 파괴 시 머티리얼 인스턴스 메모리 누수 방지.</summary>
        private void OnDestroy()
        {
            ResetDissolve();
        }
    }
}
