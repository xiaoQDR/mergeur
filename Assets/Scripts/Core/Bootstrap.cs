using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Mergeur.Core
{
    /// <summary>
    /// Application composition root. Keep registrations here and business logic in services.
    /// </summary>
    public sealed class Bootstrap : LifetimeScope
    {
        private const int TargetFrameRate = 60;

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
