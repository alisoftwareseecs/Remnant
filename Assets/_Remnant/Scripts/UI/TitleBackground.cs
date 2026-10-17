using Remnant.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Remnant.UI
{
    /// <summary>
    /// The classroom behind the title. Before the game has been completed it is night: almost dark, Aiko's desk empty.
    /// After the game has been completed (PlayerPrefs "remnant_completed") it is bright daylight and Aiko sits at her desk.
    /// </summary>
    public class TitleBackground : MonoBehaviour
    {
        [SerializeField] private Light2D globalLight;
        [SerializeField] private GameObject aiko;

        [Header("Night (not completed)")]
        [SerializeField, Min(0f)] private float nightIntensity = 0.1f;
        [SerializeField] private Color nightColor = new Color(0.45f, 0.55f, 0.85f);

        [Header("Day (completed)")]
        [SerializeField, Min(0f)] private float dayIntensity = 1f;
        [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.82f);

        private void Start()
        {
            bool completed = SaveSystem.Completed;

            if (globalLight != null)
            {
                globalLight.intensity = completed ? dayIntensity : nightIntensity;
                globalLight.color = completed ? dayColor : nightColor;
            }

            if (aiko != null) aiko.SetActive(completed);
        }
    }
}
