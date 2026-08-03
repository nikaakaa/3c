# Deterministic KCC References

The deterministic KCC runtime is implemented with project-owned Fixed Q32.32 types, a portable collision artifact, canonical feature identities, bounded query buffers, and rollback snapshot contracts. It does not link Unity Physics or a third-party KCC runtime assembly.

The primary movement-policy reference is Philippe St-Amand's Kinematic Character Controller. The locally reviewed reference is the `com.janooba.kcc` `1.0.1` refactor maintained by Gawidev/Janooba:

- `KinematicCharacterMotor.cs` SHA-256: `D7FEE8FA2D703A273DFF0CF67A64FF88A65531309A23429CC1A6BBF587440476`
- `package.json` SHA-256: `29752D03559951B9241EE9C900C092B281362A527D2D44FA9403E1375CB3A74F`
- Scope: movement sweep ordering, hit stability, grounding, step detection and commit, ledge/denivelation handling, ground probing, and remaining-movement continuation.

The local reference package is not tracked, copied into Runtime, listed in the Unity package manifest, or included in Player builds. The Fixed implementation re-expresses the reviewed behavior through project contracts instead of copying UnityEngine, Collider, Rigidbody, MonoBehaviour, or Unity Physics code.

Additional low-level collision references:

- Rapier/Parry, commit `c13133ad293ee70c7f9cec9e498eac016c362169`, Apache License 2.0: shape casting and closest-feature query structure.
- NVIDIA PhysX, commit `b4b286abff6f2b3debd1d1acb120dc428765cf2e`, BSD 3-Clause License: contact offset, overlap recovery, and collide-and-slide robustness.

OpenKCC is used only as a static test-course reference and is not a movement-policy or runtime source.
