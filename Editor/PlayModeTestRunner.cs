using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EditorTool.SceneSelectTool
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private static TestRunnerApi sTestRunnerApi;
        private static MasterScenePlayModeTestCallbacks sTestCallbacks;

        static PlayModeTestRunner()
        {
            RegisterCallbacks();
        }

        [MenuItem("Tools/Scene Select Tool/Run PlayMode Tests")]
        internal static void RunAllPlayModeTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("PlayMode tests cannot be started while Unity is already entering or leaving Play Mode.");
                return;
            }

            SceneAutoLoader.SuppressMasterSceneLoading = true;

            var filter = new Filter
            {
                testMode = TestMode.PlayMode
            };

            var executionSettings = new ExecutionSettings(filter);

            try
            {
#pragma warning disable CS0618
                var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                testRunnerApi.Execute(executionSettings);
#pragma warning restore CS0618
            }
            catch
            {
                SceneAutoLoader.SuppressMasterSceneLoading = false;
                throw;
            }
        }

        private static void RegisterCallbacks()
        {
            sTestCallbacks = new MasterScenePlayModeTestCallbacks();

#pragma warning disable CS0618
            sTestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            sTestRunnerApi.RegisterCallbacks(sTestCallbacks, int.MaxValue);
#pragma warning restore CS0618
        }

        private sealed class MasterScenePlayModeTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                SceneAutoLoader.SuppressMasterSceneLoading = false;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
