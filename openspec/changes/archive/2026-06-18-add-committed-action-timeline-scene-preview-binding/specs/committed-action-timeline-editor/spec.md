## ADDED Requirements
### Requirement: Timeline Scene Preview Binding
Committed Action Timeline Editor MUST support an explicit Editor-only scene preview target binding for visual preview. The target MAY be a scene GameObject containing an Animator or an approved equivalent scene preview target. The binding MUST be a temporary editor preview binding and MUST NOT be saved into `ActionTimelineDefinition`, `CommittedActionBranchDefinition`, runtime snapshots, rollback data, or formal gameplay configuration.

#### Scenario: Bind scene character target
- **GIVEN** the designer has opened an independent Committed Action Timeline Editor
- **WHEN** the designer assigns a scene character GameObject with an Animator as preview target
- **THEN** the editor MUST report the target as bound
- **AND** visual preview MAY sample that target in EditMode
- **AND** the action definition and runtime timeline definition MUST NOT store the scene object reference

#### Scenario: Missing target remains data preview
- **WHEN** no preview target is assigned
- **THEN** Timeline preview MUST continue to show compiler / evaluator data results
- **AND** visual preview status MUST show an explicit unbound state
- **AND** the editor MUST NOT search the scene hierarchy, Resources, global singletons, or default prefabs as a hidden fallback

#### Scenario: Invalid target reports diagnostic
- **GIVEN** the designer assigns a preview target without an Animator or approved equivalent preview component
- **WHEN** the Timeline Editor refreshes preview binding
- **THEN** the editor MUST report a clear invalid target diagnostic
- **AND** MUST NOT silently bind another object

### Requirement: Timeline Visual Preview Uses Formal Evaluator Outcome
Visual preview MUST consume the same `CommittedActionBranchEvaluator` and `ActionTimelineEvaluator` outcome used by data preview. Visual preview MUST NOT decide selector branch, condition result, local tick, active clip, animation key, motion spec, window fact, or cue request from GraphView state, scene object state, Animator playback time, Unity frame delta, or Ref/Taco timeline state.

#### Scenario: Scrub samples evaluated animation key
- **GIVEN** a preview target is bound
- **AND** current local tick evaluates to an `ActionAnimationKey`
- **WHEN** the designer scrubs the timeline locator
- **THEN** the editor MUST first evaluate the formal action definition for that local tick
- **AND** visual preview MUST sample the animation resolved from the evaluated `ActionAnimationKey`
- **AND** the sampled pose MUST NOT come from an unevaluated editor-only selected clip

#### Scenario: Selector result controls visual timeline
- **GIVEN** a Committed Action branch has multiple TimelineNode paths
- **WHEN** formal evaluator selects TimelineNode A for the preview context
- **THEN** visual preview MUST use TimelineNode A's outcome
- **AND** TimelineNode B MUST NOT drive animation, motion, window, or cue preview for that tick

### Requirement: Timeline Preview Resolves Animation Through Formal Binding
Timeline visual preview MUST resolve `ActionAnimationKey` through the formal action animation binding entry, Action Animation Profile, Animancer TransitionLibrary, or approved equivalent presentation binding associated with the bound preview target. It MUST NOT store concrete `AnimationClip`, Animancer transition asset, or scene object reference in ActionTimeline runtime data.

#### Scenario: Resolve key to preview clip
- **GIVEN** the bound preview target has a formal animation binding where `Action.Dodge.Directional` resolves to a playable clip or transition
- **AND** the evaluated timeline outcome contains `Action.Dodge.Directional`
- **WHEN** visual preview samples the current tick
- **THEN** the preview resolver MUST resolve that key through the bound presentation configuration
- **AND** the preview session MAY sample the resolved clip in Editor-only code

#### Scenario: Missing animation binding is explicit
- **GIVEN** the evaluated timeline outcome contains an animation key
- **AND** the bound preview target cannot resolve that key
- **WHEN** visual preview refreshes
- **THEN** the editor MUST show a clear missing binding diagnostic
- **AND** MUST NOT guess a clip by name, asset search, Resources, scene scan, or Ref sample data

#### Scenario: Resolver does not play runtime presenter
- **WHEN** visual preview resolves an animation key
- **THEN** the resolver MUST NOT call the formal runtime presenter play method
- **AND** MUST NOT mutate action lifecycle, blackboard, motion executor, or CharacterFramePipeline state

### Requirement: Timeline Preview Samples Animator Through Editor-only PlayableGraph
Timeline visual preview MAY use an Editor-only PlayableGraph to sample the bound preview target's Animator. The graph MUST be owned by the Timeline Editor preview session, MUST be destroyed when preview stops, target changes, window closes, or domain reloads, and MUST NOT become the formal ActionTimeline runtime runner.

#### Scenario: Scrub evaluates pose without gameplay tick
- **GIVEN** a preview target and animation binding are valid
- **WHEN** the designer moves the preview locator to local tick N
- **THEN** the preview session MAY set the resolved clip time derived from tick N
- **AND** MAY evaluate the Editor-only graph to update the Animator pose
- **AND** MUST NOT tick Action lifecycle, CharacterFramePipeline, motion executor, hitbox logic, VFX, SFX, or camera systems

#### Scenario: Preview cleanup restores ownership
- **GIVEN** a visual preview graph is active
- **WHEN** the designer clears the target, closes the window, stops preview, or Unity reloads domain
- **THEN** the preview session MUST destroy its graph
- **AND** MUST release Animator ownership and restore required target state or approved equivalent preview-safe state

#### Scenario: Ref PlayableGraph stays editor-only
- **WHEN** checking formal runtime assemblies
- **THEN** runtime MUST NOT reference Ref/Taco `TimelinePlayer`
- **AND** MUST NOT reference the Timeline Editor preview session
- **AND** MUST NOT use `PlayableGraph` as the ActionTimeline gameplay execution path

### Requirement: Timeline Scene Preview Does Not Execute Gameplay Effects
Timeline scene preview MUST keep non-animation clips as editor diagnostics in the first version. Motion clips MAY display motion spec, direction, duration, distance, warp payload, or a preview ghost/path, but MUST NOT call the formal motion executor. Window and Cue clips MAY be highlighted and listed, but MUST NOT trigger hit detection, damage, VFX, SFX, camera events, post-processing, or runtime blackboard writes.

#### Scenario: Motion preview is diagnostic
- **GIVEN** the evaluated outcome contains a Motion clip
- **WHEN** visual preview refreshes
- **THEN** the editor MAY display motion distance, duration, rotate-to-direction, and warp payload diagnostics
- **AND** MUST NOT move the bound character through `CharacterMotionDriver`, `CharacterController.Move`, root motion application, or motion warping solver

#### Scenario: Window and cue preview are diagnostic
- **GIVEN** the evaluated outcome contains active window facts or cue requests
- **WHEN** visual preview refreshes
- **THEN** the editor MAY highlight those clips and list their ids
- **AND** MUST NOT spawn hitboxes, apply damage, play VFX or SFX, trigger camera shake, or write runtime blackboard facts

### Requirement: Timeline Scene Preview Is Tested and Bounded
Timeline scene preview MUST provide EditMode tests and static boundary tests proving that preview binding, key resolution, visual sampling lifecycle, and runtime separation are correct.

#### Scenario: Automatic tests cover preview binding
- **WHEN** Timeline preview binding tests run
- **THEN** they MUST cover unbound target, invalid target, successful Animator binding, missing animation binding, and successful animation key resolution

#### Scenario: Automatic tests cover sampling lifecycle
- **WHEN** Timeline preview session tests run
- **THEN** they MUST cover graph creation, scrub time mapping, graph cleanup, and target change cleanup through testable seams or approved equivalent EditMode coverage

#### Scenario: Static runtime boundary validation
- **WHEN** runtime boundary tests run
- **THEN** they MUST confirm runtime does not reference Timeline Editor preview binding or preview session types
- **AND** MUST confirm ActionTimeline runtime does not store scene target, Animator, AnimationClip, PlayableGraph, or Ref/Taco runtime objects
