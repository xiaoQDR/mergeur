using System;
using System.Collections.Generic;
using Rive.Components;
using UnityEngine;

namespace Mergeur.Core
{
    /// <summary>
    /// UI portal for looking up, showing and hiding registered views.
    /// It deliberately contains no Rive navigation or screen-specific business rules.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Serializable]
        private sealed class ViewRegistration
        {
            [SerializeField] private string id;
            [SerializeField] private RiveWidget widget;
            [SerializeField] private GameObject root;
            [SerializeField] private bool isInitial;

            public string Id => id;
            public RiveWidget Widget => widget;
            public GameObject Root => root != null ? root : widget != null ? widget.gameObject : null;
            public bool IsInitial => isInitial;
        }

        [SerializeField] private List<ViewRegistration> views = new List<ViewRegistration>();

        private readonly Dictionary<string, ViewRegistration> viewById =
            new Dictionary<string, ViewRegistration>(StringComparer.OrdinalIgnoreCase);

        private readonly List<RiveWidget> widgets = new List<RiveWidget>();
        private bool initialized;

        public IReadOnlyList<RiveWidget> Widgets => widgets;

        public event Action<string, bool> VisibilityChanged;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            viewById.Clear();
            widgets.Clear();

            ViewRegistration fallbackInitialView = null;
            var hasInitialView = false;
            foreach (var view in views)
            {
                if (view == null || view.Widget == null || string.IsNullOrWhiteSpace(view.Id))
                {
                    continue;
                }

                var viewId = view.Id.Trim();
                if (!viewById.TryAdd(viewId, view))
                {
                    continue;
                }

                widgets.Add(view.Widget);
                fallbackInitialView ??= view;
                hasInitialView |= view.IsInitial;
            }

            foreach (var view in viewById.Values)
            {
                var root = view.Root;
                if (root != null)
                {
                    root.SetActive(view.IsInitial || !hasInitialView && ReferenceEquals(view, fallbackInitialView));
                }
            }
        }

        public bool Contains(string viewId)
        {
            Initialize();
            return !string.IsNullOrWhiteSpace(viewId) && viewById.ContainsKey(viewId.Trim());
        }

        public RiveWidget GetWidget(string viewId)
        {
            Initialize();
            return !string.IsNullOrWhiteSpace(viewId) && viewById.TryGetValue(viewId.Trim(), out var view)
                ? view.Widget
                : null;
        }

        public bool IsVisible(string viewId)
        {
            Initialize();
            return !string.IsNullOrWhiteSpace(viewId) &&
                   viewById.TryGetValue(viewId.Trim(), out var view) &&
                   view.Root != null &&
                   view.Root.activeSelf;
        }

        public bool Show(string viewId)
        {
            Initialize();
            if (string.IsNullOrWhiteSpace(viewId) || !viewById.TryGetValue(viewId.Trim(), out var target))
            {
                return false;
            }

            var root = target.Root;
            if (root == null)
            {
                return false;
            }

            if (root.activeSelf)
            {
                return true;
            }

            root.SetActive(true);
            VisibilityChanged?.Invoke(target.Id.Trim(), true);
            return true;
        }

        public bool Hide(string viewId)
        {
            Initialize();
            if (string.IsNullOrWhiteSpace(viewId) || !viewById.TryGetValue(viewId.Trim(), out var view))
            {
                return false;
            }

            var root = view.Root;
            if (root == null)
            {
                return false;
            }

            if (!root.activeSelf)
            {
                return true;
            }

            root.SetActive(false);
            VisibilityChanged?.Invoke(view.Id.Trim(), false);
            return true;
        }

    }
}
