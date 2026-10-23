using Mycelia.Domain.Sessions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mycelia.Game
{
    /// <summary>
    /// Reads the pointer (primary touch on device, mouse in the Editor) each frame and feeds it to a
    /// <see cref="ChainDrag"/> in board coordinates (ADR-0008). Reading actions with ReadValue and IsPressed
    /// allocates nothing. Until move playback exists, a committed move redraws the board at once (slice 4).
    /// </summary>
    public sealed class ChainInputController : MonoBehaviour
    {
        [SerializeField] InputActionReference point;
        [SerializeField] InputActionReference press;
        [SerializeField] ChainInputSettings settings;

        LevelSession session;
        BoardCamera boardCamera;
        BoardView boardView;
        ChainDrag drag;

        public void Init(LevelSession session, BoardCamera boardCamera, BoardView boardView, IChainFeedback feedback)
        {
            this.session = session;
            this.boardCamera = boardCamera;
            this.boardView = boardView;
            drag = new ChainDrag(session, feedback, settings.HitRadius, settings.SampleStep);
        }

        void OnEnable()
        {
            point.action.Enable();
            press.action.Enable();
        }

        void OnDisable()
        {
            point.action.Disable();
            press.action.Disable();
        }

        void Update()
        {
            // Not started, or the camera has no screen to fit yet.
            if (drag == null || boardCamera.Layout.PixelsPerCell <= 0)
                return;

            Vector2 boardPoint = boardCamera.Layout.ScreenToWorld(point.action.ReadValue<Vector2>());
            if (press.action.IsPressed())
            {
                if (drag.IsDragging)
                    drag.Move(boardPoint);
                else
                    drag.Press(boardPoint);
            }
            else if (drag.IsDragging && drag.Release() != null)
            {
                boardView.Snap(session.Board);
            }
        }
    }
}
