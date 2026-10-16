using Remnant.Player;
using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// A trigger volume that saves a restart point (scene, this object's position, current flags) when the
    /// player walks into it. Place the object on the floor where the player should reappear.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;

            GameState state = GameState.Instance;
            if (state == null) return;

            state.SaveCheckpoint(gameObject.scene.name, transform.position);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
    }
}
