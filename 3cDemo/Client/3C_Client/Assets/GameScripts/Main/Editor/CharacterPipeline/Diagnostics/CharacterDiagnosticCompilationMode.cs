using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterDiagnosticCompilationMode
    {
        public const string SamplingDefine = "KK_DIAGNOSTIC_SAMPLING";
        public const string FootDefine = "KK_DIAGNOSTIC_FOOT";

        public static bool IsFootCaptureEnabled
        {
            get
            {
                string[] defines = ReadDefines();
                return Array.IndexOf(defines, SamplingDefine) >= 0 &&
                       Array.IndexOf(defines, FootDefine) >= 0;
            }
        }

        [MenuItem("Tools/3C/Diagnostics/Enable Foot Capture Compilation")]
        public static void EnableFootCapture() => SetFootCapture(true);

        [MenuItem("Tools/3C/Diagnostics/Disable Foot Capture Compilation")]
        public static void DisableFootCapture() => SetFootCapture(false);

        static void SetFootCapture(bool enabled)
        {
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("请等待当前编译完成后再切换采样开关。");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Foot diagnostic compilation mode can only change outside Play Mode.");

            NamedBuildTarget target = ResolveTarget();
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] current);
            var next = new List<string>();
            foreach (string define in current)
            {
                if (string.IsNullOrWhiteSpace(define) ||
                    string.Equals(define, SamplingDefine, StringComparison.Ordinal) ||
                    string.Equals(define, FootDefine, StringComparison.Ordinal) ||
                    next.Contains(define))
                {
                    continue;
                }
                next.Add(define);
            }
            if (enabled)
            {
                next.Add(SamplingDefine);
                next.Add(FootDefine);
            }
            PlayerSettings.SetScriptingDefineSymbols(target, next.ToArray());
        }

        static string[] ReadDefines()
        {
            PlayerSettings.GetScriptingDefineSymbols(ResolveTarget(), out string[] defines);
            return defines ?? Array.Empty<string>();
        }

        static NamedBuildTarget ResolveTarget()
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(
                EditorUserBuildSettings.activeBuildTarget);
            if (group == BuildTargetGroup.Unknown)
                throw new InvalidOperationException("The active build target has no scripting define group.");
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }
    }
}
