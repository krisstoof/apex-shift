using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class IsometricPlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;

        private void Update()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 movement = new Vector3(horizontal, 0f, vertical);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            transform.position += movement * (moveSpeed * Time.deltaTime);
        }
    }
}

