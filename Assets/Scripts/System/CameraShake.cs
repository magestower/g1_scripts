using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>DamageType별 셰이크 강도를 매핑하는 Inspector용 항목</summary>
    [System.Serializable]
    public struct DamageTypeShakeEntry
    {
        public DamageType damageType;
        public ShakePreset preset;
    }

    /// <summary>카메라 셰이크 파라미터 묶음</summary>
    [System.Serializable]
    public struct ShakePreset
    {
        /// <summary>셰이크 지속 시간 (초)</summary>
        public float duration;
        /// <summary>최대 진폭 (미터)</summary>
        public float magnitude;
        /// <summary>Perlin noise 샘플링 속도 — 클수록 빠르게 떨림</summary>
        public float frequency;
    }

    /// <summary>
    /// Perlin noise 기반 카메라 셰이크 싱글톤.
    /// CameraController의 LateUpdate에서 Offset을 읽어 최종 위치에 더한다.
    /// HitStop의 timeScale 조작에 영향받지 않도록 비정규화 시간(unscaledDeltaTime)을 사용한다.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        /// <summary>CameraController가 매 프레임 읽어 카메라 위치에 더하는 셰이크 오프셋</summary>
        public Vector3 Offset { get; private set; }

        // Perlin noise는 같은 좌표를 반복 샘플링하면 고정값이 나오므로
        // 셰이크마다 다른 시드 오프셋을 사용해 방향을 무작위화한다.
        private float _seedX;
        private float _seedY;

        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 셰이크를 시작한다. 이미 실행 중이면 새 셰이크로 교체한다.
        /// </summary>
        /// <param name="preset">셰이크 파라미터</param>
        public void Trigger(ShakePreset preset)
        {
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(preset));
        }

        /// <summary>진행 중인 셰이크를 즉시 중단하고 오프셋을 초기화한다.</summary>
        public void Stop()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }
            Offset = Vector3.zero;
        }

        private IEnumerator ShakeRoutine(ShakePreset preset)
        {
            // 셰이크마다 Perlin 시드를 무작위화해 방향 다변화
            _seedX = Random.value * 100f;
            _seedY = Random.value * 100f;

            float elapsed = 0f;
            while (elapsed < preset.duration)
            {
                // HitStop의 timeScale 영향을 받지 않도록 unscaledDeltaTime 사용
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed * preset.frequency;

                // Perlin noise를 -1~1 범위로 변환 후 진폭 적용
                float x = (Mathf.PerlinNoise(_seedX + t, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, _seedY + t) - 0.5f) * 2f;

                // 지속 시간 후반부로 갈수록 선형 감쇠
                float damping = 1f - Mathf.Clamp01(elapsed / preset.duration);
                Offset = new Vector3(x, y, 0f) * preset.magnitude * damping;

                yield return null;
            }

            Offset = Vector3.zero;
            _shakeCoroutine = null;
        }
    }
}
