using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Mergeur.Core
{
    /// <summary>
    /// Application composition root. Keep registrations here and business logic in services.
    /// Bootstrap remains loaded as the Rive UI layer while gameplay is loaded underneath it.
    /// </summary>
    public sealed class Bootstrap : LifetimeScope
    {
        private const int TargetFrameRate = 60;
        private const string GameScenePath = "Assets/Game/Scenes/Game.unity";

        [SerializeField] private UIManager uiManager;
        [SerializeField] private RivePopupRouter popupRouter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureFrameRate()
        {
            // Mobile platforms ignore vSyncCount and use Application.targetFrameRate.
            // Keep vSync disabled here as well so desktop/editor test runs use the same 60 FPS cap.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (uiManager == null)
            {
                uiManager = GetComponentInChildren<UIManager>(true);
            }

            if (uiManager == null)
            {
                throw new MissingReferenceException(
                    "Bootstrap requires a UIManager reference in the Bootstrap scene.");
            }

            builder.RegisterComponent(uiManager);
            if (popupRouter == null)
            {
                popupRouter = GetComponentInChildren<RivePopupRouter>(true);
            }

            if (popupRouter != null)
            {
                builder.RegisterComponent(popupRouter);
            }

            builder.RegisterEntryPoint<RiveRouteService>(Lifetime.Singleton).AsSelf();
        }

        private IEnumerator Start()
        {
            Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
            if (!gameScene.IsValid() || !gameScene.isLoaded)
            {
                if (!Application.CanStreamedLevelBeLoaded(GameScenePath))
                {
                    Debug.LogError(
                        $"Bootstrap cannot load {GameScenePath}. Ensure the Game scene is enabled in Build Settings.");
                    yield break;
                }

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameScenePath, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogError($"Bootstrap failed to start loading {GameScenePath}.");
                    yield break;
                }

                yield return loadOperation;
                gameScene = SceneManager.GetSceneByPath(GameScenePath);
            }

            if (!gameScene.IsValid() || !gameScene.isLoaded)
            {
                Debug.LogError($"Bootstrap could not find the loaded Game scene at {GameScenePath}.");
                yield break;
            }

            // Runtime-created gameplay objects belong to Game. Bootstrap stays loaded so its
            // Screen Space Overlay canvas and Rive widgets continue rendering above gameplay.
            SceneManager.SetActiveScene(gameScene);
        }

    }
}
