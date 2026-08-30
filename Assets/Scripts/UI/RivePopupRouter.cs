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
        [SerializeField] private Asset homePopups;
        [SerializeField] private Asset gamePopups;
        [SerializeField] private Asset resultPopups;

        public PopupRoute? Current { get; private set; }
        public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

        private void Awake()
        {
            if (popupRoot == null && popupWidget != null)
            {
                popupRoot = popupWidget.gameObject;
            }

            popupRoot?.SetActive(false);
        }

        public void Show(PopupRoute route)
        {
            var definition = GetDefinition(route);
            if (popupWidget == null || popupRoot == null || definition.Asset == null)
            {
                return;
            }

            popupRoot.SetActive(true);
            popupWidget.Load(definition.Asset, definition.Artboard, definition.StateMachine);
            Current = route;
        }

        public void Hide()
        {
            popupRoot?.SetActive(false);
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
            if (Application.isPlaying)
            {
                Hide();
            }
        }

        private void Test(PopupRoute route)
        {
            if (Application.isPlaying)
            {
                Show(route);
            }
        }

        private PopupDefinition GetDefinition(PopupRoute route)
        {
            switch (route)
            {
                case PopupRoute.PlayerProfile:
                    return new PopupDefinition(homePopups, "PlayerProfilePopup ", "PlayerProfilePopupSM");
                case PopupRoute.LevelSelect:
                    return new PopupDefinition(homePopups, "LevelSelectPopup", "LevelSelectPopupSM");
                case PopupRoute.HomeSettings:
                    return new PopupDefinition(homePopups, "HomeSettingsPopup", "HomeSettingsPopupSM");
                case PopupRoute.GameSettings:
                    return new PopupDefinition(gamePopups, "GameSettingsPopup", "GameSettingsPopupSM");
                case PopupRoute.ExitConfirm:
                    return new PopupDefinition(gamePopups, "ExitConfirmPopup ", "ExitConfirmPopupSM");
                case PopupRoute.EnergyCost:
                    return new PopupDefinition(gamePopups, "EnergyCostPopup", "EnergyCostPopupSM");
                case PopupRoute.Victory:
                    return new PopupDefinition(resultPopups, "VictoryPopup", "VictoryPopupSM");
                case PopupRoute.Defeat:
                    return new PopupDefinition(resultPopups, "DefeatPopup", "DefeatPopupSM");
                case PopupRoute.Reward:
                    return new PopupDefinition(resultPopups, "RewardPopup", "RewardPopupSM");
                default:
                    return default;
            }
        }

        private readonly struct PopupDefinition
        {
            public PopupDefinition(Asset asset, string artboard, string stateMachine)
            {
                Asset = asset;
                Artboard = artboard;
                StateMachine = stateMachine;
            }

            public Asset Asset { get; }
            public string Artboard { get; }
            public string StateMachine { get; }
        }
    }
}
