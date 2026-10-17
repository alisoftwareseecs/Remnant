using TMPro;
using UnityEngine;

namespace Remnant.UI
{
    /// <summary>
    /// Makes a TMP text look hand-written in chalk: every character gets a small fixed wobble (offset and tilt), a slow
    /// chalky shimmer and an uneven brightness. It can also wipe the text away from one side with an eraser
    /// (EraseFront in world x, EraseDirection +1 = the eraser moves right, -1 = left).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ChalkTextEffect : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float wobble = 1.5f;
        [SerializeField, Min(0f)] private float tiltDegrees = 2.2f;
        [SerializeField, Range(0f, 1f)] private float minBrightness = 0.7f;
        [SerializeField, Min(0f)] private float shimmer = 0.5f;
        [SerializeField, Min(0.02f)] private float shimmerInterval = 0.12f;
        [Tooltip("Optional tileable grain texture multiplied into the face of the letters (TMP shader Face Texture).")]
        [SerializeField] private Texture2D grainTexture;
        [Tooltip("Size of one grain texel in canvas units; the face texture is tiled across the text box accordingly.")]
        [SerializeField, Min(0.1f)] private float grainTexelSize = 1.6f;
        [SerializeField, Range(-1f, 1f)] private float faceDilate = 0.05f;
        [SerializeField, Range(0f, 1f)] private float edgeSoftness = 0.12f;
        [Tooltip("Distance (canvas units) over which erased characters fade out at the eraser's edge.")]
        [SerializeField, Min(1f)] private float eraseSoftness = 60f;

        private TMP_Text text;
        private float[] seeds = new float[0];

        public bool Erasing { get; private set; }
        public float EraseFront { get; private set; }
        public int EraseDirection { get; private set; } = 1;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            Material material = text.fontMaterial;
            if (material.HasProperty(ShaderUtilities.ID_FaceDilate)) material.SetFloat(ShaderUtilities.ID_FaceDilate, faceDilate);
            if (material.HasProperty(ShaderUtilities.ID_OutlineSoftness)) material.SetFloat(ShaderUtilities.ID_OutlineSoftness, edgeSoftness);
            if (grainTexture != null && material.HasProperty(ShaderUtilities.ID_FaceTex))
            {
                material.SetTexture(ShaderUtilities.ID_FaceTex, grainTexture);
                Vector2 box = text.rectTransform.rect.size;
                material.SetTextureScale(ShaderUtilities.ID_FaceTex, new Vector2(box.x / (grainTexture.width * grainTexelSize), box.y / (grainTexture.height * grainTexelSize)));
            }
        }

        /// <summary>Starts or moves the eraser: everything on the side the eraser has already passed disappears.</summary>
        public void SetEraser(float worldFront, int direction)
        {
            Erasing = true;
            EraseFront = worldFront;
            EraseDirection = direction >= 0 ? 1 : -1;
        }

        public void ClearEraser()
        {
            Erasing = false;
        }

        private void LateUpdate()
        {
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            int count = info.characterCount;
            if (count == 0) return;

            if (seeds.Length < count)
            {
                float[] grown = new float[count * 2];
                System.Array.Copy(seeds, grown, seeds.Length);
                for (int i = seeds.Length; i < grown.Length; i++) grown[i] = Random.value;
                seeds = grown;
            }

            int tick = Mathf.FloorToInt(Time.unscaledTime / shimmerInterval);
            float frontLocal = Erasing ? transform.InverseTransformPoint(new Vector3(EraseFront, transform.position.y, transform.position.z)).x : 0f;

            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                TMP_MeshInfo mesh = info.meshInfo[ch.materialReferenceIndex];
                Vector3[] verts = mesh.vertices;
                Color32[] colors = mesh.colors32;
                int v = ch.vertexIndex;

                float seed = seeds[i];
                float ox = (Hash(seed, 1) - 0.5f) * 2f * wobble;
                float oy = (Hash(seed, 2) - 0.5f) * 2f * wobble;
                float tilt = (Hash(seed, 3) - 0.5f) * 2f * tiltDegrees;

                // slow chalky shimmer on top of the fixed wobble
                float sx = (Hash(seed + tick, 4) - 0.5f) * shimmer;
                float sy = (Hash(seed + tick, 5) - 0.5f) * shimmer;

                Vector3 centre = (verts[v] + verts[v + 2]) * 0.5f;
                Quaternion rot = Quaternion.Euler(0f, 0f, tilt);
                Vector3 offset = new Vector3(ox + sx, oy + sy, 0f);
                for (int k = 0; k < 4; k++) verts[v + k] = centre + rot * (verts[v + k] - centre) + offset;

                float brightness = Mathf.Lerp(minBrightness, 1f, Hash(seed, 6));
                float alpha = 1f;
                if (Erasing)
                {
                    float passed = EraseDirection > 0 ? frontLocal - centre.x : centre.x - frontLocal;
                    // fully visible until the eraser is within eraseSoftness, gone once it has passed
                    alpha = passed >= 0f ? 0f : Mathf.Clamp01(-passed / eraseSoftness);
                }

                for (int k = 0; k < 4; k++)
                {
                    Color32 c = colors[v + k];
                    colors[v + k] = new Color32((byte)(c.r * brightness), (byte)(c.g * brightness), (byte)(c.b * brightness), (byte)(c.a * alpha));
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private static float Hash(float seed, int salt)
        {
            float x = Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
