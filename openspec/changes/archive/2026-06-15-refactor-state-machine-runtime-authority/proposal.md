# Change: 收口统一状态机运行时权威

## Why
当前项目已经有一棵统一层级角色状态机配置，但运行时仍存在 Locomotion 和 FullBody 两个可能推进状态机的入口，且配置解析仍保留旧字段 fallback 和代码默认值。这样会让同一角色的当前状态、owner、状态时间、动作变体和输出事实存在多个来源，不利于后续 TurnBack、Dodge、Attack、预测回滚和手动验证收口。

## What Changes
- **BREAKING**：`PlayerFullBodyActionController` 成为统一层级状态机的唯一运行时 owner，正式玩法路径只允许它推进 `CharacterStateMachineRunner`。
- **BREAKING**：`PlayerLocomotionController` 退为 Locomotion adapter，只提供输入/空间/移动事实、运动命令构建、动画桥接和黑板写入，不再持有或创建正式运行时状态机 runner。
- **BREAKING**：`LocomotionTickAdapter` 不再作为当前角色正式 gameplay driver；simulation tick 正式入口收口到 `FullBodyActionTickAdapter`。
- **BREAKING**：角色正式配置必须从 `CharacterConfigSO` 及其正式子配置进入运行时，旧平铺字段和 `DodgeActionConfig.Default` 不得作为运行时 fallback。
- 保留现有日志和诊断通道；本变更只新增缺配置、双 driver、旧入口使用等错误诊断，不删除现有 log。
- 不改变当前状态机的数据层级表达，不引入完整 HFSM active stack、父级 enter/exit 冒泡或并行层。

## Impact
- Affected specs: `unified-character-state-machine`, `fullbody-action-framework`, `character-config-root`, `simulation-tick-locomotion`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyFramePipeline.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Runtime/LocomotionTickAdapter.cs`
  - `Assets/Scripts/Character/Config/CharacterConfigSO.cs`
  - 相关 EditMode tests 和 Sandbox/Prefab 装配资产
- Related active changes:
  - `refactor-character-frame-pipeline` 已经引入 FullBody phase pipeline，本变更把迁移期双 driver 校验推进为正式唯一入口。
  - `refactor-locomotion-decision-pipeline` 已经把 Locomotion facts 显式化，本变更把这些 facts 固定为 FullBody pipeline 的输入，而不是独立状态机入口。
  - `refactor-turnback-request-entry` 已经把 TurnBack request 收进统一请求事实，本变更保证请求事实只进入唯一 runner。
