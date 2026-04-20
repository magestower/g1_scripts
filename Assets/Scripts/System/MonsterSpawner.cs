using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 지정된 위치에 몬스터를 스폰하고 MonsterManager에 등록한다.
    /// MonsterPool을 통해 오브젝트를 재사용하며,
    /// 이 스포너가 관리하는 생존 몬스터 수가 maxAliveCount 미만이고
    /// 마지막 스폰으로부터 respawnDelay가 경과하면 자동 재스폰한다.
    /// 씬에 스폰 포인트당 하나씩 배치한다.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        /// <summary>스폰할 몬스터 프리팹 (MonsterBase를 가진 루트 오브젝트)</summary>
        [SerializeField] private MonsterBase monsterPrefab;

        /// <summary>씬 시작 시 자동 스폰 여부</summary>
        [SerializeField] private bool spawnOnStart = true;

        /// <summary>풀 초기 생성 수. 0이면 MonsterPool의 defaultInitialSize 사용.</summary>
        [SerializeField] private int poolPrewarmSize = 0;

        /// <summary>이 스포너가 유지할 최대 생존 몬스터 수. 이 수 미만이면 재스폰을 시도한다.</summary>
        [SerializeField] private int maxAliveCount = 3;

        /// <summary>마지막 스폰 후 다음 스폰까지 대기 시간 (초).</summary>
        [SerializeField] private float respawnDelay = 5f;

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        /// <summary>이 스포너가 현재 관리 중인 생존 몬스터 목록</summary>
        private readonly List<MonsterBase> ownedMonsters = new();

        /// <summary>마지막으로 Spawn()을 호출한 시각</summary>
        private float lastSpawnTime = float.MinValue;

        /// <summary>스폰 조건 체크 누적 시간</summary>
        private float checkTimer;

        /// <summary>스폰 조건 체크 주기 (초)</summary>
        private const float CheckInterval = 1f;

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>풀을 미리 준비하고, spawnOnStart가 true이면 즉시 스폰한다.</summary>
        private void Start()
        {
            if (monsterPrefab == null)
            {
                Debug.LogWarning("[MonsterSpawner] monsterPrefab이 할당되지 않았습니다.", this);
                return;
            }

            if (MonsterPool.Instance != null)
                MonsterPool.Instance.Prewarm(monsterPrefab, poolPrewarmSize);

            MonsterBase.OnMonsterDied += OnManagedMonsterDied;

            if (spawnOnStart)
                SpawnBatch();
        }

        /// <summary>스포너 파괴 시 이벤트 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            MonsterBase.OnMonsterDied -= OnManagedMonsterDied;
        }

        /// <summary>CheckInterval 주기로 스폰 조건을 검사해 필요 시 스폰한다.</summary>
        private void Update()
        {
            if (monsterPrefab == null) return;

            checkTimer += Time.deltaTime;
            if (checkTimer < CheckInterval) return;
            checkTimer = 0f;

            TryRespawn();
        }

        // ─────────────────────────────────────────
        // public 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 풀에서 몬스터를 꺼내 이 스포너의 위치/회전으로 활성화하고 MonsterManager에 등록한다.
        /// 스폰된 몬스터는 ownedMonsters 목록에 추가되어 이 스포너가 추적한다.
        /// </summary>
        /// <returns>활성화된 MonsterBase 인스턴스. 프리팹 미할당 시 null.</returns>
        public MonsterBase Spawn()
        {
            if (monsterPrefab == null) return null;

            MonsterBase monster;

            if (MonsterPool.Instance != null)
                monster = MonsterPool.Instance.Get(monsterPrefab, transform.position, transform.rotation);
            else
            {
                // 풀 미사용 환경 폴백 — PrefabID를 설정해 Release() 시 잘못된 풀에 반납되지 않도록 보장
                monster = Instantiate(monsterPrefab, transform.position, transform.rotation);
                monster.PrefabID = monsterPrefab.GetEntityId();
                monster.ResetState();
                Debug.LogWarning("[MonsterSpawner] MonsterPool 인스턴스를 찾을 수 없어 Instantiate로 대체합니다.", this);
            }

            if (MonsterManager.Instance != null)
                MonsterManager.Instance.Register(monster);
            else
                Debug.LogWarning("[MonsterSpawner] MonsterManager 인스턴스를 찾을 수 없습니다.", this);

            ownedMonsters.Add(monster);
            lastSpawnTime = Time.time;
            return monster;
        }

        // ─────────────────────────────────────────
        // private 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 시작 시 maxAliveCount 만큼 즉시 스폰한다. respawnDelay 없이 일괄 생성.
        /// </summary>
        private void SpawnBatch()
        {
            for (int i = 0; i < maxAliveCount; i++)
                Spawn();
        }

        /// <summary>
        /// 이 스포너가 관리하는 생존 몬스터 수가 maxAliveCount 미만이고
        /// respawnDelay가 경과했으면 1마리 스폰한다.
        /// </summary>
        private void TryRespawn()
        {
            if (ownedMonsters.Count >= maxAliveCount) return;
            if (Time.time - lastSpawnTime < respawnDelay) return;

            Spawn();
        }

        /// <summary>
        /// 관리 중인 몬스터가 사망하면 ownedMonsters 목록에서 제거한다.
        /// </summary>
        /// <param name="monster">사망한 몬스터</param>
        private void OnManagedMonsterDied(MonsterBase monster)
        {
            ownedMonsters.Remove(monster);
        }

#if UNITY_EDITOR
        // ─────────────────────────────────────────
        // 에디터 기즈모 (스폰 위치 시각화)
        // ─────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 1f);
        }
#endif
    }
}
