# Change: 重构为统一层级角色逻辑状态机

## Why
当前角色状态框架已经出现明显分裂路径：基础移动有 `BasicLocomotionStateMachine` 和 `LocomotionStateGraphConfigSO`，Dodge 有 `DodgeActionRuntime` / `DodgeFullBodyActionModule` / Action interrupt policy，FullBody HFSM 又只在外层把二者缝成 `/FullBody/Locomotion/*` 和 `/FullBody/Action/Dodge` 路径。设计者看不到一张完整逻辑状态图，也无法在同一个状态入口继续配置该状态的动画转换。

本变更准备先重构架构：删除或退役 Locomotion/Dodge 特化状态机与动作运行时路径，建立一个统一、可配置、层级化的角色逻辑状态机。逻辑状态机先决定当前状态和 transition；动画转换、运动输出和运行时事实作为该逻辑状态的后续配置与输出，不再散落成多套互相拼接的框架。

## What Changes
- **BREAKING**：用统一层级角色逻辑状态机取代现有 `BasicLocomotionStateMachine`、`LocomotionStateGraphConfigSO`、`FullBodyHfsmStateTreeDriver`、`DodgeActionRuntime`、`DodgeFullBodyActionModule` 等特化状态权威。
- **BREAKING**：停止推进 `add-fullbody-action-framework`、`add-fullbody-hfsm-state-tree`、`centralize-fullbody-hfsm-tree-data`、`refactor-fullbody-config-boundaries` 这条缝合路线；实现阶段应删除、回滚或归并其产物，而不是继续补配置入口。
- 新增统一状态树配置能力：同一棵树表达 `FullBody/Locomotion/Idle|MoveStart|MoveLoop|MoveStop`、`FullBody/Action/Dodge` 及未来 Roll/Jump/Attack 等逻辑状态。
- 新增通用 transition 配置：移动意图、预输入请求、状态时间、动画可退出事实、优先级和打断规则都作为状态机 transition 条件表达。
- 新增状态输出配置：每个逻辑状态可配置运动输出、动画输出 key、运行时事实写入和进入/退出行为；输出不得直接引用 Unity 场景对象。
- 新增逻辑状态后的动画转换配置：状态或状态变体可以直接绑定 Animancer `TransitionAssetBase` / TransitionLibrary key / 等价动画过渡信息；动画配置跟随逻辑状态入口可见，但运行时仍由动画外观层消费。
- 保留输入缓冲、运动执行端口、Animancer 播放外观层、相机输入和诊断日志作为外围 adapter；它们不再拥有逻辑状态切换权威。
- 增加 EditMode 测试、静态删除验证和 Play Mode 手动验证，证明普通移动、Directional Dodge、Backstep Dodge 和 Run latch 都由同一张状态图驱动。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - 现有 `unityhfsm-locomotion`、`locomotion-state-graph-config`、`basic-locomotion-animation`、`action-interrupt-arbiter`、`action-runtime-state-tracker` 后续归档时需要按新架构更新或移除冲突要求
- Supersedes active changes:
  - `add-fullbody-action-framework`
  - `add-fullbody-hfsm-state-tree`
  - `centralize-fullbody-hfsm-tree-data`
  - `refactor-fullbody-config-boundaries`
  - `add-dodge-action-profile` 中已落地但依赖分裂路径的运行时和配置部分
- Affected code after approval:
  - `Assets/Scripts/Character/Movement/**`
  - `Assets/Scripts/Character/Action/**`
  - `Assets/Scripts/Character/Animation/**`
  - `Assets/Scripts/Input/**`
  - `Assets/Tests/Editor/**`
  - `docs/agents/character-animation-state-roadmap.md`
- Affected assets after approval:
  - `Assets/Configs/3C/Statemachine/**`
  - `Assets/Configs/3C/Action/**`
  - `Assets/Configs/3C/Animation/**`
  - `Assets/Prefabs/Character/可琳.prefab`
