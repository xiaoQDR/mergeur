using Rive;
using Rive.Components;
using UnityEngine;

namespace Mergeur.Core
{
    public enum PopupRoute
    {
        PlayerProfile,
        LevelSelect,
        HomeSettings,
        GameSettings,
        ExitConfirm,
        EnergyCost,
        Victory,
        Defeat,
        Reward
    }

    [DisallowMultipleComponent]
    public sealed class RivePopupRouter : MonoBehaviour
    {
        [SerializeField] private RiveWidget popupWidget;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Asset popups;

        private Vector3 visibleScale = Vector3.one;
        private HitTestBehavior visibleHitTestBehavior;
        private bool initialized;

        public PopupRoute? Current { get; private set; }
        public bool IsOpen => Current.HasValue;

        private void Awake()
        {
            Initialize();
            SetVisible(false);
        }

        public void Show(PopupRoute route)
        {
            Initialize();
            var definition = GetDefinition(route);
            if (popupWidget == null || popupRoot == null || popups == null)
            {
                Debug.LogError(
                    $"[{nameof(RivePopupRouter)}] Cannot show {route}: " +
                    "popupWidget, popupRoot, or popups is not assigned.",
                    this);
                return;
            }

            popupWidget.Load(popups, definition.Artboard, definition.StateMachine);
            Current = route;
            SetVisible(true);

            Debug.Log(
                $"[{nameof(RivePopupRouter)}] Showing {route}: " +
                $"{definition.Artboard}/{definition.StateMachine}.",
                this);
        }

        public void Hide()
        {
            Initialize();
            SetVisible(false);
            Current = null;
        }

        public void ShowPlayerProfile() => Show(PopupRoute.PlayerProfile);
        public void ShowLevelSelect() => Show(PopupRoute.LevelSelect);
        public void ShowHomeSettings() => Show(PopupRoute.HomeSettings);
        public void ShowGameSettings() => Show(PopupRoute.GameSettings);
        public void ShowExitConfirm() => Show(PopupRoute.ExitConfirm);
        public void ShowEnergyCost() => Show(PopupRoute.EnergyCost);
        public void ShowVictory() => Show(PopupRoute.Victory);
        public void ShowDefeat() => Show(PopupRoute.Defeat);
        public void ShowReward() => Show(PopupRoute.Reward);

        [ContextMenu("Test Popup/Player Profile")]
        private void TestPlayerProfile() => Test(PopupRoute.PlayerProfile);

        [ContextMenu("Test Popup/Level Select")]
        private void TestLevelSelect() => Test(PopupRoute.LevelSelect);

        [ContextMenu("Test Popup/Home Settings")]
        private void TestHomeSettings() => Test(PopupRoute.HomeSettings);

        [ContextMenu("Test Popup/Game Settings")]
        private void TestGameSettings() => Test(PopupRoute.GameSettings);

        [ContextMenu("Test Popup/Exit Confirm")]
        private void TestExitConfirm() => Test(PopupRoute.ExitConfirm);

        [ContextMenu("Test Popup/Energy Cost")]
        private void TestEnergyCost() => Test(PopupRoute.EnergyCost);

        [ContextMenu("Test Popup/Victory")]
        private void TestVictory() => Test(PopupRoute.Victory);

        [ContextMenu("Test Popup/Defeat")]
        private void TestDefeat() => Test(PopupRoute.Defeat);

        [ContextMenu("Test Popup/Reward")]
        private void TestReward() => Test(PopupRoute.Reward);

        [ContextMenu("Test Popup/Hide")]
        private void TestHide()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    $"[{nameof(RivePopupRouter)}] Popup tests only run in Play Mode.",
                    this);
                return;
            }

            Hide();
        }

        private void Test(PopupRoute route)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    $"[{nameof(RivePopupRouter)}] Popup tests only run in Play Mode.",
                    this);
                return;
            }

            Show(route);
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (popupRoot == null && popupWidget != null)
            {
                popupRoot = popupWidget.gameObject;
            }

            if (popupRoot == null || popupWidget == null)
            {
                return;
            }

            // Keep the Rive widget registered with its panel. Rive 0.4.x can leave
            // native rendering/input state invalid after repeated disable/enable cycles.
            if (!popupRoot.activeSelf)
            {
                popupRoot.SetActive(true);
            }

            visibleScale = popupRoot.transform.localScale;
            visibleHitTestBehavior = popupWidget.HitTestBehavior;
        }

        private void SetVisible(bool visible)
        {
            if (popupRoot == null || popupWidget == null)
            {
                return;
            }

            if (!popupRoot.activeSelf)
            {
                popupRoot.SetActive(true);
            }

            if (visible)
            {
                popupRoot.transform.localScale = visibleScale;
                popupWidget.HitTestBehavior = visibleHitTestBehavior;
                popupRoot.transform.SetAsLastSibling();
            }
            else
            {
                popupWidget.HitTestBehavior = HitTestBehavior.None;
                popupRoot.transform.localScale = Vector3.zero;
            }
        }

        private PopupDefinition GetDefinition(PopupRoute route)
        {
            switch (route)
            {
                case PopupRoute.PlayerProfile:
                    return new PopupDefinition("PlayerProfilePopup ", "PlayerProfilePopupSM");
                case PopupRoute.LevelSelect:
                    return new PopupDefinition("LevelSelectPopup", "LevelSelectPopupSM");
                case PopupRoute.HomeSettings:
                    return new PopupDefinition("HomeSettingsPopup", "HomeSettingsPopupSM");
                case PopupRoute.GameSettings:
                    return new PopupDefinition("GameSettingsPopup", "GameSettingsPopupSM");
                case PopupRoute.ExitConfirm:
                    return new PopupDefinition("ExitConfirmPopup ", "ExitConfirmPopupSM");
                case PopupRoute.EnergyCost:
                    return new PopupDefinition("EnergyCostPopup", "EnergyCostPopupSM");
                case PopupRoute.Victory:
                    return new PopupDefinition("VictoryPopup", "VictoryPopupSM");
                case PopupRoute.Defeat:
                    return new PopupDefinition("DefeatPopup", "DefeatPopupSM");
                case PopupRoute.Reward:
                    return new PopupDefinition("RewardPopup", "RewardPopupSM");
                default:
                    return default;
            }
        }

        private readonly struct PopupDefinition
        {
            public PopupDefinition(string artboard, string stateMachine)
            {
                Artboard = artboard;
                StateMachine = stateMachine;
            }

            public string Artboard { get; }
            public string StateMachine { get; }
        }
    }
}
