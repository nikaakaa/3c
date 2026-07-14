# Change: 清理 Motion 语义到角色管线主线

## Why

当前角色 runtime 已经有 `CharacterPipeline`、BTSMTL/Timeline 播放链路、`MotionContribution` 和 `MotionResolver`，但运动语义需要统一收口：

- `MotionProposal` 名字像临时建议，但它实际承担的是 MotionStage 结算前的运动意图，和 `Request`、`Command`、`Contribution`、`Result` 的语义不够统一。
- root motion、motion warp、受击位移、平台速度、网络校正等外部影响不应该全塞进 Timeline，也不应该绕过 MotionStage 直接改 Transform。
- BBB 参考链路里 `PlayerSO -> MotionClipData/WarpedMotionData -> MotionDriver` 能快速做动作，但会恢复旧 SO/config 和代码状态机路径，不适合作为本项目正式主线。

本变更只负责把运动语义清理为 `MotionContribution -> MotionIntent -> MotionModifier -> MotionResult`。Action/Ability 身份不在本变更内表达，已经由 `replace-action-module-with-ability-runtime` 接管。

## What Changes

- 将 `MotionProposal` 语义改名为 `MotionIntent`，表达 Move 前的最终运动意图。
- 明确 `MotionContribution` 只表达上游来源贡献，例如输入移动、Timeline root motion、击退或外力。
- 引入 `MotionModifier` 语义，作为 MotionStage 内部 Move 前的运动修正层。motion warp 是第一类 modifier，而不是 Timeline 或 Animator 直接移动角色。
- 明确 Timeline 只负责时间编排，例如动画、root motion、motion warp window、gameplay window、cue；目标选择、外部事实和权威修正来自 Graph/Context。
- 清理文档和实现中的旧口径：不再把正式运动意图称为 proposal，不恢复 BBB 旧 motion/action SO 数据源。

## Out of Scope

- 不定义 ActionModule、ActionSubTreeNode 或节点 action identity。
- 不实现完整 GAS、完整技能系统或完整 AbilitySystemComponent。
- 不新增独立 `ActionDefinition` 资产目录或动作注册表。
- 不恢复 BBB `PlayerSO`、`ActionSO`、`MotionClipData`、`WarpedMotionData`、FootPhase 配置链路。
- 不实现真实服务端裁决、Fantasy transport 或完整网络同步。
- 不编写自动化测试，除非后续明确要求。

## Impact

- `Character/Pipeline/Motion` 需要一次命名清理和结构收口。
- `CharacterPipelineOutput.StrictGameplay` 的 motion 字段命名会破坏性变化。
- Timeline root motion 和 motion warp 只输出 `MotionContribution` 或 window 数据，最终运动仍由 MotionStage 结算。
- Action/Ability 身份不再通过 BTSMTL NodeModule 暴露，避免恢复节点 action 身份链路。

## Spec Comparison

- 与 `replace-action-module-with-ability-runtime` 一致：Action/Ability 身份不属于节点模块，不在本变更中定义。
- 与 `add-character-pipeline-runtime-entry` 一致：BTSMTL 内部 TimelinePlaybackScheduler 是 Timeline 播放和采样权威，最终运动仍由 MotionStage 结算。
- 与 `add-root-motion-curve-baking` 一致：root motion 采样输出 motion contribution，再由 resolver 生成 `MotionIntent`。
- 与 `refactor-timeline-animation-pipeline-authority` 一致：Timeline 节点只提交播放请求，不直接驱动表现或位移。
