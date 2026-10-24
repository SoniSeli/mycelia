using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Fills its camera's view with the deep blue to turquoise gradient and slowly drifting spores (design §8.3, Milestone 4
    /// slice 1). It sits under the scene's camera, so it follows the board camera, and refits when the camera's size or
    /// aspect changes: the board camera fits itself to the level after the scene loads.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour, IPausable
    {
        public const int GradientSortingOrder = -100;
        public const int SporeSortingOrder = -90;

        /// <summary>The camera size the spores' sizes, speeds and emitter are authored for; they scale with the view.</summary>
        public const float ReferenceSize = 5f;

        /// <summary>
        /// Rows blended in sRGB like the reference image; a two-row quad would blend in linear light and lighten the middle.
        /// The scene fade uses the same rows (<see cref="GradientGraphic"/>).
        /// </summary>
        public const int GradientRows = 9;

        // A little past the view's edges, so no edge shows through.
        const float Overscan = 1.02f;

        [SerializeField] Camera viewCamera;
        [SerializeField] MeshFilter gradient;
        [SerializeField] ParticleSystem spores;
        [SerializeField] Color top = new Color(0.024f, 0.212f, 0.584f);
        [SerializeField] Color bottom = new Color(0.259f, 0.729f, 0.745f);

        Mesh mesh;
        Vector3[] vertices;
        float fittedSize = -1;
        float fittedAspect = -1;

        void LateUpdate() => FitIfChanged();

        public void FitIfChanged()
        {
            if (viewCamera.orthographicSize == fittedSize && viewCamera.aspect == fittedAspect)
                return;

            fittedSize = viewCamera.orthographicSize;
            fittedAspect = viewCamera.aspect;
            if (mesh == null)
                Build();

            float halfHeight = fittedSize * Overscan;
            float halfWidth = fittedSize * fittedAspect * Overscan;
            for (int row = 0; row < GradientRows; row++)
            {
                float y = Mathf.Lerp(-halfHeight, halfHeight, row / (GradientRows - 1f));
                vertices[row * 2] = new Vector3(-halfWidth, y, 0);
                vertices[row * 2 + 1] = new Vector3(halfWidth, y, 0);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            float scale = fittedSize / ReferenceSize;
            spores.transform.localScale = new Vector3(scale, scale, 1);
            ParticleSystem.ShapeModule shape = spores.shape;
            shape.scale = new Vector3(2 * halfWidth / scale, 2 * halfHeight / scale, shape.scale.z);
        }

        public void SetPaused(bool paused)
        {
            if (paused)
                spores.Pause();
            else
                spores.Play();
        }

        void Build()
        {
            vertices = new Vector3[GradientRows * 2];
            var colours = new Color[GradientRows * 2];
            var triangles = new int[(GradientRows - 1) * 6];
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int row = 0; row < GradientRows; row++)
            {
                Color colour = Color.Lerp(bottom, top, row / (GradientRows - 1f));
                colours[row * 2] = colours[row * 2 + 1] = linear ? colour.linear : colour;
                if (row == GradientRows - 1)
                    continue;
                int at = row * 6, v = row * 2;
                triangles[at] = v;
                triangles[at + 1] = v + 2;
                triangles[at + 2] = v + 1;
                triangles[at + 3] = v + 1;
                triangles[at + 4] = v + 2;
                triangles[at + 5] = v + 3;
            }

            mesh = new Mesh { name = "Backdrop Gradient", hideFlags = HideFlags.DontSave };
            // Every vertex samples the spore texture's opaque white core, so the gradient and the spores need one material.
            // (Measured on the phone: sharing it doesn't change SetPass; particle systems don't use the SRP Batcher.)
            var uvs = new Vector2[GradientRows * 2];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = new Vector2(0.5f, 0.5f);

            mesh.vertices = vertices;
            mesh.colors = colours;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            gradient.sharedMesh = mesh;
            gradient.GetComponent<MeshRenderer>().sortingOrder = GradientSortingOrder;
            spores.GetComponent<ParticleSystemRenderer>().sortingOrder = SporeSortingOrder;
        }

        void OnDestroy()
        {
            if (mesh == null)
                return;
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
