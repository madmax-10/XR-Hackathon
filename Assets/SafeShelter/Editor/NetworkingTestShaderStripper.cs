using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace SafeShelter.Editor
{
    /// <summary>The networking test has no inference models; skip that package's costly shader variants.
    /// This filter is inactive for normal builds and does not change package or graphics settings.</summary>
    public sealed class NetworkingTestShaderStripper : IPreprocessShaders, IPreprocessComputeShaders
    {
        public int callbackOrder => 0;
        static bool IsUnusedInferenceShader(UnityEngine.Object shader) =>
            DesktopNetworkingSetup.IsBuildingNetworkTest && AssetDatabase.GetAssetPath(shader)
                .StartsWith("Packages/com.unity.ai.inference/", StringComparison.Ordinal);

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (IsUnusedInferenceShader(shader)) data.Clear();
        }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (IsUnusedInferenceShader(shader)) data.Clear();
        }
    }
}
