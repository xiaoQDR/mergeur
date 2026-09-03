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

    }
}
