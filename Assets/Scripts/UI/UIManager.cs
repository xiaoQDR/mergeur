using System;
using System.Collections.Generic;
using Rive.Components;
using UnityEngine;

namespace Mergeur.Core
{
    /// <summary>
    /// UI portal for looking up, showing and hiding registered views.
    /// Rive widgets stay active after initialization so repeated navigation does not
    /// unregister/re-register native Rive render resources on Android.
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

        private sealed class RuntimeViewState
        {
            public Vector3 VisibleScale;
            public HitTestBehavior VisibleHitTestBehavior;
            public bool IsVisible;
        }

        [SerializeField] private List<ViewRegistration> views = new List<ViewRegistration>();

        private readonly Dictionary<string, ViewRegistration> viewById =
            new Dictionary<string, ViewRegistration>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, RuntimeViewState> runtimeStateById =
            new Dictionary<string, RuntimeViewState>(StringComparer.OrdinalIgnoreCase);

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
            runtimeStateById.Clear();
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

            // Important: keep every RiveWidget active for the whole scene lifetime.
            // Rive 0.4.x unregisters a widget from its panel in OnDisable and registers it
            // again in OnEnable. Repeating that lifecycle on Android can leave native render
            // state in a bad state, so routing only changes transform visibility and hit testing.
            foreach (var pair in viewById)
            {
                var viewId = pair.Key;
                var view = pair.Value;
                var root = view.Root;
                if (root == null)
                {
                    continue;
                }

                if (!root.activeSelf)
                {
                    root.SetActive(true);
                }

                runtimeStateById[viewId] = new RuntimeViewState
                {
                    VisibleScale = root.transform.localScale,
                    VisibleHitTestBehavior = view.Widget.HitTestBehavior,
                    IsVisible = false
                };
            }

            foreach (var pair in viewById)
            {
                var view = pair.Value;
                var shouldShow = view.IsInitial || !hasInitialView && ReferenceEquals(view, fallbackInitialView);
                SetVisibility(pair.Key, shouldShow, false);
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
                   runtimeStateById.TryGetValue(viewId.Trim(), out var state) &&
                   state.IsVisible;
        }

        public bool Show(string viewId)
        {
            Initialize();
            if (string.IsNullOrWhiteSpace(viewId))
            {
                return false;
            }

            return SetVisibility(viewId.Trim(), true, true);
        }

        public bool Hide(string viewId)
        {
            Initialize();
            if (string.IsNullOrWhiteSpace(viewId))
            {
                return false;
            }

            return SetVisibility(viewId.Trim(), false, true);
        }

        private bool SetVisibility(string viewId, bool visible, bool notify)
        {
            if (!viewById.TryGetValue(viewId, out var view) ||
                !runtimeStateById.TryGetValue(viewId, out var state))
            {
                return false;
            }

            var root = view.Root;
            if (root == null || view.Widget == null)
            {
                return false;
            }

            // Never deactivate a RiveWidget after initialization.
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            if (state.IsVisible == visible)
            {
                return true;
            }

            if (visible)
            {
                root.transform.localScale = state.VisibleScale;
                view.Widget.HitTestBehavior = state.VisibleHitTestBehavior;
                root.transform.SetAsLastSibling();
            }
            else
            {
                view.Widget.HitTestBehavior = HitTestBehavior.None;
                root.transform.localScale = Vector3.zero;
            }

            state.IsVisible = visible;

            if (notify)
            {
                VisibilityChanged?.Invoke(view.Id.Trim(), visible);
            }

            return true;
        }
    }
}
