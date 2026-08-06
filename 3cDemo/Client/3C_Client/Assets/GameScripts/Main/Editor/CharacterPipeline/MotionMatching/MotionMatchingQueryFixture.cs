using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    [Serializable]
    public sealed class MotionMatchingQueryFixtureTrajectoryPoint
    {
        [Min(0f)] public float TimeOffset;
        public Vector2 LocalPositionCenter;
        public Vector2 LocalFacingCenter = Vector2.up;
        [Min(0f)] public float PositionToleranceRadius;
        [Min(0f)] public float FacingToleranceDegrees;
        [Range(0f, 1f)] public float Confidence = 1f;

        public MotionMatchingTrajectoryEnvelopePoint Build() =>
            new MotionMatchingTrajectoryEnvelopePoint(
                TimeOffset,
                LocalPositionCenter,
                LocalFacingCenter,
                PositionToleranceRadius,
                FacingToleranceDegrees,
                Confidence);
    }

    [CreateAssetMenu(
        fileName = "MotionMatchingQueryFixture",
        menuName = "3C/Character/Motion Matching/Query Fixture")]
    public sealed class MotionMatchingQueryFixture : ScriptableObject
    {
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_ProviderId = string.Empty;
        [SerializeField] string m_DatabaseId = string.Empty;
        [SerializeField] string m_TrajectorySourceId = "editor-query-fixture";
        [SerializeField, Min(1)] long m_SourceTick = 1;
        [SerializeField, Min(1)] long m_SourceSequence = 1;
        [SerializeField, Min(0f)] float m_SourceAge;
        [SerializeField] List<MotionMatchingQueryFixtureTrajectoryPoint> m_Trajectory = new List<MotionMatchingQueryFixtureTrajectoryPoint>();
        [SerializeField] float[] m_NormalizedFeatures = Array.Empty<float>();
        [SerializeField] MotionMatchingFootContactMask m_ProtectedContact;
        [SerializeField] Vector3 m_LeftRootPosition;
        [SerializeField] Vector3 m_RightRootPosition;
        [SerializeField] Vector3 m_LeftRootVelocity;
        [SerializeField] Vector3 m_RightRootVelocity;
        [SerializeField] bool m_Initialization = true;
        [SerializeField, Min(0f)] float m_SecondsSinceLastJump;
        [SerializeField, Min(0)] int m_CurrentSampleIndex;
        [SerializeField, Min(1)] long m_CurrentGeneration = 1;
        [SerializeField, Min(1)] long m_CurrentPlanId = 1;
        [SerializeField] long m_ResetSequence;

        internal CharacterPipelineDefinition Definition => m_Definition;
        internal string ProviderId => string.IsNullOrWhiteSpace(m_ProviderId)
            ? string.Empty
            : m_ProviderId.Trim();

        public MotionMatchingSearchReplayArtifact Execute()
        {
            CharacterPresentationProjection projection = LoadProjection();
            MotionMatchingProjectionPayload motionMatching = projection.MotionMatching ??
                throw new InvalidOperationException("Query Fixture Definition has no Motion Matching payload.");
            int databaseIndex = FindDatabase(motionMatching);
            RequireProviderBinding(projection, databaseIndex);
            using var database = new CharacterMotionMatchingRuntimeDatabase(motionMatching, databaseIndex);
            if (m_Trajectory == null || m_Trajectory.Count != motionMatching.TrajectoryPolicy.PointCount)
                throw new InvalidOperationException($"Query Fixture requires exactly {motionMatching.TrajectoryPolicy.PointCount} trajectory points.");
            if (m_NormalizedFeatures == null || m_NormalizedFeatures.Length != database.Capacities.DenseFeatureCount)
                throw new InvalidOperationException($"Query Fixture requires exactly {database.Capacities.DenseFeatureCount} normalized features.");
            if (m_SourceTick <= 0 || m_SourceSequence <= 0 || m_CurrentGeneration <= 0 || m_CurrentPlanId <= 0 || m_ResetSequence < 0)
                throw new InvalidOperationException("Query Fixture identities must be positive and Reset Sequence must be non-negative.");
            var envelope = new MotionMatchingTrajectoryEnvelope(m_Trajectory.Count);
            envelope.RestoreIdentity(
                new MotionMatchingTrajectorySourceIdentity(m_TrajectorySourceId),
                new SimulationTick((ulong)m_SourceTick),
                (ulong)m_SourceSequence,
                m_SourceAge,
                (ulong)m_ResetSequence);
            for (int i = 0; i < m_Trajectory.Count; i++)
                envelope.Add(m_Trajectory[i].Build());
            MotionMatchingSelectionIdentity currentSelection = default;
            if (!m_Initialization)
            {
                if ((uint)m_CurrentSampleIndex >= (uint)database.SampleCount)
                    throw new InvalidOperationException("Query Fixture current sample index is outside the selected Database.");
                currentSelection = new MotionMatchingSelectionIdentity(
                    database.ArtifactIdentity,
                    new MotionMatchingSelectionGeneration((ulong)m_CurrentGeneration),
                    new CharacterMotionMatchingPlanId((ulong)m_CurrentPlanId),
                    database.GetSample(m_CurrentSampleIndex).SampleId,
                    m_CurrentSampleIndex);
            }
            var query = new MotionMatchingQuery(
                new CharacterMotionMatchingQueryId(1),
                motionMatching.ProfileId,
                database.ArtifactIdentity,
                database.SearchDomainId,
                new MotionMatchingTrajectorySourceIdentity(m_TrajectorySourceId),
                envelope,
                new MotionMatchingFloatBuffer(m_NormalizedFeatures, 0, m_NormalizedFeatures.Length),
                new MotionMatchingContactProtection(
                    m_ProtectedContact,
                    m_LeftRootPosition,
                    m_RightRootPosition,
                    m_LeftRootVelocity,
                    m_RightRootVelocity),
                currentSelection,
                m_Initialization,
                m_SecondsSinceLastJump,
                (ulong)m_ResetSequence);
            var search = new MotionMatchingExactSearch(database);
            MotionMatchingSearchResult searchResult = search.Search(query);
            var plan = new MotionMatchingPlanEvaluator(database);
            MotionMatchingPlanEvaluationResult planResult = plan.Evaluate(query, searchResult);
            if (!planResult.IsValid)
                throw new InvalidOperationException($"Query Fixture produced no valid plan: {planResult.InvalidReason}.");
            return MotionMatchingSearchReplayArtifact.Capture(
                ProjectionIdentity(projection),
                database,
                query,
                searchResult,
                planResult);
        }

        CharacterPresentationProjection LoadProjection()
        {
            if (!m_Definition || !m_Definition.SimulationProgram || !m_Definition.PresentationProjection)
                throw new InvalidOperationException("Query Fixture requires one Character Definition with compiled Program and Presentation Projection.");
            CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
            return m_Definition.PresentationProjection.Load(
                Float32CharacterPresentationContractAdapter.Create(program));
        }

        int FindDatabase(MotionMatchingProjectionPayload projection)
        {
            if (string.IsNullOrWhiteSpace(m_DatabaseId))
                throw new InvalidOperationException("Query Fixture Database Id is missing.");
            for (int i = 0; i < projection.DatabaseCount; i++)
            {
                if (string.Equals(projection.GetDatabase(i).ArtifactIdentity.DatabaseId.Value, m_DatabaseId.Trim(), StringComparison.Ordinal))
                    return i;
            }
            throw new InvalidOperationException($"Query Fixture Database '{m_DatabaseId}' is absent from the selected Definition Projection.");
        }

        void RequireProviderBinding(
            CharacterPresentationProjection projection,
            int databaseIndex)
        {
            if (string.IsNullOrEmpty(ProviderId))
                throw new InvalidOperationException("Query Fixture Provider Id is missing.");
            if (!TryResolveMotionMatchingNodeBinding(
                    projection,
                    ProviderId,
                    out MotionMatchingNodeBindingPayload binding))
            {
                throw new InvalidOperationException(
                    "Query Fixture Provider Id is not compiled in the Motion Matching Pose Plan.");
            }
            if (databaseIndex >= binding.FirstDatabaseIndex &&
                databaseIndex < binding.FirstDatabaseIndex + binding.DatabaseCount)
            {
                return;
            }
            throw new InvalidOperationException("Query Fixture Database is not owned by the selected Motion Matching provider.");
        }

        internal static bool TryResolveMotionMatchingNodeBinding(
            CharacterPresentationProjection projection,
            string providerId,
            out MotionMatchingNodeBindingPayload binding)
        {
            binding = default;
            if (projection?.MotionMatching == null ||
                string.IsNullOrWhiteSpace(providerId))
                return false;
            PoseNodeId playerNodeId = default;
            int providerCount = 0;
            for (int machineIndex = 0;
                 machineIndex < projection.PosePlan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    projection.PosePlan.StateMachines[machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDescriptor state = machine.States[stateIndex];
                    for (int providerIndex = 0;
                         providerIndex < state.SourceProviders.Count;
                         providerIndex++)
                    {
                        PoseStateSourceProviderPlan plan =
                            state.SourceProviders[providerIndex];
                        if (plan == null ||
                            plan.SourceKind != AnimationPoseSourceKind.MotionMatching ||
                            !string.Equals(
                                plan.ProviderId.Value,
                                providerId,
                                StringComparison.Ordinal))
                            continue;
                        playerNodeId = plan.PlayerNodeId;
                        providerCount++;
                    }
                }
            }
            if (providerCount != 1 || !playerNodeId.IsValid)
                return false;
            int bindingCount = 0;
            for (int i = 0; i < projection.MotionMatching.NodeBindingCount; i++)
            {
                MotionMatchingNodeBindingPayload candidate =
                    projection.MotionMatching.GetNodeBinding(i);
                if (candidate.PoseNodeId != playerNodeId)
                    continue;
                binding = candidate;
                bindingCount++;
            }
            return bindingCount == 1;
        }

        static string ProjectionIdentity(CharacterPresentationProjection projection) =>
            $"{projection.ProgramId}@{projection.SourceRevision}:{projection.ContractHash}";
    }

    [CustomEditor(typeof(MotionMatchingQueryFixture))]
    public sealed class MotionMatchingQueryFixtureInspector : UnityEditor.Editor
    {
        MotionMatchingSearchReplayArtifact m_LastArtifact;
        MotionMatchingQueryPreviewAdapter m_PreviewSession;
        CharacterPipelineHost m_PreviewTarget;
        string m_LastPreviewStatus = string.Empty;

        void OnDisable()
        {
            m_PreviewSession?.Dispose();
            m_PreviewSession = null;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(
                "This Editor-only fixture is an explicit query input. It is not referenced by Runtime Profiles or Character assets and does not execute Program, World Solver, Foot Physics, or Camera.",
                MessageType.Info);
            m_PreviewTarget = (CharacterPipelineHost)EditorGUILayout.ObjectField(
                "Pose Preview Target",
                m_PreviewTarget,
                typeof(CharacterPipelineHost),
                true);
            if (GUILayout.Button("Run Formal Database Search And Plan"))
                RunFixture();
            if (GUILayout.Button("Run Formal Module Pose Preview"))
                RunPosePreview();
            using (new EditorGUI.DisabledScope(m_LastArtifact == null))
            {
                if (GUILayout.Button("Save Search Replay Artifact"))
                    SaveArtifact();
            }
            if (m_LastArtifact != null)
            {
                EditorGUILayout.LabelField("Expected Digest", m_LastArtifact.ExpectedDigest.ToString());
                EditorGUILayout.LabelField("Database", m_LastArtifact.DatabaseIdentity.DatabaseId.Value);
                EditorGUILayout.LabelField("Projection", m_LastArtifact.ProjectionIdentity);
            }
            if (!string.IsNullOrEmpty(m_LastPreviewStatus))
                EditorGUILayout.HelpBox(m_LastPreviewStatus, MessageType.Info);
        }

        void RunFixture()
        {
            try
            {
                m_LastArtifact = ((MotionMatchingQueryFixture)target).Execute();
            }
            catch (Exception exception)
            {
                m_LastArtifact = null;
                EditorUtility.DisplayDialog("Motion Matching Query Fixture Failed", exception.Message, "OK");
            }
        }

        void SaveArtifact()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Motion Matching Search Replay",
                $"{target.name}-search-replay",
                "bytes",
                "Choose the Search Replay Artifact path.");
            if (string.IsNullOrEmpty(path))
                return;
            File.WriteAllBytes(Path.GetFullPath(path), MotionMatchingSearchReplayArtifactCodec.Encode(m_LastArtifact));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        void RunPosePreview()
        {
            try
            {
                if (!m_PreviewTarget)
                    throw new InvalidOperationException("Query Fixture Pose Preview requires an explicit scene CharacterPipelineHost target.");
                MotionMatchingQueryFixture fixture = (MotionMatchingQueryFixture)target;
                MotionMatchingSearchReplayArtifact artifact = fixture.Execute();
                m_PreviewSession?.Dispose();
                m_PreviewSession = new MotionMatchingQueryPreviewAdapter(fixture, m_PreviewTarget);
                ComposedAnimationPoseFrame finalPose = m_PreviewSession.Evaluate(artifact);
                m_LastArtifact = artifact;
                m_LastPreviewStatus = $"Pose preview completed through Module and compiled Pose Plan. Completion={finalPose.CompletionIdentity}";
            }
            catch (Exception exception)
            {
                m_PreviewSession?.Dispose();
                m_PreviewSession = null;
                m_LastPreviewStatus = string.Empty;
                EditorUtility.DisplayDialog("Motion Matching Query Fixture Preview Failed", exception.Message, "OK");
            }
        }
    }

    internal sealed class MotionMatchingQueryPreviewAdapter : IDisposable
    {
        readonly AnimationPreviewRuntime m_Playback;
        readonly string m_ProviderId;
        readonly Transform[] m_Bones;
        readonly Vector3[] m_LocalPositions;
        readonly Quaternion[] m_LocalRotations;
        readonly Vector3[] m_LocalScales;
        bool m_Disposed;

        internal MotionMatchingQueryPreviewAdapter(
            MotionMatchingQueryFixture fixture,
            CharacterPipelineHost target)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Motion Matching Query Fixture Pose Preview is Editor-only and cannot run during Play Mode.");
            if (fixture == null || !fixture.Definition || !target || target.Definition != fixture.Definition)
                throw new InvalidOperationException("Query Fixture and Pose Preview target must use the same explicit Character Definition.");
            if (!target.Animancer || !target.AnimationRigBinding || !target.WorldBodyBinding)
                throw new InvalidOperationException("Query Fixture Pose Preview target requires Animancer, Animation Rig Binding, and World Body Binding.");
            if (!fixture.Definition.SimulationProgram || !fixture.Definition.PresentationProjection)
                throw new InvalidOperationException("Query Fixture Pose Preview requires compiled Program and Presentation Projection.");

            CharacterSimulationProgram program =
                fixture.Definition.SimulationProgram.Load();
            CharacterPresentationSemanticContract contract =
                Float32CharacterPresentationContractAdapter.Create(program);
            CharacterPresentationProjection projection = fixture.Definition.PresentationProjection.Load(contract);
            m_ProviderId = fixture.ProviderId;
            if (!MotionMatchingQueryFixture.TryResolveMotionMatchingNodeBinding(
                    projection,
                    m_ProviderId,
                    out _))
                throw new InvalidOperationException("Query Fixture Pose Preview provider is not compiled in the Motion Matching Pose Plan.");
            target.AnimationRigBinding.RequireValid(projection.Rig);
            IReadOnlyList<Transform> sourceBones = target.AnimationRigBinding.PhysicalBones;
            m_Bones = new Transform[sourceBones.Count];
            m_LocalPositions = new Vector3[sourceBones.Count];
            m_LocalRotations = new Quaternion[sourceBones.Count];
            m_LocalScales = new Vector3[sourceBones.Count];
            for (int i = 0; i < sourceBones.Count; i++)
            {
                Transform bone = sourceBones[i];
                m_Bones[i] = bone;
                m_LocalPositions[i] = bone.localPosition;
                m_LocalRotations[i] = bone.localRotation;
                m_LocalScales[i] = bone.localScale;
            }
            m_Playback = new AnimationPreviewRuntime(
                fixture.Definition,
                program,
                projection,
                target.Animancer,
                target.AnimationRigBinding,
                CharacterPresentationBodyState.FromFloat32(target.WorldBodyBinding.InitialBody),
                target.WorldAwarePresentation,
                target.gameObject.scene.GetPhysicsScene(),
                target.EquipmentPreviewFixture,
                null,
                Guid.NewGuid());
        }

        internal ComposedAnimationPoseFrame Evaluate(MotionMatchingSearchReplayArtifact fixture)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(MotionMatchingQueryPreviewAdapter));
            ComposedAnimationPoseFrame finalPose =
                m_Playback.EvaluateMotionMatchingQuery(
                    m_ProviderId,
                    fixture);
            if (finalPose.Availability != AnimationPoseAvailability.Pose)
                throw new InvalidOperationException("Query Fixture Pose Preview did not produce a valid ComposedAnimationPoseFrame.");
            return finalPose;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            try
            {
                m_Playback.Dispose();
            }
            finally
            {
                for (int i = 0; i < m_Bones.Length; i++)
                {
                    Transform bone = m_Bones[i];
                    if (!bone)
                        continue;
                    bone.localPosition = m_LocalPositions[i];
                    bone.localRotation = m_LocalRotations[i];
                    bone.localScale = m_LocalScales[i];
                }
            }
        }

    }
}
