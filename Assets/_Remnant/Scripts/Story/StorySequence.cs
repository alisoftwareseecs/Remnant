using UnityEngine;

namespace Remnant.Story
{
    /// <summary>A scripted multi-step story moment that an interaction can start.</summary>
    public abstract class StorySequence : MonoBehaviour
    {
        public abstract void Run();
    }
}
