using UnityEngine;

namespace Remnant.Core
{
    /// <summary>A named place the SceneTransition can put the player after loading a scene.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public const string DefaultId = "start";

        [SerializeField] private string id = DefaultId;

        public string Id => id;

        public static SpawnPoint Find(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId)) spawnId = DefaultId;

            foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if (point.id == spawnId) return point;
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.4f);
        }
    }
}
