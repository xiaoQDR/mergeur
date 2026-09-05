using System;
using Rive;
using Rive.Components;
using VContainer.Unity;

namespace Mergeur.Core
{
    /// <summary>
    /// Routes the single main Rive widget between artboards in merge_main.riv.
    /// </summary>
    public sealed class RiveRouteService : IStartable, ITickable, IDisposable
    {
        private const string MainViewId = "main";
        private const string LogoViewId = "logo";
        private const string HomeArtboardName = "Home";
        private const string HomeStateMachineName = "HomeSM";
        private const string GameArtboardName = "Game";
        private const string GameStateMachineName = "GameSM";
        private const string MainButtonTriggerName = "mainBtnTrigget";
        private const string LogoFadeOutEventName = "fadeOut";

        private readonly UIManager uiManager;

        private RiveWidget mainWidget;
        private ViewModelInstance homeViewModel;
        private ViewModelInstanceTriggerProperty mainButtonTrigger;
        private bool pendingGameLoad;
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
            mainWidget = uiManager.GetWidget(MainViewId);

            foreach (var widget in uiManager.Widgets)
            {
                if (widget != null)
                {
                    widget.OnRiveEventReported += OnRiveEventReported;
                }
            }

            EnsureHomeIsLoaded();
        }

        public void Tick()
        {
            ApplyPendingGameLoad();
            ApplyPendingLogoHide();
            BindMainButtonTrigger();
        }

        private void EnsureHomeIsLoaded()
        {
            if (mainWidget == null || mainWidget.Asset == null)
            {
                return;
            }

            if (!string.Equals(mainWidget.ArtboardName, HomeArtboardName, StringComparison.Ordinal) ||
                !string.Equals(mainWidget.StateMachineName, HomeStateMachineName, StringComparison.Ordinal))
            {
                mainWidget.Load(mainWidget.Asset, HomeArtboardName, HomeStateMachineName);
            }
        }

        private void BindMainButtonTrigger()
        {
            if (mainWidget == null ||
                !string.Equals(mainWidget.ArtboardName, HomeArtboardName, StringComparison.Ordinal))
            {
                UnbindMainButtonTrigger();
                return;
            }

            var viewModel = mainWidget.StateMachine?.ViewModelInstance;
            if (viewModel == null || ReferenceEquals(homeViewModel, viewModel))
            {
                return;
            }

            UnbindMainButtonTrigger();
            homeViewModel = viewModel;
            mainButtonTrigger = viewModel.GetTriggerProperty(MainButtonTriggerName);
            if (mainButtonTrigger != null)
            {
                mainButtonTrigger.OnTriggered += OnMainButtonTriggered;
            }
        }

        private void OnMainButtonTriggered()
        {
            pendingGameLoad = true;
        }

        private void ApplyPendingGameLoad()
        {
            if (!pendingGameLoad)
            {
                return;
            }

            pendingGameLoad = false;
            UnbindMainButtonTrigger();

            if (mainWidget?.Asset != null)
            {
                mainWidget.Load(mainWidget.Asset, GameArtboardName, GameStateMachineName);
            }
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

        private void OnRiveEventReported(ReportedEvent reportedEvent)
        {
            if (string.Equals(reportedEvent.Name, LogoFadeOutEventName, StringComparison.OrdinalIgnoreCase))
            {
                pendingLogoHide = true;
            }
        }

        private void UnbindMainButtonTrigger()
        {
            if (mainButtonTrigger != null)
            {
                mainButtonTrigger.OnTriggered -= OnMainButtonTriggered;
            }

            mainButtonTrigger = null;
            homeViewModel = null;
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

            UnbindMainButtonTrigger();
            mainWidget = null;
            pendingGameLoad = false;
            pendingLogoHide = false;
            started = false;
        }
    }
}
