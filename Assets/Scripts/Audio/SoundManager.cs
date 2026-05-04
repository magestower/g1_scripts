using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// AudioSource 풀을 관리하는 싱글톤 사운드 매니저.
    /// 여러 몬스터가 동시에 피격되어도 소리가 끊기지 않도록 풀에서 AudioSource를 할당해 재생한다.
    /// 재생이 끝난 AudioSource는 자동으로 풀에 반납된다.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        /// <summary>풀에 미리 생성해둘 AudioSource 수</summary>
        [SerializeField] private int prewarmSize = 16;

        /// <summary>3D 사운드 기본 최대 청취 거리</summary>
        [SerializeField] private float defaultMaxDistance = 20f;

        private readonly Stack<AudioSource> pool = new();
        /// <summary>재생 중인 AudioSource → 코루틴 매핑. 게임오브젝트 비활성화 시 누수 방지용.</summary>
        private readonly Dictionary<AudioSource, Coroutine> activeCoroutines = new();

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>
        /// 게임 시작 시 SoundManager 인스턴스가 없으면 자동으로 생성한다.
        /// 씬에 수동 배치 없이도 첫 호출 전에 항상 존재가 보장된다.
        /// </summary>
        // AfterSceneLoad: Awake가 먼저 실행되므로 씬 수동 배치 오브젝트가 있으면 Instance가 이미 설정됨
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfAbsent()
        {
            if (Instance != null) return;
            GameObject go = new("SoundManager");
            go.AddComponent<SoundManager>();
        }

        /// <summary>싱글톤을 설정하고 AudioSource 풀을 미리 채운다. 씬 전환 후에도 유지된다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad는 루트 오브젝트에만 적용 가능하므로 부모에서 분리 후 호출
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < prewarmSize; i++)
                pool.Push(CreateSource());
        }

        /// <summary>
        /// 게임오브젝트 비활성화 시 진행 중인 코루틴을 모두 명시적으로 중단하고
        /// 해당 AudioSource를 풀에 반납해 누수를 방지한다.
        /// </summary>
        private void OnDisable()
        {
            foreach (var (source, coroutine) in activeCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
                source.Stop();
                source.clip = null;
                source.gameObject.SetActive(false);
                pool.Push(source);
            }
            activeCoroutines.Clear();
        }

        // ─────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────

        /// <summary>
        /// 지정 위치에서 클립을 1회 재생한다.
        /// </summary>
        /// <param name="clip">재생할 오디오 클립</param>
        /// <param name="worldPos">3D 재생 위치</param>
        /// <param name="volume">볼륨 (0~1)</param>
        /// <param name="pitchVariance">랜덤 피치 편차. 0이면 고정 피치 1.</param>
        /// <param name="priority">오디오 우선순위. 0=최고, 256=최저, 기본값=128. 채널 한도 초과 시 우선순위 낮은 소리가 먼저 끊긴다.</param>
        public void Play(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitchVariance = 0.1f, int priority = 128)
        {
            if (clip == null) return;

            // pitchVariance를 0~0.9로 제한해 pitch가 음수(역재생)가 되는 경우 방지
            float safeVariance = Mathf.Clamp(pitchVariance, 0f, 0.9f);

            AudioSource source = pool.Count > 0 ? pool.Pop() : CreateSource();
            source.transform.position = worldPos;
            source.clip = clip;
            source.volume = volume;
            source.priority = Mathf.Clamp(priority, 0, 256);
            // 피치 미세 변주로 반복 재생 시 기계적인 느낌 방지
            source.pitch = 1f + Random.Range(-safeVariance, safeVariance);
            source.gameObject.SetActive(true);
            source.Play();

            // 코루틴 참조를 보관해 게임오브젝트 비활성화 시 누수를 추적할 수 있도록 함
            var coroutine = StartCoroutine(ReleaseWhenDone(source));
            activeCoroutines[source] = coroutine;
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>재생이 완료될 때까지 대기한 후 AudioSource를 풀에 반납한다.</summary>
        private System.Collections.IEnumerator ReleaseWhenDone(AudioSource source)
        {
            // clip 길이만큼 실제 시간 기준으로 대기 (HitStop timeScale 영향 배제)
            // pitch를 0.5~2 범위로 클램프해 극단적 pitchVariance 입력 시 대기 시간 폭발 방지
            float safePitch = Mathf.Clamp(source.pitch, 0.5f, 2f);
            yield return new WaitForSecondsRealtime(source.clip.length / safePitch);
            // OnDisable이 먼저 처리했으면 이미 Remove+Push 완료 → 중복 반납 방지
            if (!activeCoroutines.Remove(source)) yield break;
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            pool.Push(source);
        }

        /// <summary>새 AudioSource 게임오브젝트를 생성하고 3D 공간음으로 설정한다.</summary>
        private AudioSource CreateSource()
        {
            GameObject go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;        // 완전 3D 사운드
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = defaultMaxDistance;
            src.playOnAwake = false;
            go.SetActive(false);
            return src;
        }
    }
}
