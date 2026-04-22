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

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>싱글톤을 설정하고 AudioSource 풀을 미리 채운다. 씬 전환 후에도 유지된다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < prewarmSize; i++)
                pool.Push(CreateSource());
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
        public void Play(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitchVariance = 0.1f)
        {
            if (clip == null) return;

            AudioSource source = pool.Count > 0 ? pool.Pop() : CreateSource();
            source.transform.position = worldPos;
            source.clip = clip;
            source.volume = volume;
            // 피치 미세 변주로 반복 재생 시 기계적인 느낌 방지
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.gameObject.SetActive(true);
            source.Play();

            StartCoroutine(ReleaseWhenDone(source));
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>재생이 완료될 때까지 대기한 후 AudioSource를 풀에 반납한다.</summary>
        private System.Collections.IEnumerator ReleaseWhenDone(AudioSource source)
        {
            // clip 길이만큼 실제 시간 기준으로 대기 (HitStop timeScale 영향 배제)
            yield return new WaitForSecondsRealtime(source.clip.length / Mathf.Max(0.01f, source.pitch));
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
