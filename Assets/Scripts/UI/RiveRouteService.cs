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
        private const string PlayerProfileButtonTriggerName = "playerProfileBtnTrigger";
        private const string SettingsButtonTriggerName = "settingsBtnTrigger";
        private const string LogoFadeExitTriggerName = "lodingFadeExit";

        private readonly UIManager uiManager;
        private readonly RivePopupRouter popupRouter;

        private RiveWidget mainWidget;
        private RiveWidget logoWidget;
        private ViewModelInstance homeViewModel;
        private ViewModelInstance logoViewModel;
        private ViewModelInstanceTriggerProperty mainButtonTrigger;
        private ViewModelInstanceTriggerProperty playerProfileButtonTrigger;
        private ViewModelInstanceTriggerProperty settingsButtonTrigger;
        private ViewModelInstanceTriggerProperty logoFadeExitTrigger;
        private bool pendingGameLoad;
        private bool pendingLogoHide;
        private bool started;

        public RiveRouteService(UIManager uiManager, RivePopupRouter popupRouter)
        {
            this.uiManager = uiManager;
            this.popupRouter = popupRouter;
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
            logoWidget = uiManager.GetWidget(LogoViewId);

            if (logoWidget != null)
            {
                logoWidget.OnWidgetStatusChanged += OnLogoWidgetStatusChanged;
                BindLogoFadeExitTrigger();
            }

            EnsureHomeIsLoaded();
        }

        public void Tick()
        {
            ApplyPendingGameLoad();
            ApplyPendingLogoHide();
            BindMainButtonTrigger();
            BindLogoFadeExitTrigger();
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
            playerProfileButtonTrigger = viewModel.GetTriggerProperty(PlayerProfileButtonTriggerName);
            settingsButtonTrigger = viewModel.GetTriggerProperty(SettingsButtonTriggerName);
            if (mainButtonTrigger != null)
            {
                mainButtonTrigger.OnTriggered += OnMainButtonTriggered;
            }

            if (playerProfileButtonTrigger != null)
            {
                playerProfileButtonTrigger.OnTriggered += OnPlayerProfileButtonTriggered;
            }

            if (settingsButtonTrigger != null)
            {
                settingsButtonTrigger.OnTriggered += OnSettingsButtonTriggered;
            }
        }

        private void BindLogoFadeExitTrigger()
        {
            if (logoWidget == null)
            {
                UnbindLogoFadeExitTrigger();
                return;
            }

            var viewModel = logoWidget.StateMachine?.ViewModelInstance;
            if (viewModel == null || ReferenceEquals(logoViewModel, viewModel))
            {
                return;
            }

            UnbindLogoFadeExitTrigger();
            logoViewModel = viewModel;
            logoFadeExitTrigger = viewModel.GetTriggerProperty(LogoFadeExitTriggerName);
            if (logoFadeExitTrigger != null)
            {
                logoFadeExitTrigger.OnTriggered += OnLogoFadeExitTriggered;
            }
        }

        private void OnLogoWidgetStatusChanged()
        {
            BindLogoFadeExitTrigger();
        }

        private void OnMainButtonTriggered()
        {
            pendingGameLoad = true;
        }

        private void OnPlayerProfileButtonTriggered()
        {
            popupRouter.ShowPlayerProfile();
        }

        private void OnSettingsButtonTriggered()
        {
            popupRouter.ShowHomeSettings();
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

        private void OnLogoFadeExitTriggered()
        {
            pendingLogoHide = true;
        }

        private void UnbindLogoFadeExitTrigger()
        {
            if (logoFadeExitTrigger != null)
            {
                logoFadeExitTrigger.OnTriggered -= OnLogoFadeExitTriggered;
            }

            logoFadeExitTrigger = null;
            logoViewModel = null;
        }

        private void UnbindMainButtonTrigger()
        {
            if (mainButtonTrigger != null)
            {
                mainButtonTrigger.OnTriggered -= OnMainButtonTriggered;
            }

            if (playerProfileButtonTrigger != null)
            {
                playerProfileButtonTrigger.OnTriggered -= OnPlayerProfileButtonTriggered;
            }

            if (settingsButtonTrigger != null)
            {
                settingsButtonTrigger.OnTriggered -= OnSettingsButtonTriggered;
            }

            mainButtonTrigger = null;
            playerProfileButtonTrigger = null;
            settingsButtonTrigger = null;
            homeViewModel = null;
        }

        public void Dispose()
        {
            if (!started)
            {
                return;
            }

            UnbindMainButtonTrigger();
            UnbindLogoFadeExitTrigger();
            if (logoWidget != null)
            {
                logoWidget.OnWidgetStatusChanged -= OnLogoWidgetStatusChanged;
            }

            mainWidget = null;
            logoWidget = null;
            pendingGameLoad = false;
            pendingLogoHide = false;
            started = false;
        }
    }
}
