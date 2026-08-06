using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public enum StairTraversalDiagnosticSeverity : byte
    {
        Info = 0,
        Error = 1
    }

    public readonly struct StairTraversalDiagnostic
    {
        public StairTraversalDiagnostic(
            StairTraversalDiagnosticSeverity severity,
            string code,
            string stairIdentity,
            string objectPath,
            string message)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            StairIdentity = stairIdentity ?? string.Empty;
            ObjectPath = objectPath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public StairTraversalDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string StairIdentity { get; }
        public string ObjectPath { get; }
        public string Message { get; }
    }

    public sealed class StairTraversalSurfaceValidationReport
    {
        readonly List<StairTraversalDiagnostic> m_Diagnostics = new List<StairTraversalDiagnostic>();

        internal StairTraversalSurfaceValidationReport(StairTraversalSurfaceAuthoring authoring)
        {
            Authoring = authoring;
            StairIdentity = authoring && !string.IsNullOrWhiteSpace(authoring.StairIdentity)
                ? authoring.StairIdentity.Trim()
                : "<empty>";
            ObjectPath = authoring ? StairTraversalSurfaceValidator.HierarchyPath(authoring.transform.root, authoring.transform) : "<missing>";
        }

        public StairTraversalSurfaceAuthoring Authoring { get; }
        public string StairIdentity { get; }
        public string ObjectPath { get; }
        public IReadOnlyList<StairTraversalDiagnostic> Diagnostics => m_Diagnostics;
        public bool HasErrors => m_Diagnostics.Any(value => value.Severity == StairTraversalDiagnosticSeverity.Error);
        public string RampColliderPath { get; internal set; } = "<missing>";
        public int RampLayer { get; internal set; } = -1;
        public CharacterSurfaceRole RampRole { get; internal set; } = CharacterSurfaceRole.Unknown;
        public string RampOwnerPath { get; internal set; } = "<none>";
        public int FootSurfaceColliderCount { get; internal set; }
        public string FootSurfaceLayers { get; internal set; } = "<none>";
        public int FootSurfaceFixedOwnerBindings { get; internal set; }
        public float LowerTransitionError { get; internal set; } = float.NaN;
        public float UpperTransitionError { get; internal set; } = float.NaN;
        public float RampWidth { get; internal set; } = float.NaN;
        public float FootSurfaceWidth { get; internal set; } = float.NaN;
        public float DirectionErrorDegrees { get; internal set; } = float.NaN;

        internal void Error(string code, UnityEngine.Object target, string message)
        {
            string path = target is Component component
                ? StairTraversalSurfaceValidator.HierarchyPath(component.transform.root, component.transform)
                : ObjectPath;
            m_Diagnostics.Add(new StairTraversalDiagnostic(
                StairTraversalDiagnosticSeverity.Error,
                code,
                StairIdentity,
                path,
                message));
        }
    }

    public sealed class StairTraversalWorldValidationReport
    {
        readonly StairTraversalSurfaceValidationReport[] m_Stairs;

        internal StairTraversalWorldValidationReport(StairTraversalSurfaceValidationReport[] stairs)
        {
            m_Stairs = stairs ?? Array.Empty<StairTraversalSurfaceValidationReport>();
        }

        public IReadOnlyList<StairTraversalSurfaceValidationReport> Stairs => m_Stairs;
        public bool HasErrors => m_Stairs.Any(value => value.HasErrors);

        public string FormatErrors()
        {
            return string.Join(
                Environment.NewLine,
                m_Stairs
                    .SelectMany(value => value.Diagnostics)
                    .Where(value => value.Severity == StairTraversalDiagnosticSeverity.Error)
                    .Select(value => $"{value.Code} | stair={value.StairIdentity} | path={value.ObjectPath} | {value.Message}"));
        }
    }

    public readonly struct StairSurfaceLayerResolution
    {
        public StairSurfaceLayerResolution(int ground, int traversal, int footPlacement)
        {
            Ground = ground;
            Traversal = traversal;
            FootPlacement = footPlacement;
        }

        public int Ground { get; }
        public int Traversal { get; }
        public int FootPlacement { get; }
    }

    public static class StairSurfaceLayerResolver
    {
        public static bool TryResolve(out StairSurfaceLayerResolution resolution, out string error)
        {
            int ground = LayerMask.NameToLayer(CharacterSurfaceLayerRoles.GroundName);
            int traversal = LayerMask.NameToLayer(CharacterSurfaceLayerRoles.CharacterTraversalName);
            int foot = LayerMask.NameToLayer(CharacterSurfaceLayerRoles.FootPlacementSurfaceName);
            resolution = new StairSurfaceLayerResolution(ground, traversal, foot);
            if (ground < 0 || traversal < 0 || foot < 0)
            {
                error = $"Required layers are missing. {CharacterSurfaceLayerRoles.GroundName}={ground}, {CharacterSurfaceLayerRoles.CharacterTraversalName}={traversal}, {CharacterSurfaceLayerRoles.FootPlacementSurfaceName}={foot}.";
                return false;
            }
            if (ground != CharacterSurfaceLayerRoles.GroundLayer ||
                traversal != CharacterSurfaceLayerRoles.CharacterTraversalLayer ||
                foot != CharacterSurfaceLayerRoles.FootPlacementSurfaceLayer)
            {
                error = $"Layer indices do not match the stable runtime contract. Ground={ground}/{CharacterSurfaceLayerRoles.GroundLayer}, CharacterTraversal={traversal}/{CharacterSurfaceLayerRoles.CharacterTraversalLayer}, FootPlacementSurface={foot}/{CharacterSurfaceLayerRoles.FootPlacementSurfaceLayer}.";
                return false;
            }
            if (ground == traversal || ground == foot || traversal == foot)
            {
                error = $"Surface roles resolve to duplicate layer indices. Ground={ground}, CharacterTraversal={traversal}, FootPlacementSurface={foot}.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public static StairSurfaceLayerResolution ResolveRequired()
        {
            if (!TryResolve(out StairSurfaceLayerResolution value, out string error))
                throw new InvalidOperationException(error);
            return value;
        }
    }

    public static class StairTraversalSurfaceValidator
    {
        public const float EndpointTolerance = 0.03f;
        public const float WidthTolerance = 0.02f;
        public const float DirectionToleranceDegrees = 1f;
        public const float TransitionSupportTolerance = 0.06f;

        public static StairTraversalWorldValidationReport ValidateWorld(DeterministicCollisionWorldAuthoring world)
        {
            if (!world)
                throw new ArgumentNullException(nameof(world));
            StairTraversalSurfaceAuthoring[] stairs = world
                .GetComponentsInChildren<StairTraversalSurfaceAuthoring>(true)
                .Where(value => value)
                .OrderBy(value => value.StairIdentity?.Trim(), StringComparer.Ordinal)
                .ThenBy(value => HierarchyPath(world.transform, value.transform), StringComparer.Ordinal)
                .ToArray();
            if (stairs.Length == 0)
                return new StairTraversalWorldValidationReport(Array.Empty<StairTraversalSurfaceValidationReport>());
            return Validate(stairs, world.transform);
        }

        public static StairTraversalSurfaceValidationReport ValidateSingle(StairTraversalSurfaceAuthoring authoring)
        {
            if (!authoring)
                throw new ArgumentNullException(nameof(authoring));
            StairTraversalWorldValidationReport world = Validate(
                authoring.transform.root.GetComponentsInChildren<StairTraversalSurfaceAuthoring>(true),
                authoring.transform.root);
            return world.Stairs.First(value => value.Authoring == authoring);
        }

        internal static float MeasureFootSurfaceWidth(Transform root, Vector3 right)
        {
            Collider[] colliders = root
                .GetComponentsInChildren<Collider>(true)
                .Where(value => value && value.enabled && value.gameObject.activeInHierarchy && !value.isTrigger)
                .ToArray();
            return MeasureProjectedWidth(colliders, right);
        }

        internal static string HierarchyPath(Transform root, Transform value)
        {
            if (!root || !value)
                return "<missing>";
            var names = new Stack<string>();
            Transform current = value;
            while (current)
            {
                names.Push(current.name);
                if (current == root)
                    return string.Join("/", names);
                current = current.parent;
            }
            return value.name;
        }

        static StairTraversalWorldValidationReport Validate(
            IEnumerable<StairTraversalSurfaceAuthoring> values,
            Transform contextRoot)
        {
            StairTraversalSurfaceAuthoring[] stairs = values
                .Where(value => value)
                .OrderBy(value => value.StairIdentity?.Trim(), StringComparer.Ordinal)
                .ThenBy(value => HierarchyPath(contextRoot, value.transform), StringComparer.Ordinal)
                .ToArray();
            DeterministicCollisionSurfaceAuthoring[] sources = contextRoot
                .GetComponentsInChildren<DeterministicCollisionSurfaceAuthoring>(true)
                .Where(value => value && value.isActiveAndEnabled)
                .OrderBy(value => HierarchyPath(contextRoot, value.transform), StringComparer.Ordinal)
                .ToArray();
            var duplicateIdentities = new HashSet<string>(
                stairs
                    .Where(value => !string.IsNullOrWhiteSpace(value.StairIdentity))
                    .GroupBy(value => value.StairIdentity.Trim(), StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.Ordinal);
            bool layersValid = StairSurfaceLayerResolver.TryResolve(out StairSurfaceLayerResolution layers, out string layerError);
            string[] profileErrors = ValidateFootPlacementProfiles();
            var reports = new StairTraversalSurfaceValidationReport[stairs.Length];
            for (int i = 0; i < stairs.Length; i++)
            {
                StairTraversalSurfaceAuthoring stair = stairs[i];
                var report = new StairTraversalSurfaceValidationReport(stair);
                reports[i] = report;
                ValidateStructure(stair, contextRoot, sources, duplicateIdentities, layersValid, layers, layerError, profileErrors, report);
            }
            return new StairTraversalWorldValidationReport(reports);
        }

        static void ValidateStructure(
            StairTraversalSurfaceAuthoring stair,
            Transform contextRoot,
            IReadOnlyList<DeterministicCollisionSurfaceAuthoring> sources,
            HashSet<string> duplicateIdentities,
            bool layersValid,
            StairSurfaceLayerResolution layers,
            string layerError,
            IReadOnlyList<string> profileErrors,
            StairTraversalSurfaceValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(stair.StairIdentity))
                report.Error("stair_identity_empty", stair, "Stair identity is empty; required non-empty stable identity.");
            else if (!string.Equals(stair.StairIdentity, stair.StairIdentity.Trim(), StringComparison.Ordinal))
                report.Error("stair_identity_not_canonical", stair, $"Stair identity '{stair.StairIdentity}' contains leading or trailing whitespace.");
            else if (duplicateIdentities.Contains(stair.StairIdentity.Trim()))
                report.Error("stair_identity_duplicate", stair, $"Stair identity '{stair.StairIdentity.Trim()}' is duplicated in authoring context '{contextRoot.name}'.");
            if (!stair.isActiveAndEnabled || !stair.gameObject.activeInHierarchy)
                report.Error("stair_authoring_inactive", stair, "Stair authoring is inactive and cannot prove the baked surface contract.");
            if (!layersValid)
                report.Error("stair_layer_contract_invalid", stair, layerError);
            for (int i = 0; i < profileErrors.Count; i++)
                report.Error("stair_foot_profile_mask_invalid", stair, profileErrors[i]);

            BoxCollider ramp = stair.TraversalRampCollider;
            Transform footRoot = stair.FootSurfaceRoot;
            Transform lower = stair.LowerTransition;
            Transform upper = stair.UpperTransition;
            if (!ramp)
                report.Error("stair_ramp_missing", stair, "Traversal Ramp BoxCollider reference is missing.");
            if (!footRoot)
                report.Error("stair_foot_surface_missing", stair, "Foot Placement Surface root reference is missing.");
            if (!lower)
                report.Error("stair_lower_transition_missing", stair, "Lower Transition reference is missing.");
            if (!upper)
                report.Error("stair_upper_transition_missing", stair, "Upper Transition reference is missing.");

            ValidateContext(stair, contextRoot, ramp ? ramp.transform : null, "stair_ramp_context_invalid", "Traversal Ramp", report);
            ValidateContext(stair, contextRoot, footRoot, "stair_foot_surface_context_invalid", "Foot Surface", report);
            ValidateContext(stair, contextRoot, lower, "stair_lower_transition_context_invalid", "Lower Transition", report);
            ValidateContext(stair, contextRoot, upper, "stair_upper_transition_context_invalid", "Upper Transition", report);

            Collider[] footColliders = Array.Empty<Collider>();
            if (footRoot)
            {
                footColliders = footRoot
                    .GetComponentsInChildren<Collider>(true)
                    .Where(value => value && value.enabled && value.gameObject.activeInHierarchy)
                    .OrderBy(value => HierarchyPath(footRoot, value.transform), StringComparer.Ordinal)
                    .ToArray();
                report.FootSurfaceColliderCount = footColliders.Length;
                report.FootSurfaceLayers = footColliders.Length == 0
                    ? "<none>"
                    : string.Join(", ", footColliders
                        .Select(value => $"{value.gameObject.layer}:{CharacterSurfaceLayerRoles.ResolveRole(value.gameObject.layer)}")
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal));
                report.FootSurfaceFixedOwnerBindings = footColliders.Sum(value => Owners(value, sources).Length);
                if (footColliders.Length == 0)
                    report.Error("stair_foot_surface_empty", footRoot, "Foot Surface root has 0 active Colliders; required at least 1.");
                for (int i = 0; i < footColliders.Length; i++)
                    ValidateFootCollider(stair, footColliders[i], ramp, sources, layersValid, layers, report);
            }

            if (ramp)
                ValidateRamp(stair, ramp, footRoot, footColliders, contextRoot, sources, layersValid, layers, lower, upper, report);
        }

        static void ValidateContext(
            StairTraversalSurfaceAuthoring stair,
            Transform contextRoot,
            Transform target,
            string code,
            string role,
            StairTraversalSurfaceValidationReport report)
        {
            if (!target)
                return;
            bool sameRoot = target.root == contextRoot;
            bool sameScene = target.gameObject.scene.handle == stair.gameObject.scene.handle;
            if (!sameRoot || !sameScene)
                report.Error(code, target, $"{role} '{target.name}' is outside authoring root '{contextRoot.name}' or scene handle '{stair.gameObject.scene.handle}'.");
        }

        static void ValidateFootCollider(
            StairTraversalSurfaceAuthoring stair,
            Collider collider,
            BoxCollider ramp,
            IReadOnlyList<DeterministicCollisionSurfaceAuthoring> sources,
            bool layersValid,
            StairSurfaceLayerResolution layers,
            StairTraversalSurfaceValidationReport report)
        {
            if (collider.isTrigger)
                report.Error("stair_foot_surface_trigger", collider, $"Foot Surface Collider '{collider.name}' is a trigger; required false.");
            if (layersValid && collider.gameObject.layer != layers.FootPlacement)
                report.Error("stair_foot_surface_layer_invalid", collider, $"Foot Surface Collider layer is {collider.gameObject.layer}; required {layers.FootPlacement} ({CharacterSurfaceLayerRoles.FootPlacementSurfaceName}).");
            DeterministicCollisionSurfaceAuthoring[] owners = Owners(collider, sources);
            if (owners.Length != 0)
                report.Error("stair_foot_surface_fixed_owner", collider, $"Foot Surface Collider has {owners.Length} deterministic owner(s): {string.Join(", ", owners.Select(value => value.SurfaceIdentity))}; required 0.");
            if (ramp && collider == ramp)
                report.Error("stair_surface_role_collision", collider, "The same Collider is referenced as Traversal Ramp and Foot Surface.");
        }

        static void ValidateRamp(
            StairTraversalSurfaceAuthoring stair,
            BoxCollider ramp,
            Transform footRoot,
            Collider[] footColliders,
            Transform contextRoot,
            IReadOnlyList<DeterministicCollisionSurfaceAuthoring> sources,
            bool layersValid,
            StairSurfaceLayerResolution layers,
            Transform lower,
            Transform upper,
            StairTraversalSurfaceValidationReport report)
        {
            report.RampColliderPath = HierarchyPath(contextRoot, ramp.transform);
            report.RampLayer = ramp.gameObject.layer;
            report.RampRole = CharacterSurfaceLayerRoles.ResolveRole(report.RampLayer);
            if (!ramp.enabled || !ramp.gameObject.activeInHierarchy)
                report.Error("stair_ramp_inactive", ramp, "Traversal Ramp is inactive; required enabled and active.");
            if (ramp.isTrigger)
                report.Error("stair_ramp_trigger", ramp, "Traversal Ramp is a trigger; required false.");
            Renderer[] renderers = ramp.GetComponents<Renderer>();
            if (renderers.Length != 0)
                report.Error("stair_ramp_renderer_present", ramp, $"Traversal Ramp GameObject has {renderers.Length} Renderer component(s); required 0.");
            if (layersValid && ramp.gameObject.layer != layers.Traversal)
                report.Error("stair_ramp_layer_invalid", ramp, $"Traversal Ramp layer is {ramp.gameObject.layer}; required {layers.Traversal} ({CharacterSurfaceLayerRoles.CharacterTraversalName}).");
            if (footRoot && (ramp.transform == footRoot || ramp.transform.IsChildOf(footRoot)))
                report.Error("stair_ramp_inside_foot_surface", ramp, $"Traversal Ramp is inside Foot Surface root '{footRoot.name}'; required disjoint subtrees.");
            DeterministicCollisionSurfaceAuthoring[] owners = Owners(ramp, sources);
            report.RampOwnerPath = owners.Length == 1
                ? HierarchyPath(contextRoot, owners[0].transform)
                : $"<{owners.Length} owners>";
            if (owners.Length != 1)
                report.Error("stair_ramp_fixed_owner_invalid", ramp, $"Traversal Ramp has {owners.Length} deterministic owner(s); required exactly 1. Owners={string.Join(", ", owners.Select(value => value.SurfaceIdentity))}.");
            if (!lower || !upper)
                return;

            Vector3 rampLower = RampTopEndpoint(ramp, false);
            Vector3 rampUpper = RampTopEndpoint(ramp, true);
            report.LowerTransitionError = Vector3.Distance(rampLower, lower.position);
            report.UpperTransitionError = Vector3.Distance(rampUpper, upper.position);
            if (report.LowerTransitionError > EndpointTolerance)
            {
                Vector3 delta = rampLower - lower.position;
                report.Error("stair_ramp_lower_endpoint_mismatch", ramp, $"Ramp lower endpoint={rampLower}, transition={lower.position}, horizontalError={new Vector2(delta.x, delta.z).magnitude:0.####}m, heightError={Mathf.Abs(delta.y):0.####}m, limit={EndpointTolerance:0.####}m.");
            }
            if (report.UpperTransitionError > EndpointTolerance)
            {
                Vector3 delta = rampUpper - upper.position;
                report.Error("stair_ramp_upper_endpoint_mismatch", ramp, $"Ramp upper endpoint={rampUpper}, transition={upper.position}, horizontalError={new Vector2(delta.x, delta.z).magnitude:0.####}m, heightError={Mathf.Abs(delta.y):0.####}m, limit={EndpointTolerance:0.####}m.");
            }
            Vector3 transitionDirection = upper.position - lower.position;
            Vector3 rampDirection = rampUpper - rampLower;
            if (transitionDirection.y <= EndpointTolerance)
                report.Error("stair_transition_height_order_invalid", stair, $"Upper Transition Y={upper.position.y:0.####} must exceed Lower Transition Y={lower.position.y:0.####} by more than {EndpointTolerance:0.####}m.");
            report.DirectionErrorDegrees = transitionDirection.sqrMagnitude > 0.000001f && rampDirection.sqrMagnitude > 0.000001f
                ? Vector3.Angle(rampDirection, transitionDirection)
                : float.PositiveInfinity;
            if (report.DirectionErrorDegrees > DirectionToleranceDegrees)
                report.Error("stair_ramp_direction_mismatch", ramp, $"Ramp direction={rampDirection.normalized}, stair ascent direction={transitionDirection.normalized}, error={report.DirectionErrorDegrees:0.####}deg, limit={DirectionToleranceDegrees:0.####}deg.");

            report.RampWidth = Vector3.Distance(
                ramp.transform.TransformPoint(ramp.center - Vector3.right * ramp.size.x * 0.5f),
                ramp.transform.TransformPoint(ramp.center + Vector3.right * ramp.size.x * 0.5f));
            report.FootSurfaceWidth = MeasureProjectedWidth(footColliders, ramp.transform.right.normalized);
            if (report.FootSurfaceWidth > 0f && report.RampWidth + WidthTolerance < report.FootSurfaceWidth)
                report.Error("stair_ramp_width_insufficient", ramp, $"Ramp width={report.RampWidth:0.####}m, Foot Surface walkable width={report.FootSurfaceWidth:0.####}m, tolerance={WidthTolerance:0.####}m.");

            if (layersValid)
            {
                ValidateTransitionSupport(stair, contextRoot, ramp, footRoot, lower, "lower", layers.Ground, report);
                ValidateTransitionSupport(stair, contextRoot, ramp, footRoot, upper, "upper", layers.Ground, report);
            }
        }

        static void ValidateTransitionSupport(
            StairTraversalSurfaceAuthoring stair,
            Transform contextRoot,
            BoxCollider ramp,
            Transform footRoot,
            Transform transition,
            string role,
            int groundLayer,
            StairTraversalSurfaceValidationReport report)
        {
            Collider[] supports = contextRoot
                .GetComponentsInChildren<Collider>(true)
                .Where(value => value && value != ramp && value.enabled && value.gameObject.activeInHierarchy &&
                                !value.isTrigger && value.gameObject.layer == groundLayer &&
                                (!footRoot || (value.transform != footRoot && !value.transform.IsChildOf(footRoot))) &&
                                ContainsHorizontal(value.bounds, transition.position, TransitionSupportTolerance) &&
                                Mathf.Abs(value.bounds.max.y - transition.position.y) <= TransitionSupportTolerance)
                .OrderBy(value => Mathf.Abs(value.bounds.max.y - transition.position.y))
                .ThenBy(value => HierarchyPath(contextRoot, value.transform), StringComparer.Ordinal)
                .ToArray();
            if (supports.Length == 0)
            {
                report.Error($"stair_{role}_transition_support_missing", transition, $"{role} transition={transition.position} has no Ground support within horizontal/height tolerance {TransitionSupportTolerance:0.####}m.");
                return;
            }
            if (supports.Length > 1)
                report.Error($"stair_{role}_transition_support_ambiguous", transition, $"{role} transition={transition.position} has {supports.Length} Ground supports within tolerance {TransitionSupportTolerance:0.####}m: {string.Join(", ", supports.Select(value => value.name))}.");
        }

        static string[] ValidateFootPlacementProfiles()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterFootPlacementProfile");
            if (guids.Length == 0)
                return new[] { "Project contains 0 CharacterFootPlacementProfile assets; required at least 1 formal Foot Placement consumer." };
            var errors = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterFootPlacementProfile profile = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementProfile>(path);
                if (!profile)
                {
                    errors.Add($"Foot Placement Profile '{path}' could not be loaded.");
                    continue;
                }
                int mask = profile.FinalIkGrounding.GroundLayerMask;
                bool ground = (mask & CharacterSurfaceLayerRoles.GroundMask) != 0;
                bool foot = (mask & CharacterSurfaceLayerRoles.FootPlacementSurfaceMask) != 0;
                bool traversal = (mask & CharacterSurfaceLayerRoles.CharacterTraversalMask) != 0;
                if (!ground || !foot || traversal)
                    errors.Add($"Foot Placement Profile '{path}' mask={mask} requires Ground=true, FootPlacementSurface=true, CharacterTraversal=false; actual {ground}/{foot}/{traversal}.");
            }
            return errors.ToArray();
        }

        static DeterministicCollisionSurfaceAuthoring[] Owners(
            Collider collider,
            IReadOnlyList<DeterministicCollisionSurfaceAuthoring> sources)
        {
            return sources
                .Where(value => collider.transform == value.transform || collider.transform.IsChildOf(value.transform))
                .ToArray();
        }

        static Vector3 RampTopEndpoint(BoxCollider ramp, bool upper)
        {
            Vector3 half = ramp.size * 0.5f;
            return ramp.transform.TransformPoint(ramp.center + new Vector3(0f, half.y, upper ? half.z : -half.z));
        }

        static float MeasureProjectedWidth(IReadOnlyList<Collider> colliders, Vector3 axis)
        {
            if (colliders == null || colliders.Count == 0 || axis.sqrMagnitude <= 0.000001f)
                return 0f;
            axis.Normalize();
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            for (int i = 0; i < colliders.Count; i++)
            {
                Bounds bounds = colliders[i].bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    float projection = Vector3.Dot(point, axis);
                    minimum = Mathf.Min(minimum, projection);
                    maximum = Mathf.Max(maximum, projection);
                }
            }
            return maximum - minimum;
        }

        static bool ContainsHorizontal(Bounds bounds, Vector3 point, float tolerance)
        {
            return point.x >= bounds.min.x - tolerance && point.x <= bounds.max.x + tolerance &&
                   point.z >= bounds.min.z - tolerance && point.z <= bounds.max.z + tolerance;
        }
    }
}
