# Change: 正式接入动作打断准入门

## Why
当前运行时已经存在 `ActionInterruptArbiter`、`ActionInterruptPolicySetSO` 和统一角色状态机，但 Dodge 进入路径仍在状态机 transition 中使用 `RequestPriorityAtLeast` 直接判断优先级。这让“动作打断仲裁”和“状态机 transition 条件”形成两套动作准入规则，属于分裂路径风险。

## What Changes
- 将 FullBody Action 请求的唯一准入裁决收束到 `ActionInterruptArbiter`。
- 让 `PlayerFullBodyActionController` 或等价 FullBody Action 请求门面读取策略集合、构建仲裁上下文、提交请求并只把 accepted decision 转成统一状态机输入事实。
- 将默认 Dodge 进入 transition 从 `HasInputRequest + RequestPriorityAtLeast` 收敛为只消费已被仲裁接受的 `HasInputRequest(Dodge)`。
- 保留统一状态机对 Locomotion 四阶段和 Action 生命周期的权威，但禁止它在默认动作入口中直接执行优先级、抗性、force 或时间窗口裁决。
- 保留 `ActionInterruptArbiter` 的纯数据边界，禁止仲裁器直接切状态、播放动画或执行位移。
- 补齐自动测试、静态边界验证和 Play Mode 手动验证说明。

## Impact
- Affected specs: `action-interrupt-arbiter`, `action-interrupt-policy-data`, `local-preinput-buffer`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Solver/CommittedActionInputRequestBuilder.cs`
  - `Assets/Scripts/Character/Action/Config/ActionInterruptPolicySetSO.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineDefinition.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/CharacterStateTransitionEvaluator.cs`
  - `Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/ActionInterruptArbiterTests.cs`
