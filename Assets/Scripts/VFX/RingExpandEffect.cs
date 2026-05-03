using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 원형 링이 바깥으로 확장되면서 얇아지고 페이드아웃되는 이펙트.
    /// G1/RingExpand 셰이더를 사용하는 머티리얼이 할당된 Quad에 붙여 사용한다.
    /// Inspector의 Effect Type을 Ring으로 설정해야 HitSparkPool이 올바르게 탐색한다.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public partial class RingExpandEffect : EffectBase
    {
        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        /// <summary>링이 확장되는 총 시간(초). 0 이하면 즉시 종료되므로 반드시 양수로 설정.</summary>
        [SerializeField, Min(0.01f)] private float duration = 0.4f;

        /// <summary>시작 크기 (월드 단위 Quad 스케일)</summary>
        [SerializeField, Range(0f, 5f)] private float startRadius = 0f;

        /// <summary>종료 크기 (월드 단위 Quad 스케일). 클수록 더 크게 확장</summary>
        [SerializeField, Range(0f, 10f)] private float endRadius = 3f;

        /// <summary>시작 두께 (월드 단위)</summary>
        [SerializeField, Range(0f, 2f)] private float startThickness = 0.3f;

        /// <summary>종료 두께 (월드 단위, 0에 가까울수록 얇게 사라짐)</summary>
        [SerializeField, Range(0f, 2f)] private float endThickness = 0.05f;

        /// <summary>링 색상</summary>
        [SerializeField] private Color color = Color.white;

        /// <summary>링 엣지 부드러움 (0 = 완전 선명, 올릴수록 부드러워짐)</summary>
        [SerializeField, Range(0f, 0.05f)] private float smoothness = 0f;

        // ─────────────────────────────────────────
        // 내부 상태
        // ─────────────────────────────────────────

        private static readonly int PropRadius     = Shader.PropertyToID("_Radius");
        private static readonly int PropThickness  = Shader.PropertyToID("_Thickness");
        private static readonly int PropAlpha      = Shader.PropertyToID("_Alpha");
        private static readonly int PropColor      = Shader.PropertyToID("_Color");
        private static readonly int PropSmoothness = Shader.PropertyToID("_Smoothness");

        private Material mat;
        private Coroutine animCoroutine;
        private Camera mainCam;
        private ParticleSystem[] childParticles;

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>머티리얼, 메인 카메라, 자식 파티클 캐싱</summary>
        private void Awake()
        {
            InitMat();
            mainCam = Camera.main;
            childParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>매 프레임 카메라를 향하도록 회전 (빌보드)</summary>
        private void LateUpdate()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;
            transform.rotation = mainCam.transform.rotation;
        }

        /// <summary>
        /// 머티리얼 인스턴스 초기화. Awake에서 1회 실행되며,
        /// Play()에서 재호출 시 이미 초기화된 경우 early-return으로 무해하게 처리된다.
        /// </summary>
        private void InitMat()
        {
            if (mat != null) return;
            mat = GetComponent<MeshRenderer>().material;
        }

        /// <summary>애니메이션 중 변하지 않는 셰이더 프로퍼티를 설정한다. Play/EditorStartPreview 시 1회 호출.</summary>
        private void ApplyStaticShaderProps()
        {
            if (mat == null) return;
            mat.SetColor(PropColor, color);
            mat.SetFloat(PropSmoothness, smoothness);
            mat.SetFloat(PropRadius, 0.5f);
        }

        /// <summary>오브젝트 파괴 시 머티리얼 인스턴스 해제</summary>
        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }

        // ─────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────

        /// <summary>에디터에서 현재 위치로 이펙트를 재생한다. 플레이 모드가 아니면 에디터 미리보기로 대체.</summary>
        [ContextMenu("테스트 재생")]
        private void PlayAtCurrentPosition()
        {
            Play(transform.position);
        }

        /// <summary>
        /// 지정한 월드 위치에서 링 이펙트를 재생한다.
        /// 이미 재생 중이면 처음부터 다시 시작한다.
        /// </summary>
        /// <param name="worldPos">이펙트 중심 월드 좌표</param>
        public override void Play(Vector3 worldPos)
        {
            InitMat();
            transform.position = worldPos;

            // SetActive(true)를 StopCoroutine 전에 해야 코루틴 중단/시작이 안전하게 동작
            gameObject.SetActive(true);

            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

            ApplyStaticShaderProps();

            // 시작 전 스케일을 즉시 초기화해 이전 상태가 한 프레임 보이지 않도록 함
            ApplyAtTime(0f);
            animCoroutine = StartCoroutine(AnimateRoutine());

            // 자식 파티클 재시작 (이미 재생 중이어도 처음부터 다시 재생)
            if (childParticles != null)
                foreach (ParticleSystem ps in childParticles)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play();
                }
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>duration 동안 반지름/두께/알파를 보간해 링 확장 연출을 수행한다.</summary>
        private IEnumerator AnimateRoutine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                ApplyAtTime(elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            animCoroutine = null;
            gameObject.SetActive(false);
        }

        /// <summary>Quad 스케일과 매 프레임 변하는 셰이더 값(_Thickness, _Alpha)을 t(0~1)로 갱신한다.</summary>
        private void ApplyAtTime(float t)
        {
            if (mat == null) return;
            float eased = 1f - (1f - t) * (1f - t);

            // Quad 스케일로 월드 크기 제어 (startRadius → endRadius, 이징 적용)
            float worldSize = Mathf.Lerp(
                Mathf.Max(startRadius, 0.01f),
                Mathf.Max(endRadius,   0.01f),
                eased);
            transform.localScale = Vector3.one * worldSize;

            // 월드 단위 두께를 UV 비율로 변환. worldSize 최솟값 0.01 보장으로 나눗셈 안전.
            float thick = Mathf.Lerp(startThickness, endThickness, t) / worldSize;

            mat.SetFloat(PropThickness, thick);
            mat.SetFloat(PropAlpha,     1f - t);
        }

    }
}
