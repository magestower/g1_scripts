using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;



namespace G1
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 플레이어 상태 — Idle(정지), Moving(이동 중), Attacking(공격 중), Dead(사망)
        /// </summary>
        enum PlayerState { Idle, Moving, Attacking, Dead }

        /// <summary>
        /// 애니메이터 파라미터 해시 — StringToHash로 앱 시작 시 1회 변환하여 매 프레임 변환 비용 제거
        /// </summary>
        private static class AnimParam
        {
            public static readonly int IsWalking = Animator.StringToHash("isWalking");
            public static readonly int Hit = Animator.StringToHash("Hit");
            public static readonly int IsDead = Animator.StringToHash("IsDead");
        }

        [Header("체력")]
        [SerializeField] private int maxHealth = 100;
        /// <summary>Hit 트리거 재발동 억제 시간(초)</summary>
        [SerializeField] private float hitTriggerCooldown = 1f;
        private int currentHealth;
        private float lastHitTriggerTime = -999f;

        /// <summary>현재 체력이 0 이하인지 여부 — IDamageable 구현, PlayerState.Dead 여부로 판단</summary>
        public bool IsDead => playerState == PlayerState.Dead;

        /// <summary>
        /// 데미지를 받아 체력을 감소시킨다. 사망 시 Dead 상태로 전이한다.
        /// </summary>
        /// <param name="damage">적용할 데미지 양 (양수)</param>
        public void TakeDamage(int damage, bool isCritical = false)
        {
            if (IsDead) return;

            currentHealth -= damage;

            // 쿨다운 이내 중복 Hit 트리거 억제
            if (Time.time - lastHitTriggerTime >= hitTriggerCooldown)
            {
                lastHitTriggerTime = Time.time;
                animator.SetTrigger(AnimParam.Hit);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                // 사망 전 Hit 트리거를 제거해 GetHit 애니메이션이 사망 애니메이션보다 먼저 재생되는 것을 방지
                animator.ResetTrigger(AnimParam.Hit);
                TransitionTo(PlayerState.Dead);
            }
        }


        [Header("공격 데이터")]
        /// <summary>
        /// 무기 타입별 기본 공격 AttackData — WeaponType enum 순서와 인덱스가 반드시 일치해야 합니다.
        /// [0]=Unarmed(펀치), [1]=Fryingpan, ...
        /// </summary>
        [SerializeField] private AttackData[] weaponAttackData;

        /// <summary>스킬 슬롯 AttackData — 슬롯 1~3, 향후 스킬 추가 시 사용</summary>
        [SerializeField] private AttackData[] skillSlots;

        /// <summary>현재 실행 중인 공격 데이터 — AttackStateBehaviour에서 읽어 동작 결정</summary>
        public AttackData CurrentAttackData { get; private set; }

        /// <summary>현재 Idle 상태 여부 — NeckController 등 외부에서 비활성 조건 판단에 사용</summary>
        public bool IsIdle => playerState == PlayerState.Idle;

        /// <summary>현재 공격 중 여부 — GetHitStateBehaviour에서 강제 공격 종료 판단에 사용</summary>
        public bool IsAttacking => playerState == PlayerState.Attacking;

        /// <summary>현재 장착 무기 타입 — 미장착 시 Unarmed 반환</summary>
        public WeaponType CurrentWeaponType
        {
            get
            {
                if (equipmentManager != null)
                {
                    EquipmentData equipped = equipmentManager.GetEquippedData(EquipmentSlot.Weapon);
                    if (equipped != null)
                        return equipped.weaponType;
                }
                return WeaponType.Unarmed;
            }
        }

        /// <summary>장착 무기 조회용 — 인스펙터 미할당 시 Awake에서 자동 탐색</summary>
        [SerializeField] private CharacterEquipmentManager equipmentManager;

        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        private CharacterController controller;
        private Animator animator;
        private PlayerState playerState = PlayerState.Idle;
        [SerializeField] private float moveResumeDelay = 0.1f; // 이동 대기 시간

        [Header("몬스터 자동 조준")]
        [SerializeField] private string monsterTag = "Monster";         // 탐색할 몬스터 태그
        [SerializeField] private float monsterDetectRadius = 5f;        // 몬스터 감지 반경
        [SerializeField] private float autoAimUpdateInterval = 0.1f;    // 자동 조준 갱신 주기 (초)

        [Header("공격 히트 판정")]
        [SerializeField] private float hitRadius = 1.5f;                // 공격 판정 반경 (플레이어 중심 기준)

        private float autoAimTimer = 0f;                    // 자동 조준 갱신 타이머
        private readonly Collider[] monsterHitBuffer = new Collider[16]; // OverlapSphereNonAlloc 전용 버퍼 (GC 방지)

        private Transform _currentAttackTarget;
        /// <summary>
        /// 현재 자동 조준 중인 몬스터 트랜스폼. 범위 밖이거나 없으면 null.
        /// 값이 변경될 때만 OnAttackTargetChanged 이벤트를 발행한다.
        /// </summary>
        public Transform CurrentAttackTarget
        {
            get => _currentAttackTarget;
            private set
            {
                if (_currentAttackTarget == value) return;
                _currentAttackTarget = value;
                OnAttackTargetChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// VirtualJoystick은 첫 프레임 말미(WaitForEndOfFrame)에 Canvas 위치를 재보정합니다.
        /// 보정 전 첫 프레임에 비정상 axis 값이 읽혀 Moving 상태로 전이되는 것을 방지하기 위해
        /// 두 번째 프레임부터 입력을 허용합니다.
        /// </summary>
        private bool inputReady = false;

        /// <summary>
        /// BackgroundParallax 경계 상태.
        ///  +1 : 배경이 양방향 경계 → 캐릭터 음방향 입력 차단
        ///  -1 : 배경이 음방향 경계 → 캐릭터 양방향 입력 차단
        ///   0 : 자유 이동
        /// </summary>
        private Vector2 bgBoundary = Vector2.zero;

        [Header("감지 반경 표시")]
        [SerializeField] private bool showDetectRadius = true;                      // 감지 반경 표시 여부
        [SerializeField] private Color detectRadiusColor = new(1f, 0f, 0f, 0.5f);  // 반경 표시 색상
        [SerializeField] private Material rangeIndicatorMaterial;                   // 감지 반경 LineRenderer 머티리얼 (인스펙터에서 직접 할당)
        [SerializeField] private float rangeIndicatorWidth = 0.05f;                // 감지 반경 라인 두께
        private LineRenderer rangeIndicator;                                        // 감지 반경 시각화용 LineRenderer
        private const int RangeIndicatorSegments = 36;                              // 원 분할 수


        public static event Action<bool> OnMoveStateChanged;

        /// <summary>공격 시작/종료 이벤트 — true: 공격 시작, false: 공격 종료</summary>
        public static event Action<bool> OnAttackStateChanged;

        /// <summary>자동 조준 타겟 변경 이벤트 — 타겟이 없으면 null 전달</summary>
        public static event Action<Transform> OnAttackTargetChanged;

        /// <summary>플레이어 사망 이벤트</summary>
        public static event Action OnPlayerDead;


        private void Awake()
        {
            currentHealth = maxHealth;
            controller = GetComponent<CharacterController>();
            controller.skinWidth = 0.01f;
            animator = GetComponent<Animator>();

            // 인스펙터 미할당 시 같은 오브젝트에서 자동 탐색
            if (equipmentManager == null)
                equipmentManager = GetComponent<CharacterEquipmentManager>();

            InitRangeIndicator();

            // 시작 시 애니메이터 파라미터를 명시적으로 초기화 (파라미터 기본값 의존 방지)
            animator.SetBool(AnimParam.IsWalking, false);
        }

        /// <summary>
        /// VirtualJoystick Canvas 위치 재보정(WaitForEndOfFrame) 이후
        /// 안정적인 입력을 위해 한 프레임 대기 후 입력을 활성화합니다.
        /// </summary>
        private void Start()
        {
            StartCoroutine(EnableInputNextFrame());
        }

        /// <summary>
        /// OnEnable: BackgroundParallax 경계 이벤트 구독
        /// </summary>
        private void OnEnable()
        {
            BackgroundParallax.OnBoundaryChanged += OnBgBoundaryChanged;
        }

        /// <summary>
        /// OnDisable: BackgroundParallax 경계 이벤트 구독 해제
        /// </summary>
        private void OnDisable()
        {
            BackgroundParallax.OnBoundaryChanged -= OnBgBoundaryChanged;
        }

        /// <summary>
        /// 배경 경계 상태 변경 시 호출됩니다.
        /// </summary>
        /// <param name="boundary">경계 방향 벡터 (+1/-1/0)</param>
        private void OnBgBoundaryChanged(Vector2 boundary)
        {
            bgBoundary = boundary;
        }

        /// <summary>
        /// 한 프레임 대기 후 inputReady를 true로 설정합니다.
        /// VirtualJoystick의 Activate() 코루틴(WaitForEndOfFrame)보다
        /// 한 프레임 뒤에 실행되므로 정확한 axis 값을 보장합니다.
        /// </summary>
        private IEnumerator EnableInputNextFrame()
        {
            yield return null;
            inputReady = true;
        }

        /// <summary>
        /// 몬스터 감지 반경을 LineRenderer로 시각화합니다.
        /// showDetectRadius가 false이면 비활성화 상태로 생성됩니다.
        /// </summary>
        private void InitRangeIndicator()
        {
            rangeIndicator = gameObject.AddComponent<LineRenderer>();
            rangeIndicator.loop = true;
            rangeIndicator.widthMultiplier = rangeIndicatorWidth;
            rangeIndicator.useWorldSpace = false;
            rangeIndicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeIndicator.receiveShadows = false;

            // 인스펙터에서 머티리얼이 할당된 경우 사용, 미할당 시 런타임 생성 (Shader.Find 실패 방지)
            if (rangeIndicatorMaterial != null)
            {
                rangeIndicator.material = rangeIndicatorMaterial;
            }
            else
            {
                Shader fallback = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Sprites/Default");
                if (fallback != null)
                    rangeIndicator.material = new Material(fallback);
                else
                    Debug.LogWarning("[PlayerController] rangeIndicator 머티리얼 셰이더를 찾을 수 없습니다. 인스펙터에서 직접 할당해 주세요.");
            }

            rangeIndicator.positionCount = RangeIndicatorSegments;
            UpdateRangeIndicatorShape();
        }

        /// <summary>
        /// 인스펙터에서 값이 변경될 때 호출 — 감지 반경 및 색상을 즉시 반영합니다.
        /// </summary>
        private void OnValidate()
        {
            if (rangeIndicator == null) return;
            UpdateRangeIndicatorShape();
        }

        /// <summary>
        /// 감지 반경 원형 꼭짓점, 색상, 활성화 상태를 갱신합니다.
        /// </summary>
        private void UpdateRangeIndicatorShape()
        {
            float angleStep = 360f / RangeIndicatorSegments;
            for (int i = 0; i < RangeIndicatorSegments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                rangeIndicator.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * monsterDetectRadius,
                    0f,
                    Mathf.Sin(angle) * monsterDetectRadius
                ));
            }
            rangeIndicator.startColor = detectRadiusColor;
            rangeIndicator.endColor = detectRadiusColor;
            rangeIndicator.enabled = showDetectRadius;
        }

        private void Update()
        {
            // 사망 상태에서는 모든 입력/이동 처리 중단
            if (IsDead) return;

            Vector3 horizontalMove = Vector3.zero;

            // 이동 및 상태 전환 처리 (Idle, Moving 상태에서만)
            if (playerState == PlayerState.Idle || playerState == PlayerState.Moving)
                horizontalMove = HandleMovement();

            controller.Move(horizontalMove * Time.deltaTime);
        }

        /// <summary>
        /// 플랫폼에 따라 이동 입력 벡터를 반환합니다.
        /// 모바일: 가상 조이스틱, 에디터(Windows): 키보드 폴백.
        /// VirtualJoystick 초기화 전(첫 프레임)에는 Vector2.zero를 반환합니다.
        /// </summary>
        /// <returns>정규화되지 않은 2D 입력 벡터</returns>
        private Vector2 GetInput()
        {
            // VirtualJoystick Canvas 재보정 전 첫 프레임 비정상 입력 차단
            if (!inputReady) return Vector2.zero;

            Vector2 input = Terresquall.VirtualJoystick.GetAxis();

#if UNITY_EDITOR_WIN
            if (input.sqrMagnitude < 0.01f)
            {
                float h = 0f;
                float v = 0f;

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;

                input = new Vector2(h, v);
            }
#endif

            // 배경 경계 도달 시 해당 방향 입력 차단
            // bgBoundary.x =  1 → 배경이 +maxX (캐릭터가 좌(-X) 이동 중) → 좌 입력 차단
            // bgBoundary.x = -1 → 배경이 -maxX (캐릭터가 우(+X) 이동 중) → 우 입력 차단
            if (bgBoundary.x > 0f && input.x < 0f) input.x = 0f;
            if (bgBoundary.x < 0f && input.x > 0f) input.x = 0f;

            /* Y축 경계는 일단 보류
    		 * 상호연동 LINK: D:\workspace\code\G1\Assets\Scripts\UI\BackgroundParallax.cs#L96
            if (bgBoundary.y >  0f && input.y < 0f) input.y = 0f;
            if (bgBoundary.y < 0f  && input.y > 0f) input.y = 0f;
    		*/

            return input;
        }

        /// <summary>
        /// 이동 입력을 처리합니다. 회전 및 Idle↔Moving 상태 전환을 수행하고 수평 이동 벡터를 반환합니다.
        /// </summary>
        /// <returns>이번 프레임에 적용할 수평 이동 벡터</returns>
        private Vector3 HandleMovement()
        {
            Vector2 input = GetInput();
            bool isMoving = input.sqrMagnitude > 0.01f;
            Vector3 moveDir = new(input.x, 0f, input.y);

            // 이동 방향으로 회전 (수평 방향만)
            if (isMoving)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
            else
            {
                // 정지 상태: 주기적으로 가장 가까운 몬스터를 향해 회전
                autoAimTimer += Time.deltaTime;
                if (autoAimTimer >= autoAimUpdateInterval)
                {
                    autoAimTimer = 0f;
                    FaceNearestMonster();
                }
            }

            TransitionTo(isMoving ? PlayerState.Moving : PlayerState.Idle);

            return moveDir * moveSpeed;
        }

        /// <summary>
        /// 감지 반경 내에서 가장 가까운 몬스터를 찾아 플레이어가 해당 방향을 바라보도록 즉시 회전합니다.
        /// 현재 타겟이 공격 사정거리 안에 살아있으면 타겟을 유지하고 해당 방향을 바라봅니다.
        /// 몬스터가 없으면 아무 동작도 하지 않습니다.
        /// </summary>
        private void FaceNearestMonster()
        {
            // 현재 타겟이 사정거리 안에서 살아있으면 타겟 고정
            // activeInHierarchy 체크: 풀 반납 후 ResetState로 IsDead=false가 된 오브젝트 오판 방지
            if (CurrentAttackTarget != null && CurrentAttackTarget.gameObject.activeInHierarchy)
            {
                float distToCurrent = Vector3.Distance(transform.position, CurrentAttackTarget.position);
                bool currentAlive = !CurrentAttackTarget.TryGetComponent<IDamageable>(out var d) || !d.IsDead;
                if (currentAlive && distToCurrent <= hitRadius)
                {
                    FaceToward(CurrentAttackTarget);
                    return;
                }
            }

            // monsterTag를 가진 콜라이더를 감지 반경 내에서 탐색 (NonAlloc으로 GC 방지)
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, monsterDetectRadius, monsterHitBuffer);

            Transform nearest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = monsterHitBuffer[i];
                if (!hit.CompareTag(monsterTag)) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }

            CurrentAttackTarget = nearest;
            if (nearest == null) return;

            FaceToward(nearest);
        }

        /// <summary>
        /// 대상 방향(수평)으로 즉시 회전합니다. 거리가 너무 가까우면 무시합니다.
        /// </summary>
        /// <param name="target">바라볼 대상 트랜스폼</param>
        private void FaceToward(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }



        /// <summary>
        /// 슬롯에 해당하는 공격을 실행합니다.
        /// AttackSlot.Basic: 기본 공격 — 장착 무기의 WeaponType으로 weaponAttackData를 선택합니다.
        ///                   무기 미장착 시 WeaponType.Unarmed AttackData를 사용합니다.
        /// AttackSlot.Skill1~: 스킬 슬롯 — skillSlots[(int)slot - 1] AttackData를 사용합니다.
        /// </summary>
        /// <param name="slot">공격 슬롯 (AttackSlot.Basic=기본공격, Skill1~=스킬)</param>
        public void OnAttackStart(AttackSlot slot = AttackSlot.Basic)
        {
            // Idle, Moving 상태에서만 공격 가능
            if (playerState == PlayerState.Attacking || IsDead) return;

            AttackData data = ResolveAttackData(slot);
            if (data == null) return;

            CurrentAttackData = data;
            TransitionTo(PlayerState.Attacking);
        }

        /// <summary>
        /// 공격 슬롯과 현재 장착 무기를 기반으로 사용할 AttackData를 결정합니다.
        /// </summary>
        /// <param name="slot">공격 슬롯</param>
        /// <returns>실행할 AttackData, 유효하지 않으면 null</returns>
        private AttackData ResolveAttackData(AttackSlot slot)
        {
            if (slot == AttackSlot.Basic)
            {
                // 기본 공격 — 현재 장착 무기 타입으로 인덱스 결정
                int typeIndex = (int)CurrentWeaponType;
                if (weaponAttackData == null || typeIndex >= weaponAttackData.Length)
                {
                    Debug.LogWarning($"[PlayerController] weaponAttackData[{typeIndex}]({CurrentWeaponType})가 할당되지 않았습니다.");
                    return null;
                }
                return weaponAttackData[typeIndex];
            }
            else
            {
                // 스킬 슬롯 — AttackSlot 값에서 1을 빼면 skillSlots 인덱스
                int skillIndex = (int)slot - 1;
                if (skillSlots == null || skillIndex >= skillSlots.Length)
                {
                    Debug.LogWarning($"[PlayerController] skillSlots[{skillIndex}]({slot})가 할당되지 않았습니다.");
                    return null;
                }
                return skillSlots[skillIndex];
            }
        }

        /// <summary>
        /// 히트 판정 발생 시 AttackStateBehaviour에서 호출됩니다.
        /// </summary>
        /// <param name="damage">CurrentAttackData에서 전달된 데미지 값</param>
        public void OnAttackHit(int damage = 0)
        {
            // hitRadius 반경 내 Monster 태그 콜라이더를 탐색해 IDamageable에 데미지 적용
            int count = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, monsterHitBuffer);
            for (int i = 0; i < count; i++)
            {
                if (!monsterHitBuffer[i].CompareTag(monsterTag)) continue;

                if (monsterHitBuffer[i].TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage);
            }
        }

        /// <summary>
        /// 플레이어 상태를 전환하고, 진입 상태에 따른 애니메이터 및 이벤트 처리를 수행합니다.
        /// 동일 상태로의 전환은 무시합니다.
        /// </summary>
        /// <param name="next">전환할 상태</param>
        private void TransitionTo(PlayerState next)
        {
            if (playerState == next) return;

            PlayerState prev = playerState;
            playerState = next;

            switch (next)
            {
                case PlayerState.Idle:
                    animator.SetBool(AnimParam.IsWalking, false);
                    if (prev == PlayerState.Moving) OnMoveStateChanged?.Invoke(false);
                    if (prev == PlayerState.Attacking) OnAttackStateChanged?.Invoke(false);
                    break;

                case PlayerState.Moving:
                    animator.SetBool(AnimParam.IsWalking, true);
                    if (prev == PlayerState.Idle) OnMoveStateChanged?.Invoke(true);
                    break;

                case PlayerState.Attacking:
                    animator.SetBool(AnimParam.IsWalking, false);
                    // Trigger 방식 — CurrentAttackData의 animatorTrigger로 해당 공격 스테이트 진입
                    if (CurrentAttackData != null && !string.IsNullOrEmpty(CurrentAttackData.animatorTrigger))
                        animator.SetTrigger(CurrentAttackData.animatorTrigger);
                    // Moving 상태에서 공격으로 전환된 경우에만 이동 중단 이벤트 발생
                    if (prev == PlayerState.Moving) OnMoveStateChanged?.Invoke(false);
                    OnAttackStateChanged?.Invoke(true);
                    break;

                case PlayerState.Dead:
                    animator.SetBool(AnimParam.IsWalking, false);
                    animator.SetBool(AnimParam.IsDead, true);
                    if (prev == PlayerState.Moving) OnMoveStateChanged?.Invoke(false);
                    if (prev == PlayerState.Attacking) OnAttackStateChanged?.Invoke(false);
                    OnPlayerDead?.Invoke();
                    break;
            }
        }

        private Coroutine resumeMovementCoroutine;

        /// <summary>
        /// 공격 애니메이션 끝에서 호출됩니다.
        /// 중복 호출 및 코루틴 누적을 방지하기 위해 Attacking 상태일 때만 처리한다.
        /// </summary>
        public void OnAttackEnd()
        {
            if (!IsAttacking) return;
            if (resumeMovementCoroutine != null)
                StopCoroutine(resumeMovementCoroutine);
            resumeMovementCoroutine = StartCoroutine(ResumeMovement());
        }

        private IEnumerator ResumeMovement()
        {
            yield return new WaitForSeconds(moveResumeDelay);
            // 사망 상태로 전환된 경우 Idle 복귀 차단
            if (!IsDead)
                TransitionTo(PlayerState.Idle);
        }
    }
}
