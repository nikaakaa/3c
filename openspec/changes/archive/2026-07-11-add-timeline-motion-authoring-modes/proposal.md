# Change: add-timeline-motion-authoring-modes

## Why

旧 Timeline 曾经能通过 `AnimationClip.RootMotionCurve` 输出 root motion，并能通过 `MotionWarpTrack` 输出 motion warp window，但这会把动画表现片段和玩法位移事实绑在一起。普通攻击前踏、Walk/Run 这类动作如果直接使用完整 root 位移，容易把动画资源里的横向 X/Z 漂移、采样对象朝向或导入差异放大成角色运动错误。

项目需要把 Timeline 位移创作拆成清晰的正式模式：

- 完整动画 root motion：保留动画设计出的本地 XYZ + yaw 轨迹。
- 前向速度/距离 + yaw：忽略横向漂移，用角色当前 forward 推进，适合普通攻击前踏和稳定 locomotion。
- 手画 motion curve：不依赖动画 root，直接在 Timeline 中调位移/yaw 曲线，适合策划微调攻击距离。
- MotionWarp：继续作为 Move 前 modifier，只做目标对齐/吸附修正，不伪装成 root motion。

## What Changes

- 扩展 `RootMotionCurveAsset`，让曲线资产声明求值模式。
- 扩展 Root Motion Baker，支持完整本地位移模式和前向速度/距离模式。
- 扩展 `RootMotionCurveEvaluator`，按曲线模式输出统一 root motion delta。
- 移除 `Timeline.AnimationClip.RootMotionCurve` 作为运行时入口，动画轨只提交动画表现。
- 新增正式 Timeline motion curve authoring，用于直接在 Timeline 中输出 motion contribution。
- `RootMotionCurveAsset` 保持为动画派生曲线资产，但运行时 Timeline 位移必须通过显式 `MotionCurveTrack` 表达。
- 保持 MotionWarp 为 `MotionModifier`，不把它并入 root motion 曲线或直接位移轨。
- 增强 debug/校验，让 Timeline 位移来源能区分完整 root motion、前向速度/距离、手画曲线和 motion warp。

## Out Of Scope

- 不在本 change 内实现 Timeline Loop/Hold 播放策略；那属于播放生命周期，不是位移创作模式。
- 不自动给 Corin 的 Timeline 选择动画 clip 或烘焙具体攻击曲线。
- 不恢复旧 BBB `MotionClipData`、`WarpedMotionData`、PlayerSO 或旧 locomotion/action SO。
- 不新增 fallback 查找：曲线必须由 Timeline clip 或 Timeline motion track 显式引用/配置。
- 不运行 Unity batchmode，不新增测试。

## Current Spec Comparison

- `character-root-motion-curves` 当前要求 `RootMotionCurveAsset` 保存累计本地位置 XYZ 和累计 yaw。这个 change 会修改该要求：完整模式继续保存 XYZ + yaw；前向速度/距离模式保存累计前向距离 + yaw，并明确 runtime 按角色 forward 解释。
- `character-root-motion-curves` 当前仍允许 Timeline 动画片段显式引用 `RootMotionCurveAsset`。当前决策会移除这条运行时入口，避免动画表现和玩法位移耦合。
- `character-motion-semantics` 当前允许 Timeline root motion 和 MotionWarp 进入 motion 管线。这个 change 不改变 MotionStage 唯一边界，但会移除 AnimationClip root motion 入口，把 Timeline 位移入口收敛为 MotionCurveTrack，并强调 MotionWarp 仍是 modifier。
- `character-animation-pipeline` 当前要求 Timeline 轨道只输出管线数据，不直接改 Transform。这个 change 与该要求一致。
- `openspec/project.md` 当前仍写着 `add-pipeline-blackboard-authoring` 未完成，但 `openspec list` 显示该 change 已 Complete；本 change 不修改 project.md，只记录这个上下文偏差。

## Impact

- 作者能按动作业务选择位移模式，而不是所有动作都被迫使用完整 root motion。
- 普通攻击和 locomotion 的位移更稳定，减少动画横向漂移影响。
- 特殊动作仍能保留完整动画轨迹。
- Timeline 位移继续进入 `MotionContribution` / `MotionModifier` / `CharacterMotionStage`，不会绕过预测、网络同步、debug 或纠偏链路。
