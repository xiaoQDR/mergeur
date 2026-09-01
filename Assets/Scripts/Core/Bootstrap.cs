using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Mergeur.Core
{
    /// <summary>
    /// Application composition root. Keep registrations here and business logic in services.
    /// Bootstrap stays loaded as the persistent UI layer while the gameplay scene is loaded additively underneath it.
    /// </summary>
    public sealed class Bootstrap : LifetimeScope
    {
        private const int TargetFrameRate = 60;
        private const string GameScenePath = "Assets/Game/Scenes/Game.unity";

        [SerializeField] private UIManager uiManager;
        [SerializeField] private RivePopupRouter popupRouter;

        private float smoothedFps;
        private GUIStyle fpsStyle;

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
            // Keep Bootstrap loaded so its Screen Space - Overlay Rive canvas remains above gameplay.
            // Game is loaded additively, which gives us gameplay + Rive UI at the same time.
            Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
            if (!gameScene.IsValid() || !gameScene.isLoaded)
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameScenePath, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogError($"Bootstrap failed to start gameplay scene: {GameScenePath}");
                    yield break;
                }

                yield return loadOperation;
                gameScene = SceneManager.GetSceneByPath(GameScenePath);
            }

            if (gameScene.IsValid() && gameScene.isLoaded)
            {
                // Make Game the active scene for runtime-created gameplay objects while Bootstrap stays loaded.
                SceneManager.SetActiveScene(gameScene);
            }
            else
            {
                Debug.LogError($"Bootstrap could not find the loaded gameplay scene: {GameScenePath}");
            }
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var currentFps = 1f / deltaTime;
            smoothedFps = smoothedFps <= 0f
                ? currentFps
                : Mathf.Lerp(smoothedFps, currentFps, 0.1f);
        }

        private void OnGUI()
        {
            if (fpsStyle == null)
            {
                fpsStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.02f), 24, 48),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
                fpsStyle.normal.textColor = Color.white;
            }

            GUI.Label(
                new Rect(16f, 16f, 420f, 72f),
                $"FPS {smoothedFps:0.0}  Target {Application.targetFrameRate}",
                fpsStyle);
        }
    }
}
