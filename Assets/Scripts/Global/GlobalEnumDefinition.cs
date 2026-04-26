namespace G1
{
    /// <summary>
    /// 의상 슬롯 종류 — 캐릭터 신체 부위별 장착 위치를 정의합니다.
    /// </summary>
    public enum OutfitSlot
    {
        Top,        // 상의 (가슴, 몸통)
        Bottom,     // 하의 (다리, 바지)
        Head,       // 머리 (헤어 포함)
        Shoes,      // 신발
        Accessory   // 액세서리 (모자, 귀걸이 등)
    }

    /// <summary>
    /// 의상 장착 시 숨길 신체 부위를 비트 플래그로 정의합니다.
    /// 여러 부위를 동시에 지정할 수 있습니다. (Flags)
    /// </summary>
    [System.Flags]
    public enum BodyPartFlags
    {
        None  = 0,
        Torso = 1 << 0,  // 몸통
        Legs  = 1 << 1,  // 다리
        Head  = 1 << 2,  // 머리
        Shoes = 1 << 3   // 신발
    }

    /// <summary>
    /// 장비 슬롯 종류 — 손 본 위치 기반 장착 위치를 정의합니다.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon,       // 주무기 (오른손 RightHand 본)
        SubEquipment  // 보조장비 (왼손 LeftHand 본)
    }

    /// <summary>
    /// 공격 슬롯 종류 — Basic은 기본 공격, Skill1~은 스킬 슬롯을 의미합니다.
    /// OnAttackStart 호출 시 매직 넘버 대신 이 enum을 사용하세요.
    /// </summary>
    public enum AttackSlot
    {
        Basic  = 0,  // 기본 공격 (장착 무기 기반)
        Skill1 = 1,  // 스킬 슬롯 1
        Skill2 = 2,  // 스킬 슬롯 2
        Skill3 = 3,  // 스킬 슬롯 3
    }

    /// <summary>
    /// 무기 종류 — Unarmed(맨손)를 포함하며, PlayerController의 weaponAttackData 배열 인덱스와 순서가 일치해야 합니다.
    /// 새 무기 추가 시 반드시 Unarmed 다음에 순서대로 추가하고 weaponAttackData 배열도 함께 확장하세요.
    /// </summary>
    public enum WeaponType
    {
        Unarmed    = 0,  // 맨손 — 펀치 공격
        OneHanded  = 1,  // 한손 무기
        TwoHanded  = 2,  // 양손 무기
    }

    /// <summary>
    /// 공격 종류 — 방어/저항 계산 및 속성 효과 분기에 사용합니다.
    /// 새 종류 추가 시 저항 계산 로직도 함께 확장하세요.
    /// </summary>
    public enum AttackType
    {
        Physical  = 0,  // 물리 공격 — 방어력으로 감소
        Magic     = 1,  // 마법 공격 — 마법 저항으로 감소
        Fire      = 2,  // 화염 공격
        Electric  = 3,  // 전기 공격
        Poison    = 4,  // 독 공격
        Curse     = 5,  // 저주 공격
    }

    /// <summary>
    /// 데미지 유형 — 피격 연출(이펙트, 사운드 등) 분기에 사용합니다.
    /// </summary>
    public enum DamageType
    {
        Normal    = 0,  // 일반 피해 — 기본 연출
        Critical  = 1,  // 크리티컬 — 강조 연출
    }

    /// <summary>
    /// 공격 대상 범위 — Single은 가장 가까운 단일 대상, Area는 hitRadius 내 전체 대상에게 피해를 줍니다.
    /// </summary>
    public enum TargetType
    {
        Single = 0,  // 단일 대상 — 가장 가까운 몬스터 1명
        Area   = 1,  // 광역 대상 — hitRadius 내 모든 몬스터
    }

    /// <summary>
    // 아이템 희귀도 등급 — 아이템의 희귀도를 정의합니다.
    /// </summary>
    public enum Rarity
	{
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary
	}
}
