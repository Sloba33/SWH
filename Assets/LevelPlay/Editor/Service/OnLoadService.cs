using System;
using UnityEditor;
using System.Threading.Tasks;
using Unity.Services.LevelPlay.Editor.Analytics;

namespace Unity.Services.LevelPlay.Editor
{
    internal class OnLoadService : IOnLoadService
    {
        [InitializeOnLoadMethod]
        static async Task InitializeOnLoad()
        {
            var onLoadService = EditorServices.Instance.OnLoadService;
            await onLoadService.OnLoad();
        }

        private readonly IEditorAnalyticsService m_EditorAnalyticsService;
        private readonly IIronSourceSdkInstaller m_IronSourceSdkInstaller;
        internal OnLoadService(IEditorAnalyticsService editorAnalyticsService, IIronSourceSdkInstaller ironSourceSdkInstaller)
        {
            m_EditorAnalyticsService = editorAnalyticsService;
            m_IronSourceSdkInstaller = ironSourceSdkInstaller;
        }

        public async Task OnLoad()
        {
            EditorSessionTracker.NewSession();
            m_EditorAnalyticsService.Initialize();
            ServicesCoreDependencyInstaller.InstallServicesCoreIfNotFound();
            // Disabled: installs an older EDM4U at Assets/MobileDependencyResolver/ that conflicts
            // with the newer one at Assets/ExternalDependencyManager/ and breaks Android builds
            // (re-emits jcenter() into mainTemplate.gradle).
            // MobileDependencyResolverInstaller.InstallPlayServicesResolverIfNeeded();
            EnvironmentVariables.BuildManifestPath();

            await m_IronSourceSdkInstaller.OnLoad();
        }
    }
}
