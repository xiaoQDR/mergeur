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
        [SerializeField] private UIManager uiManager;
        [SerializeField] private RivePopupRouter popupRouter;

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
