using Remnant.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Remnant.Cameras
{
    /// <summary>
    /// Lets the CameraRig prefab work in any scene: on start it points every child
    /// CinemachineCamera that has no Follow target at the scene's player.
    /// </summary>
    public class PlayerCameraTargets : MonoBehaviour
    {
        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("PlayerCameraTargets: no PlayerController in the scene.", this);
                return;
            }

            foreach (CinemachineCamera cam in GetComponentsInChildren<CinemachineCamera>(true))
            {
                if (cam.Follow == null) cam.Follow = player.transform;
            }
        }
    }
}
