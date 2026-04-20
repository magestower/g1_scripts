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
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
