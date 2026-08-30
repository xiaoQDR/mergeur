using System;
using Rive;
using Rive.Components;
using UnityEngine;
using VContainer.Unity;

namespace Mergeur.Core
{
    /// <summary>
    /// Converts Rive state-machine events into UI navigation requests.
    /// </summary>
    public sealed class RiveRouteService : IStartable, ITickable, IDisposable
    {
        private const string LogoViewId = "logo";
        private const string InitialRouteViewId = "home";
        private const string GameViewId = "game";
        private const string LogoFadeOutEvent = "fadeOut";
        private const string HomeMainButtonTrigger = "mainBtnTrigget";

        private static readonly string[] RoutePrefixes =
        {
            "route:", "route/", "navigate:", "navigate/"
        };

        private readonly UIManager uiManager;
        private string pendingRoute;
        private string currentRouteView = InitialRouteViewId;
        private RiveWidget homeWidget;
        private ViewModelInstance homeViewModel;
        private ViewModelInstanceTriggerProperty homeMainButtonTrigger;
        private bool pendingLogoHide;
        private bool started;

        public RiveRouteService(UIManager uiManager)
        {
            this.uiManager = uiManager;
        }

        public void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            uiManager.Initialize();
            homeWidget = uiManager.GetWidget(InitialRouteViewId);
            foreach (var widget in uiManager.Widgets)
            {
                if (widget != null)
                {
                    widget.OnRiveEventReported += OnRiveEventReported;
                }
            }
        }

        public void Tick()
        {
            ApplyPendingRoute();
            ApplyPendingLogoHide();
            BindHomeMainButtonTrigger();
        }

        private void BindHomeMainButtonTrigger()
        {
            var viewModel = homeWidget?.StateMachine?.ViewModelInstance;
            if (viewModel == null || ReferenceEquals(homeViewModel, viewModel))
            {
                return;
            }

            UnbindHomeMainButtonTrigger();
            homeViewModel = viewModel;
            homeMainButtonTrigger = viewModel.GetTriggerProperty(HomeMainButtonTrigger);
            if (homeMainButtonTrigger == null)
            {
                return;
            }

            homeMainButtonTrigger.OnTriggered += OnHomeMainButtonTriggered;
        }

        private void OnHomeMainButtonTriggered()
        {
            pendingRoute = GameViewId;
        }

        private void UnbindHomeMainButtonTrigger()
        {
            if (homeMainButtonTrigger != null)
            {
                homeMainButtonTrigger.OnTriggered -= OnHomeMainButtonTriggered;
            }

            homeMainButtonTrigger = null;
            homeViewModel = null;
        }

        private void ApplyPendingLogoHide()
        {
            if (!pendingLogoHide)
            {
                return;
            }

            pendingLogoHide = false;
            uiManager.Hide(LogoViewId);
        }

        private void ApplyPendingRoute()
        {
            if (string.IsNullOrEmpty(pendingRoute))
            {
                return;
            }

            var route = pendingRoute;
            pendingRoute = null;
            if (!string.Equals(currentRouteView, route, StringComparison.OrdinalIgnoreCase))
            {
                uiManager.Hide(currentRouteView);
            }

            if (!uiManager.Show(route))
            {
                return;
            }

            currentRouteView = route;
        }

        private void QueueLogoHide()
        {
            if (pendingLogoHide)
            {
                return;
            }

            pendingLogoHide = true;
        }

        public void Dispose()
        {
            if (!started)
            {
                return;
            }

            foreach (var widget in uiManager.Widgets)
            {
                if (widget != null)
                {
                    widget.OnRiveEventReported -= OnRiveEventReported;
                }
            }

            started = false;
            pendingRoute = null;
            pendingLogoHide = false;
            currentRouteView = InitialRouteViewId;
            UnbindHomeMainButtonTrigger();
            homeWidget = null;
        }

        private void OnRiveEventReported(ReportedEvent reportedEvent)
        {
            var isFadeOutEvent = string.Equals(
                reportedEvent.Name,
                LogoFadeOutEvent,
                StringComparison.OrdinalIgnoreCase);
            if (isFadeOutEvent)
            {
                QueueLogoHide();
            }

            var route = reportedEvent["route"] as string;
            if (string.IsNullOrWhiteSpace(route))
            {
                route = RouteFromEventName(reportedEvent.Name);
            }

            if (!string.IsNullOrWhiteSpace(route))
            {
                pendingRoute = route.Trim();
            }
        }

        private string RouteFromEventName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return null;
            }

            var trimmedName = eventName.Trim();
            if (uiManager.Contains(trimmedName))
            {
                return trimmedName;
            }

            foreach (var prefix in RoutePrefixes)
            {
                if (trimmedName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedName.Substring(prefix.Length).Trim();
                }
            }

            return null;
        }
    }
}
