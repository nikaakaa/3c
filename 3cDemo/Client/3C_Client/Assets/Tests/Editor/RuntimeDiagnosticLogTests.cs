using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThirdPersonDiagnostics;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonDiagnostics.Tests
{
    public sealed class RuntimeDiagnosticLogTests
    {
        readonly List<GameObject> createdGameObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            RuntimeDiagnosticLog.Reset();
            for (int i = 0; i < createdGameObjects.Count; i++)
                Object.DestroyImmediate(createdGameObjects[i]);

            createdGameObjects.Clear();
        }

        [Test]
        public void FilterBlocksDisabledCategory()
        {
            List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();
            RuntimeDiagnosticLog.Filter.SetEnabled(RuntimeDiagnosticLogCategory.Locomotion, false);

            using (RuntimeDiagnosticLog.Capture(events.Add))
            {
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Locomotion,
                    RuntimeDiagnosticLogLevel.Info,
                    "locomotion-phase-changed"));
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.FullBody,
                    RuntimeDiagnosticLogLevel.Info,
                    "fullbody-path-changed"));
            }

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(RuntimeDiagnosticLogCategory.FullBody, events[0].Category);
        }

        [Test]
        public void FilterBlocksDisabledChannelKey()
        {
            List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();
            RuntimeDiagnosticLog.Filter.SetChannelEnabled("Action.Dodge.Accepted", false);

            using (RuntimeDiagnosticLog.Capture(events.Add))
            {
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Action,
                    RuntimeDiagnosticLogLevel.Info,
                    "dodge-accepted",
                    channelKey: "Action.Dodge.Accepted"));
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Action,
                    RuntimeDiagnosticLogLevel.Info,
                    "dodge-rejected",
                    channelKey: "Action.Dodge.Rejected"));
            }

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("Action.Dodge.Rejected", events[0].ChannelKey);
        }

        [Test]
        public void EventUsesDefaultChannelKeyWhenMissing()
        {
            RuntimeDiagnosticLogEvent diagnosticEvent = new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Info,
                "interrupt-request-accepted");

            Assert.AreEqual("Action.interrupt-request-accepted", diagnosticEvent.ChannelKey);
        }

        [Test]
        public void EventUsesExplicitChannelKeyWhenProvided()
        {
            RuntimeDiagnosticLogEvent diagnosticEvent = new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Info,
                "interrupt-request-accepted",
                channelKey: "Action.Interrupt.Accepted");

            Assert.AreEqual("Action.Interrupt.Accepted", diagnosticEvent.ChannelKey);
        }

        [Test]
        public void FormatIncludesStableContextFields()
        {
            RuntimeDiagnosticLogEvent diagnosticEvent = new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Info,
                "fullbody-path-changed",
                "Action.Dodge",
                "Locomotion.MoveStart",
                3,
                9,
                "owner=Action action=Action.Dodge");

            string formatted = RuntimeDiagnosticLog.Format(in diagnosticEvent);

            StringAssert.Contains("[3C-DIAG][Info][FullBody]", formatted);
            StringAssert.Contains("[FullBody.fullbody-path-changed]", formatted);
            StringAssert.Contains("frame=9", formatted);
            StringAssert.Contains("step=3", formatted);
            StringAssert.Contains("from=Locomotion.MoveStart", formatted);
            StringAssert.Contains("path=Action.Dodge", formatted);
            StringAssert.Contains("message=fullbody-path-changed", formatted);
            StringAssert.Contains("owner=Action action=Action.Dodge", formatted);
        }

        [Test]
        public void FormatPreservesTurnBackRootMotionKeyword()
        {
            RuntimeDiagnosticLogEvent diagnosticEvent = new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "turnback-root-motion-consumed",
                "Locomotion.TurnBack",
                string.Empty,
                7,
                42,
                "[TURNBACK_RM_CHAIN] stage=controller alias=Locomotion.Turn.Back bakedMotion=True yawDelta=3.5");

            string formatted = RuntimeDiagnosticLog.Format(in diagnosticEvent);

            StringAssert.Contains("TURNBACK_RM_CHAIN", formatted);
            StringAssert.Contains("stage=controller", formatted);
            StringAssert.Contains("message=turnback-root-motion-consumed", formatted);
        }

        [Test]
        public void RuntimeDiagnosticLogUsesBuildDefineForUnityOutput()
        {
            string source = File.ReadAllText(SourcePath("RuntimeDiagnosticLog.cs"));

            StringAssert.Contains("THIRDPERSON_DIAGNOSTIC_LOGS", source);
            StringAssert.Contains("Conditional(\"THIRDPERSON_DIAGNOSTIC_LOGS\")", source);
            StringAssert.Contains("Conditional(\"UNITY_EDITOR\")", source);
            StringAssert.Contains("Conditional(\"UNITY_INCLUDE_TESTS\")", source);
        }

        [Test]
        public void InspectorControllerAppliesChannelsToUnifiedFilter()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();
            RuntimeDiagnosticLog.RegisterChannel("Action.Dodge.Accepted");

            controller.SetChannelEnabled("Action.Dodge.Accepted", false);
            controller.ApplyChannels();

            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Action.Dodge.Accepted"));
        }

        [Test]
        public void InspectorControllerContainsFilterOnlyEnablesMatchingChannels()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();
            RuntimeDiagnosticLog.RegisterChannel("Action.Dodge.Accepted");
            RuntimeDiagnosticLog.RegisterChannel("Animation.Motion.Window");
            RuntimeDiagnosticLog.RegisterChannel("FullBody.Path.Changed");

            controller.ApplyContainsFilter("Dodge");

            Assert.True(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Action.Dodge.Accepted"));
            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Animation.Motion.Window"));
            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("FullBody.Path.Changed"));
        }

        [Test]
        public void InspectorControllerPrefixFilterOnlyEnablesMatchingChannels()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();
            RuntimeDiagnosticLog.RegisterChannel("Action.Dodge.Accepted");
            RuntimeDiagnosticLog.RegisterChannel("Animation.Motion.Window");
            RuntimeDiagnosticLog.RegisterChannel("FullBody.Path.Changed");

            controller.ApplyPrefixFilter("Action.");

            Assert.True(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Action.Dodge.Accepted"));
            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Animation.Motion.Window"));
            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("FullBody.Path.Changed"));
        }

        [Test]
        public void InspectorControllerSuffixFilterOnlyEnablesMatchingChannels()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();
            RuntimeDiagnosticLog.RegisterChannel("Action.Dodge.Accepted");
            RuntimeDiagnosticLog.RegisterChannel("Animation.Motion.Window");
            RuntimeDiagnosticLog.RegisterChannel("FullBody.Path.Changed");

            controller.ApplySuffixFilter(".Changed");

            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Action.Dodge.Accepted"));
            Assert.False(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Animation.Motion.Window"));
            Assert.True(RuntimeDiagnosticLog.Filter.IsChannelEnabled("FullBody.Path.Changed"));
        }

        [Test]
        public void InspectorControllerSynchronizesOneChannelForEachKey()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();
            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty channels = serializedObject.FindProperty("channels");
            channels.arraySize = 2;
            SetSerializedChannel(channels.GetArrayElementAtIndex(0), "Action.Dodge.Accepted", false);
            SetSerializedChannel(channels.GetArrayElementAtIndex(1), "Action.Dodge.Accepted", true);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            RuntimeDiagnosticLog.RegisterChannel("FullBody.Path.Changed");

            controller.SynchronizeChannels();

            Assert.AreEqual(2, controller.Channels.Count);
            Assert.AreEqual(1, CountChannel(controller, "Action.Dodge.Accepted"));
            Assert.False(FindChannel(controller, "Action.Dodge.Accepted").Enabled);
            Assert.True(FindChannel(controller, "FullBody.Path.Changed").Enabled);
        }

        [Test]
        public void InspectorControllerAddsManualChannelKey()
        {
            RuntimeDiagnosticLogInspectorController controller = CreateController();

            controller.AddManualChannel("Action.Debug.Window");

            Assert.True(RuntimeDiagnosticLog.Filter.IsChannelEnabled("Action.Debug.Window"));
            Assert.AreEqual(1, CountChannel(controller, "Action.Debug.Window"));
            Assert.True(FindChannel(controller, "Action.Debug.Window").Enabled);
        }

        [Test]
        public void SubmittedEventRegistersDefaultChannelKey()
        {
            using (RuntimeDiagnosticLog.Capture(_ => { }))
            {
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Input,
                    RuntimeDiagnosticLogLevel.Info,
                    "input-request-added"));
            }

            string[] keys = RuntimeDiagnosticLog.Filter.GetKnownChannelKeys();

            CollectionAssert.Contains(keys, "Input.input-request-added");
        }

        [Test]
        public void ResetRestoresUnityOutputMinimumLevel()
        {
            RuntimeDiagnosticLog.MinimumUnityLogLevel = RuntimeDiagnosticLogLevel.Warning;

            RuntimeDiagnosticLog.Reset();

            Assert.AreEqual(RuntimeDiagnosticLogLevel.Info, RuntimeDiagnosticLog.MinimumUnityLogLevel);
        }

        [Test]
        public void InspectorControllerSourceUsesUnifiedLogEntryPoints()
        {
            string source = File.ReadAllText(SourcePath("RuntimeDiagnosticLogInspectorController.cs"));

            StringAssert.Contains("RuntimeDiagnosticLog.Filter", source);
            Assert.False(source.Contains("RuntimeDiagnosticLog.Submit"));
        }

        [Test]
        public void InspectorControllerSourcesDoNotReferenceForbiddenRuntimePaths()
        {
            string[] files =
            {
                SourcePath("RuntimeDiagnosticLogChannelToggle.cs"),
                SourcePath("RuntimeDiagnosticLogInspectorController.cs")
            };

            string[] forbidden =
            {
                "Animancer",
                "Animator",
                "AnimationClip",
                "CharacterController",
                "KinematicCharacterMotor",
                "InputAction",
                "Cinemachine",
                "UnityHFSM",
                "BBBNexus",
                "StateMachine"
            };

            for (int i = 0; i < files.Length; i++)
            {
                string source = File.ReadAllText(files[i]);
                for (int j = 0; j < forbidden.Length; j++)
                    Assert.False(source.Contains(forbidden[j]), $"{files[i]} references {forbidden[j]}");
            }
        }

        [Test]
        public void InspectorControllerEditorDoesNotEmitDirectDebugLogs()
        {
            string source = File.ReadAllText(EditorPath("RuntimeDiagnosticLogInspectorControllerEditor.cs"));

            Assert.False(source.Contains("Debug.Log"));
            Assert.False(source.Contains("RuntimeDiagnosticLog.Submit"));
        }

        [Test]
        public void RuntimeDiagnosticSourcesDoNotReferenceForbiddenRuntimePaths()
        {
            string[] files =
            {
                SourcePath("RuntimeDiagnosticLog.cs"),
                SourcePath("RuntimeDiagnosticLogCategory.cs"),
                SourcePath("RuntimeDiagnosticLogLevel.cs"),
                SourcePath("RuntimeDiagnosticLogEvent.cs"),
                SourcePath("RuntimeDiagnosticLogFilter.cs"),
                SourcePath("RuntimeDiagnosticLogChannelToggle.cs"),
                SourcePath("RuntimeDiagnosticLogInspectorController.cs")
            };

            string[] forbidden =
            {
                "Animancer",
                "Animator",
                "AnimationClip",
                "CharacterController",
                "KinematicCharacterMotor",
                "InputAction",
                "Cinemachine",
                "UnityHFSM",
                "BBBNexus",
                "StateMachine"
            };

            for (int i = 0; i < files.Length; i++)
            {
                string source = File.ReadAllText(files[i]);
                for (int j = 0; j < forbidden.Length; j++)
                    Assert.False(source.Contains(forbidden[j]), $"{files[i]} references {forbidden[j]}");
            }
        }

        static string SourcePath(string fileName)
        {
            return Path.Combine(Application.dataPath, "Scripts", "Diagnostics", fileName);
        }

        static string EditorPath(string fileName)
        {
            return Path.Combine(Application.dataPath, "Editor", "Diagnostics", fileName);
        }

        RuntimeDiagnosticLogInspectorController CreateController()
        {
            GameObject gameObject = new GameObject("RuntimeDiagnosticLogInspectorControllerTests");
            createdGameObjects.Add(gameObject);
            return gameObject.AddComponent<RuntimeDiagnosticLogInspectorController>();
        }

        static void SetSerializedChannel(SerializedProperty channel, string key, bool enabled)
        {
            channel.FindPropertyRelative("key").stringValue = key;
            channel.FindPropertyRelative("enabled").boolValue = enabled;
        }

        static int CountChannel(RuntimeDiagnosticLogInspectorController controller, string key)
        {
            int count = 0;
            for (int i = 0; i < controller.Channels.Count; i++)
            {
                if (controller.Channels[i].Key == key)
                    count++;
            }

            return count;
        }

        static RuntimeDiagnosticLogChannelToggle FindChannel(RuntimeDiagnosticLogInspectorController controller, string key)
        {
            for (int i = 0; i < controller.Channels.Count; i++)
            {
                if (controller.Channels[i].Key == key)
                    return controller.Channels[i];
            }

            Assert.Fail($"Missing channel {key}");
            return default;
        }
    }
}
