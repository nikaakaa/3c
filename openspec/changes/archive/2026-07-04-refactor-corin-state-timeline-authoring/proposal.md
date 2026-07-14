# Proposal: 重编排 Corin StateMachine + Timeline 资产

## Why

Corin 当前 RootTree 是最小测试闭环，不是最终作者结构。它把 move input、action activation、Timeline、window/cue/result 输出都平铺在 RootTree。用户需要的是用 BTSMTL 的 Tree、StateMachine、Timeline 建立可调手感结构：

- Locomotion 有自己的状态机：Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop、RunEnd、运动中转身。
- Action 有自己的状态机：None、Attack1、Attack2，表达基础连招。
- 状态 body 默认内联下钻，不为一次性状态 body 创建 SubTree asset。
- Timeline 负责状态内的时间内容。

## What Changes

- 将 Corin RootTree 重编排为高层主流程。
- RootTree 下保留或创建 Locomotion StateMachine 和 Action StateMachine。
- Locomotion StateMachine 第一阶段包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn`。
- Action StateMachine 第一阶段包含 `None`、`Attack1`、`Attack2`。
- 每个 StateNode 使用 inline StateBehaviorSubTree 承载 TimelineNode 和少量 runtime 原语节点。
- Timeline 资产可以新建或调整，但不能用 fallback 配置假装资源存在。
- 删除 Corin RootTree 中当前污染主图的攻击测试 sequence：`Activate Attack`、`Play Attack Timeline`、`Submit Attack Window`、`Submit Attack Cue`、`Submit Loopback Result`。
- 低层动作原语节点不作为主 tree 语义出现；需要时只能位于 Action StateMachine 的状态 inline body、Timeline 轨道/adapter 或后续正式 solver 边界。

## Dependencies

- 依赖 `add-state-machine-runtime-facts` 提供 StateRootCompleted、StateElapsed 等 transition 条件。
- 依赖 `add-timeline-action-fact-authoring` 让 Timeline 攻击的 window/cue 来自 Timeline 轨道。
- 与 `refactor-character-motion-arbitration` 并行：本变更只重编排资产，不定义最终 motion channel 仲裁。

## Non-Goals

- 不实现完整动作库。
- 不做最终商业级 locomotion 手感。
- 不新增业务特化节点类。
- 不恢复旧 locomotion/action SO 或 BBB state registry。
- 不创建一次性 SubTree asset。
- 不实现服务端命中和伤害裁决。

## 当前资产事实

- `CorinCharacterPipelineDefinition.asset` 已引用 Corin RootTree 和 Attack ActionProfile。
- `CorinPlayableRootTree.asset` 当前有 `Move Input To MotionIntent` 和平铺的 Attack 测试 sequence。
- `CorinAttackTimeline.asset` 当前只有 AnimationTrack。
- `CorinAttackActionProfile.asset` 已有基础 network/window/motion/cue 策略配置。
- Corin pipeline 目录当前没有 locomotion timeline 资产。

## 决策和 Tradeoff

### 方案 A：只修当前 RootTree 平铺节点

- 优点：最少资产变动。
- 缺点：作者结构继续混杂，locomotion 与 action 无状态层次。
- 业务取舍：不能展示 BTSMTL authoring 能力。

### 方案 B：所有状态都新建 shared SubTree asset

- 优点：Project 面板中一眼看到所有状态 body。
- 缺点：违反 inline-first；一次性状态 body 资产过多；复用边界和下钻边界混淆。
- 业务取舍：不符合当前 spec。

### 方案 C：RootTree 两个状态机，状态 body inline，下钻编辑

- 优点：主图干净；状态行为自然下钻；Timeline 资源可复用；符合当前项目心智。
- 缺点：资产迁移较大，依赖状态机运行事实和 Timeline 动作事实。
- 业务取舍：最适合当前 demo。

本 proposal 选择方案 C。
