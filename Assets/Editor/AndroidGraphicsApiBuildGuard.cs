#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mergeur.Editor
{
    [InitializeOnLoad]
    internal static class AndroidGraphicsApiSettings
    {
        private static readonly GraphicsDeviceType[] OpenGles3Only =
        {
            GraphicsDeviceType.OpenGLES3
        };

        static AndroidGraphicsApiSettings()
        {
            EditorApplication.delayCall += EnsureOpenGles3Only;
        }

        internal static void EnsureOpenGles3Only()
        {
            const BuildTarget target = BuildTarget.Android;
            var currentApis = PlayerSettings.GetGraphicsAPIs(target);

            var needsUpdate =
                PlayerSettings.GetUseDefaultGraphicsAPIs(target) ||
                currentApis == null ||
                currentApis.Length != 1 ||
                currentApis[0] != GraphicsDeviceType.OpenGLES3;

            if (!needsUpdate)
            {
                return;
            }

            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, OpenGles3Only);

            Debug.Log("[AndroidGraphicsApi] Android graphics API forced to OpenGLES3 only; Vulkan is disabled.");
        }
    }

    internal sealed class AndroidGraphicsApiBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            AndroidGraphicsApiSettings.EnsureOpenGles3Only();

            var currentApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            var valid =
                !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
                currentApis != null &&
                currentApis.Length == 1 &&
                currentApis[0] == GraphicsDeviceType.OpenGLES3;

            if (!valid)
            {
                throw new BuildFailedException(
                    "Android build must use OpenGLES3 only. Vulkan/Automatic Graphics API is disabled because of Rive Android native stability issues.");
            }
        }
    }
}
#endif
