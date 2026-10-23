using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mycelia.Game
{
    /// <summary>
    /// A fixed set of spore views, all created when the scene starts (ADR-0011). The pool never grows: running out
    /// is a bug, so <see cref="Rent"/> fails loudly instead of creating a view during play.
    /// </summary>
    public sealed class SporeViewPool
    {
        readonly Stack<SporeView> available;

        public SporeViewPool(SporeView prefab, Transform parent, int capacity)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The pool needs at least one view.");

            Capacity = capacity;
            available = new Stack<SporeView>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                SporeView view = Object.Instantiate(prefab, parent);
                view.name = $"Spore {i:00}";
                view.Prepare();
                view.InPool = true;
                view.gameObject.SetActive(false);
                available.Push(view);
            }
        }

        public int Capacity { get; }
        public int Available => available.Count;

        public SporeView Rent()
        {
            if (available.Count == 0)
                throw new InvalidOperationException($"All {Capacity} spore views are in use; the pool never grows during play.");

            SporeView view = available.Pop();
            view.InPool = false;
            view.gameObject.SetActive(true);
            return view;
        }

        public void Return(SporeView view)
        {
            if (view.InPool)
                throw new InvalidOperationException($"{view.name} is already back in the pool.");

            view.InPool = true;
            view.gameObject.SetActive(false);
            available.Push(view);
        }
    }
}
