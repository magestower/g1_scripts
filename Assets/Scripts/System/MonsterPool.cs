using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 프리팹별 MonsterBase 오브젝트 풀을 관리하는 싱글톤.
    /// MonsterSpawner가 Get()으로 꺼내고, MonsterBase가 Release()로 반납한다.
    /// </summary>
    public class MonsterPool : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // 싱글톤
        // ─────────────────────────────────────────
        public static MonsterPool Instance { get; private set; }

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        /// <summary>풀 초기 크기 — 씬 시작 시 프리팹별로 이 수만큼 미리 생성한다.</summary>
        [SerializeField] private int defaultInitialSize = 3;

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        /// <summary>프리팹 EntityId → 비활성 오브젝트 스택</summary>
        private readonly Dictionary<EntityId, Stack<MonsterBase>> pools = new();

        /// <summary>프리팹 EntityId → 원본 프리팹 참조 (풀 소진 시 추가 생성에 사용)</summary>
        private readonly Dictionary<EntityId, MonsterBase> prefabMap = new();

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>싱글톤 인스턴스를 설정한다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ─────────────────────────────────────────
        // public 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 프리팹에 대한 풀을 미리 생성한다. MonsterSpawner의 Start에서 호출하면 된다.
        /// 이미 등록된 프리팹이면 무시한다.
        /// </summary>
        /// <param name="prefab">풀링할 몬스터 프리팹</param>
        /// <param name="initialSize">초기 생성 수. 0이면 defaultInitialSize 사용.</param>
        public void Prewarm(MonsterBase prefab, int initialSize = 0)
        {
            EntityId id = prefab.GetEntityId();
            if (pools.ContainsKey(id)) return;

            pools[id]     = new Stack<MonsterBase>();
            prefabMap[id] = prefab;

            int count = initialSize > 0 ? initialSize : defaultInitialSize;
            for (int i = 0; i < count; i++)
                pools[id].Push(CreateInstance(prefab));
        }

        /// <summary>
        /// 풀에서 몬스터 인스턴스를 꺼내 지정 위치/회전으로 활성화한다.
        /// 풀이 비어있으면 새로 생성한다.
        /// </summary>
        /// <param name="prefab">꺼낼 프리팹</param>
        /// <param name="position">스폰 위치</param>
        /// <param name="rotation">스폰 회전</param>
        /// <returns>활성화된 MonsterBase 인스턴스</returns>
        public MonsterBase Get(MonsterBase prefab, Vector3 position, Quaternion rotation)
        {
            EntityId id = prefab.GetEntityId();

            // 풀이 없으면 즉시 등록
            if (!pools.ContainsKey(id))
                Prewarm(prefab, 0);

            MonsterBase monster = pools[id].Count > 0
                ? pools[id].Pop()
                : CreateInstance(prefab);

            monster.PrefabID = prefab.GetEntityId(); // 풀 재사용 시에도 ID 보장
            monster.transform.SetPositionAndRotation(position, rotation);
            monster.gameObject.SetActive(true);
            monster.ResetState();
            return monster;
        }

        /// <summary>
        /// 몬스터를 비활성화해 풀에 반납한다. MonsterBase.Die()에서 지연 호출된다.
        /// </summary>
        /// <param name="monster">반납할 인스턴스</param>
        public void Release(MonsterBase monster)
        {
            EntityId id = monster.PrefabID;

            if (!pools.ContainsKey(id))
                pools[id] = new Stack<MonsterBase>();

            monster.gameObject.SetActive(false);
            pools[id].Push(monster);
        }

        // ─────────────────────────────────────────
        // private 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 프리팹을 인스턴스화한다.
        /// SetActive(true) → Awake() 실행 → SetActive(false) 순서로 호출해
        /// 컴포넌트 참조가 Awake()에서 반드시 캐싱된 후 풀에 보관되도록 보장한다.
        /// </summary>
        private MonsterBase CreateInstance(MonsterBase prefab)
        {
            MonsterBase instance = Instantiate(prefab, transform);
            instance.PrefabID = prefab.GetEntityId();
            instance.gameObject.SetActive(true);  // Awake() 강제 실행
            instance.gameObject.SetActive(false); // 풀 보관용 비활성화
            return instance;
        }
    }
}
