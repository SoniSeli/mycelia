using Mycelia.Content;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// The chain demo on "How to Grow" (design §8.3 slice 8): the game's own spores and chain line, drawn in the world
    /// like the board, where a finger draws each example chain in turn (<see cref="ChainDemoTimeline"/>). It fits the
    /// space its page leaves for it, and runs only while shown. Playing it allocates nothing.
    /// </summary>
    public sealed class ChainDemo : MonoBehaviour
    {
        [SerializeField, Tooltip("Placed one unit apart, like board cells.")]
        SporeView[] spores = new SporeView[0];
        [SerializeField, Tooltip("Each spore's type, as a position in the spore catalog.")]
        int[] sporeTypes = new int[0];
        [SerializeField, Tooltip("Spore positions along each example chain, one chain after another.")]
        int[] chains = new int[0];
        [SerializeField, Tooltip("How many spores each example chain has.")]
        int[] chainLengths = new int[0];
        [SerializeField] LineRenderer line;
        [SerializeField, Tooltip("The touch point gliding along the chain.")]
        SpriteRenderer finger;
        [SerializeField, Tooltip("The space on its page the demo fills.")]
        RectTransform area;
        [SerializeField, Tooltip("The camera drawing the demo.")]
        Camera view;
        [SerializeField, Tooltip("The demo's size in its own units, spores included.")]
        Vector2 size = new Vector2(6f, 3f);
        [SerializeField] ChainDemoTiming timing = ChainDemoTiming.Default;

        readonly Vector3[] corners = new Vector3[4];
        Canvas canvas;
        float lineWidth;
        float startTime;
        Rect fitted;

        public void Init(SporeCatalog catalog)
        {
            for (int i = 0; i < spores.Length; i++)
            {
                spores[i].Prepare();
                spores[i].Show(catalog.Types[sporeTypes[i]], spores[i].transform.localPosition);
            }

            line.useWorldSpace = false;
            line.sortingOrder = ChainView.SortingOrder;
            line.positionCount = 0;
            lineWidth = line.widthMultiplier;
            finger.sortingOrder = SporeView.SortingOrder + 1;
            finger.enabled = false;
            canvas = area.GetComponentInParent<Canvas>(true);
        }

        /// <summary>The largest uniform scale that fits <paramref name="content"/> in <paramref name="area"/>, centred.</summary>
        public static (Vector3 Centre, float Scale) Fit(Rect area, Vector2 content) =>
            (area.center, Mathf.Min(area.width / content.x, area.height / content.y));

        /// <summary>Shows the demo <paramref name="seconds"/> after it started; Update calls it, and tests.</summary>
        public void ShowAt(float seconds)
        {
            ChainDemoFrame frame = ChainDemoTimeline.At(seconds, chainLengths, timing);
            for (int i = 0; i < spores.Length; i++)
                spores[i].transform.localScale = Vector3.one;

            int first = 0;
            for (int e = 0; e < frame.Example; e++)
                first += chainLengths[e];
            var chainedScale = new Vector3(frame.ChainScale, frame.ChainScale, 1f);
            for (int k = 0; k < frame.Links; k++)
                spores[chains[first + k]].transform.localScale = chainedScale;

            line.positionCount = frame.LineShown ? frame.Links : 0;
            if (frame.LineShown)
            {
                // A chain is one type, so its first spore gives the line its colour.
                line.startColor = line.endColor = spores[chains[first]].Renderer.color;
                for (int k = 0; k < frame.Links; k++)
                    line.SetPosition(k, spores[chains[first + k]].transform.localPosition);
            }

            finger.enabled = frame.FingerShown;
            if (frame.FingerShown)
            {
                int last = chainLengths[frame.Example] - 1;
                Vector3 from = spores[chains[first + frame.FingerFrom]].transform.localPosition;
                Vector3 to = spores[chains[first + Mathf.Min(frame.FingerFrom + 1, last)]].transform.localPosition;
                finger.transform.localPosition = Vector3.Lerp(from, to, frame.FingerLerp);
            }
        }

        // Each time the page shows, the demo starts from its first example.
        void OnEnable()
        {
            startTime = Time.unscaledTime;
            fitted = default;
        }

        void Update() => ShowAt(Time.unscaledTime - startTime);

        // After the canvas has laid out (the safe area can change), the demo follows its space on the page.
        void LateUpdate()
        {
            if (canvas == null)
                return;

            Camera uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            area.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            Rect screen = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            if (screen == fitted)
                return;

            fitted = screen;
            float depth = transform.position.z - view.transform.position.z;
            Vector3 worldMin = view.ScreenToWorldPoint(new Vector3(min.x, min.y, depth));
            Vector3 worldMax = view.ScreenToWorldPoint(new Vector3(max.x, max.y, depth));
            (Vector3 centre, float scale) = Fit(Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y), size);
            transform.position = new Vector3(centre.x, centre.y, transform.position.z);
            transform.localScale = new Vector3(scale, scale, 1f);
            // A line's width is in world units whatever its transform's scale.
            line.widthMultiplier = lineWidth * scale;
        }

        /// <summary>Fits the demo to its space now; the screen renders without a frame loop.</summary>
        public void FitNow() => LateUpdate();
    }
}
