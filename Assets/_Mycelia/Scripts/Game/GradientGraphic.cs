using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// A UI rectangle filled with the backdrop's vertical gradient, in the same rows blended the same way
    /// (<see cref="Backdrop.GradientRows"/>), so the scene fade looks like the background with the screen's content gone
    /// (design §8.3 slice 2). The rows carry sRGB colours, which the UI treats like any other UI colour.
    /// </summary>
    public sealed class GradientGraphic : MaskableGraphic
    {
        [SerializeField] Color top = new Color(0.024f, 0.212f, 0.584f);
        [SerializeField] Color bottom = new Color(0.259f, 0.729f, 0.745f);

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            int rows = Backdrop.GradientRows;
            for (int row = 0; row < rows; row++)
            {
                float t = row / (rows - 1f);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                Color32 colour = Color.Lerp(bottom, top, t) * color;
                helper.AddVert(new Vector3(rect.xMin, y), colour, Vector2.zero);
                helper.AddVert(new Vector3(rect.xMax, y), colour, Vector2.zero);
                if (row == 0)
                    continue;
                int v = row * 2;
                helper.AddTriangle(v - 2, v, v - 1);
                helper.AddTriangle(v - 1, v, v + 1);
            }
        }
    }
}
