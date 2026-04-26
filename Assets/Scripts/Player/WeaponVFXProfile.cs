using UnityEngine;

namespace G1
{
    /// <summary>
    /// 무기 타입별 슬래시 VFX 설정을 담는 ScriptableObject.
    /// Assets/에 에셋 파일로 저장되므로 코드 변경과 무관하게 수치가 유지됩니다.
    /// 메뉴: Create > G1 > Weapon VFX Profile
    /// </summary>
    [CreateAssetMenu(menuName = "G1/Weapon VFX Profile", fileName = "WeaponVFXProfile")]
    public class WeaponVFXProfile : ScriptableObject
    {
        /// <summary>
        /// WeaponType별 VFX 수치 설정 엔트리.
        /// ParticleSystem은 씬/프리팹 오브젝트 참조이므로 여기서는 수치만 보관하고,
        /// 파티클 참조는 PlayerWeaponVFXController 인스펙터에서 별도 관리합니다.
        /// </summary>
        [System.Serializable]
        public struct Entry
        {
            /// <summary>이 엔트리가 대응하는 무기 타입</summary>
            public WeaponType weaponType;
            /// <summary>파티클 Start Size 배율 (1.0 = 원본 크기, 2.0 = 2배)</summary>
            public float effectSize;
            /// <summary>파티클 재생 지연 시간 (초)</summary>
            public float playDelay;
            /// <summary>오른손 본 기준 이펙트 발생 위치 보정 (캐릭터 로컬 right/up/forward)</summary>
            public Vector3 positionOffset;
            /// <summary>캐릭터 정면 기준 슬래시 회전값 (Euler)</summary>
            public Vector3 slashRotation;
        }

        /// <summary>무기 타입별 VFX 수치 목록 — 인스펙터에서 편집합니다.</summary>
        public Entry[] entries;

        /// <summary>
        /// weaponType에 대응하는 Entry를 반환합니다.
        /// 등록되지 않은 타입이면 found=false를 반환합니다.
        /// </summary>
        /// <param name="weaponType">조회할 무기 타입</param>
        /// <param name="entry">찾은 엔트리</param>
        /// <returns>엔트리 존재 여부</returns>
        public bool TryGetEntry(WeaponType weaponType, out Entry entry)
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e.weaponType == weaponType)
                    {
                        entry = e;
                        return true;
                    }
                }
            }
            entry = default;
            return false;
        }
    }
}
