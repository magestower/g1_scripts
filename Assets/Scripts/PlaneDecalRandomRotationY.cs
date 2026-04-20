using UnityEngine;


namespace G1
{
    public class PlaneDecalRandomRotationY : MonoBehaviour
    {
        private void Awake()
        {
            float randomY = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, randomY, transform.eulerAngles.z);
        }
    }
}
