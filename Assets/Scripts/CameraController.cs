using UnityEngine;


namespace G1
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 5f;           // 기본 거리
    	[SerializeField] private float moveDistance = 7f;		// 이동 중 거리
    	[SerializeField] private float height = 2f;
        [SerializeField] private float smoothSpeed = 10f;
    	[SerializeField] private float distanceLerpSpeed = 3f; // distance 변화 속도

    	private float _currentDistance;		// 현재 보간되는 거리
    	private float _targetDistance;      // 목표 distance
    	private Vector3 _prevShakeOffset;   // 이전 프레임 셰이크 오프셋 — 스무딩 누적 방지용

    	/// <summary>
    	/// Awake: 인스펙터 미할당 시 씬에서 PlayerController를 자동 탐색하여 target을 설정합니다.
    	/// </summary>
    	private void Awake()
        {
            if (target == null)
            {
                // 인스펙터 미할당 시 씬에서 PlayerController를 자동 탐색
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                    target = player.transform;
            }

    		// 초기값 설정
    		_currentDistance = distance;
    		_targetDistance = distance;
    	}

        private void Start()
        {
            if (target == null) return;

            transform.position = target.position - transform.forward * distance + Vector3.up * height;
        }

    	private void OnEnable()
    	{
    		PlayerController.OnMoveStateChanged += HandleMoveState;
    	}

    	private void OnDisable()
    	{
    		PlayerController.OnMoveStateChanged -= HandleMoveState;
    	}

    	private void HandleMoveState(bool isMoving)
    	{
    		_targetDistance = isMoving ? moveDistance : distance;
    	}

    	private void LateUpdate()
        {
            if (target == null) return;

    		// distance를 목표값으로 부드럽게 보간
    		_currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, distanceLerpSpeed * Time.deltaTime);

    		Vector3 desiredPosition = target.position - transform.forward * _currentDistance + Vector3.up * height;

            // 이전 프레임 셰이크 오프셋을 제거한 순수 카메라 위치를 Lerp 시작점으로 사용한다.
            // 제거하지 않으면 셰이크 오프셋이 매 프레임 스무딩에 혼입되어 셰이크 종료 후 드리프트가 발생한다.
            Vector3 basePosition = transform.position - _prevShakeOffset;
            Vector3 smoothed = Vector3.Lerp(basePosition, desiredPosition, smoothSpeed * Time.deltaTime);

            Vector3 shakeOffset = CameraShake.Instance != null ? CameraShake.Instance.Offset : Vector3.zero;
            transform.position = smoothed + shakeOffset;
            _prevShakeOffset = shakeOffset;
        }
    }
}
