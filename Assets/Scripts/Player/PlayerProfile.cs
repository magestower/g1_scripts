using UnityEngine;

namespace G1
{
    /// <summary>
    /// 플레이어 표시용 프로필 데이터를 정의하는 ScriptableObject.
    /// 이름, 초상화 등 UI에 표시되는 정보를 보관한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerProfile", menuName = "G1/PlayerProfile")]
    public class PlayerProfile : ScriptableObject
    {
        [Header("프로필")]
        /// <summary>캐릭터 이름</summary>
        public string playerName;

        /// <summary>캐릭터 초상화 이미지</summary>
        public Sprite portrait;

        [TextArea(3, 5)]
        /// <summary>캐릭터 설명</summary>
        public string description;
    }
}
