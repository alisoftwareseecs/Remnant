using System;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// A slow, small swing around this object's origin. Put it on a pivot at the top of whatever hangs
    /// with the hanging parts as children. Other things (the rope creak) can read the current angle and
    /// be told when the swing turns round.
    /// </summary>
    public class HangingSway : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float degrees = 2f;
        [SerializeField, Min(0.5f)] private float periodSeconds = 5f;
        [Tooltip("Adds a little irregularity so the movement does not look mechanical.")]
        [SerializeField, Min(0f)] private float irregularityDegrees = 0.5f;

        private float phase;
        private float noiseSeed;
        private float lastAngle;
        private float lastDirection;

        /// <summary>Current swing angle in degrees (positive = anticlockwise).</summary>
        public float Angle { get; private set; }

        /// <summary>The largest angle the swing normally reaches.</summary>
        public float MaxDegrees => degrees + irregularityDegrees;

        /// <summary>Raised each time the swing reaches its end and turns back, where the rope would strain and creak.</summary>
        public event Action SwingReversed;

        private void Awake()
        {
            phase = UnityEngine.Random.value * Mathf.PI * 2f;
            noiseSeed = UnityEngine.Random.value * 50f;
        }

        private void Update()
        {
            float swing = Mathf.Sin(phase + Time.time * Mathf.PI * 2f / periodSeconds) * degrees;
            float noise = (Mathf.PerlinNoise(noiseSeed, Time.time * 0.4f) - 0.5f) * 2f * irregularityDegrees;
            Angle = swing + noise;
            transform.localRotation = Quaternion.Euler(0f, 0f, Angle);

            float direction = Mathf.Sign(Angle - lastAngle);
            if (Mathf.Abs(Angle - lastAngle) > 0.0001f)
            {
                if (lastDirection != 0f && direction != lastDirection) SwingReversed?.Invoke();
                lastDirection = direction;
            }

            lastAngle = Angle;
        }
    }
}
