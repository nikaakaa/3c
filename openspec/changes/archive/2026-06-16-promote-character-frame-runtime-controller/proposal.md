# Change: 提升 CharacterFrameRuntimeController 为正式角色入口

## Why
当前代码已经有 `CharacterFramePipeline` 和 `CharacterFrameRuntimeHost`，但正式 Unity frame tick 仍从 `PlayerFullBodyActionController.Update` 进入，simulation tick 也仍通过 `FullBodyActionTickAdapter` 间接进入 FullBody controller。结果是架构文档说 Character 是最高调度入口，生产装配却仍表现为 FullBody 入口。

本变更把正式运行主线提升为 Character 级 runtime controller，并把 Locomotion 与 FullBody Action 明确规划为 Character frame owner 下的兄弟 submitter。

## What Changes
- 新增 `CharacterFrameRuntimeController` 或等价 MonoBehaviour，作为当前 Corin playable 主线的正式 Unity/Runtime tick 入口。
- `CharacterFrameRuntimeController` 持有并创建唯一 `CharacterFrameRuntimeHost`，组合角色级 runtime port、submitter graph、output composer/applier 所需依赖。
- `PlayerFullBodyActionController` 降级为 FullBody Action module / adapter / compatibility view，不再作为正式 frame tick owner。
- `PlayerLocomotionController` 降级为 Locomotion adapter / submitter dependency，不再作为正式 direct tick 主线。
- 生产路径拆出 Locomotion submitter 与 FullBody Action submitter，二者作为 sibling submitters 进入 `CharacterFramePipeline`。
- `FullBodyIntegratedFrameAdapter` 只保留为迁移兼容路径，并从 Corin 正式 prefab/scene 主线退出。
- `FullBodyActionTickAdapter` 被角色级 tick adapter 替代或降级为兼容转发，不再作为正式 simulation tick 入口。
- Corin 当前正式 playable prefab/scene 绑定到 `CharacterFrameRuntimeController -> CharacterFramePipeline -> sibling submitters -> CharacterFramePlan -> Unified Output Applier` 主线。

## Impact
- Affected specs: `character-frame-pipeline`, `character-runtime-ports`, `fullbody-action-framework`, `wasd-locomotion-pipeline`, `simulation-tick-system`, `character-config-root`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline*.cs`
  - `Assets/Scripts/Character/Pipeline/Contracts/*.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - Corin 当前正式 playable prefab/scene 绑定
- Dependencies:
  - Builds on `formalize-character-frame-arbitration-contract`.
  - Builds on `retire-fullbody-integrated-frame-paths`.
  - Coordinates with `generalize-character-action-request-resolution` but does not implement Attack/Jump/UpperBody.

## Out of Scope
- 不实现新的 Attack、Jump、HitReact、UpperBody、Aim 或 IK runtime。
- 不新增第二 `CharacterFramePipeline`、第二 runner、第二 motion executor、第二 animation presenter。
- 不重排统一状态机拓扑和配置资产语义。
- 不做全项目历史资产清理，只处理当前 Corin playable 主线需要的 prefab/scene 绑定。
