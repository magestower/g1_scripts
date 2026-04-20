using UnityEngine;

namespace G1
{
	public static class G
	{
		/// <summary>
		/// 자식 오브젝트를 재귀적으로 검색합니다. (대소문자 구분 없음 - Case Insensitive)
		/// </summary>
		/// <param name="parent">검색 시작할 부모 Transform</param>
		/// <param name="name">찾고 싶은 오브젝트 이름 (대소문자 무시)</param>
		/// <returns>찾으면 해당 Transform, 못 찾으면 null</returns>
		public static Transform FindDeep(this Transform parent, string name)
		{
			if (parent == null || string.IsNullOrEmpty(name))
				return null;

			// 현재 오브젝트 이름 비교 (대소문자 무시)
			if (string.Equals(parent.name, name, System.StringComparison.OrdinalIgnoreCase))
				return parent;

			// 모든 자식을 재귀적으로 검색
			foreach (Transform child in parent)
			{
				Transform result = child.FindDeep(name);
				if (result != null)
					return result;
			}

			return null;
		}

		/// <summary>
		/// FindDeep의 GameObject 버전 (편의 메서드)
		/// </summary>
		public static GameObject FindDeepGameObject(this Transform parent, string name)
		{
			Transform t = parent.FindDeep(name);
			return t != null ? t.gameObject : null;
		}
	}
}

