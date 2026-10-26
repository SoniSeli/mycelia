using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// The intro (design §8.3 slice 8): welcome, how to play and the SDGs, one page at a time under one round Next
    /// button. The chain demo draws in the world behind the canvas, so it shows and hides with its page from here.
    /// </summary>
    public sealed class IntroView : MonoBehaviour
    {
        [SerializeField, Tooltip("In reading order.")]
        GameObject[] pages;
        [SerializeField] Button next;
        [SerializeField, Tooltip("The world-space chain demo, shown only with its page.")]
        GameObject demo;
        [SerializeField, Min(0), Tooltip("The page the demo belongs to (0-based).")]
        int demoPage = 1;

        IntroPages flow;
        Action onFinished;
        Action onLeft;

        /// <param name="onFinished">Next on the last page.</param>
        /// <param name="onLeft">Back on the first page.</param>
        public void Init(Action onFinished, Action onLeft)
        {
            this.onFinished = onFinished;
            this.onLeft = onLeft;
            flow = new IntroPages(pages.Length);
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(Next);
            Show();
        }

        /// <summary>What a press of back does; the scene's back button calls it.</summary>
        public void Back()
        {
            if (flow.Back() == IntroStep.Left)
                onLeft();
            else
                Show();
        }

        void Next()
        {
            if (flow.Next() == IntroStep.Finished)
                onFinished();
            else
                Show();
        }

        void Show()
        {
            for (int i = 0; i < pages.Length; i++)
                pages[i].SetActive(i == flow.Current);
            demo.SetActive(flow.Current == demoPage);
        }
    }
}
