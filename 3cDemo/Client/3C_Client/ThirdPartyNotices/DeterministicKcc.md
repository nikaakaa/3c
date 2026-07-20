# Deterministic KCC References

The deterministic KCC runtime is an original Fixed Q32.32 implementation. No source code from the Unity Asset Store Kinematic Character Controller package is included or linked.

The following implementations and documents were reviewed for algorithm structure and behavior:

- Rapier/Parry, commit `c13133ad293ee70c7f9cec9e498eac016c362169`, Apache License 2.0: shape casting, closest-feature queries, kinematic movement stages, slopes, steps and ground snapping.
- NVIDIA PhysX, commit `b4b286abff6f2b3debd1d1acb120dc428765cf2e`, BSD 3-Clause License: character-controller contact offset, overlap recovery and collide-and-slide behavior.
- Philippe St-Amand's Kinematic Character Controller documentation and Unity release discussion: behavioral comparison only. Its Asset Store source and runtime assembly are not used by this project.

The project implementation uses its own portable collision artifact, Fixed Q32.32 arithmetic, canonical feature identities, bounded query buffers and rollback snapshot contracts.

