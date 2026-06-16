# Change: 收口 TurnBack 请求入口

## Why
TurnBack 现在已经需要保留 `LocomotionTurnBackIntent` 作为移动侧候选事实，但状态机仍可能通过 `MoveTurnBackRequested` 直接消费该 intent 进入 `FullBody/Locomotion/TurnBack`。这会让 TurnBack 同时拥有“intent 直进”和“accepted request fact 进入”两种语义入口，削弱 `ActionInterruptArbiter`、timeline window、priority 和 resistance 的统一权威。

## What Changes
- 保留 `LocomotionTurnBackIntent`，但将它降级为候选事实，只用于构建 TurnBack 请求。
- 统一状态机从 `MoveStart` 或 `MoveLoop` 进入 TurnBack 时 MUST 只消费已被状态请求仲裁接受的 `CharacterInputRequestFact(InputRequestKind.TurnBack)`。
- `MoveTurnBackRequested` 或等价 intent 直读条件 MUST 不再作为默认 TurnBack 进入 transition 的权威条件。
- TurnBack 进入后继续沿用状态 timeline facts 控制 motion、input lock 和 exit window。
- 不新增 TurnBack 专用仲裁器，不新增 fallback 配置，不修改 Humanoid 路径。

## Impact
- Affected specs: `unified-character-state-machine`, `action-interrupt-arbiter`, `basic-locomotion-animation`
- Affected code: `CharacterStateTransitionEvaluator`, 默认状态机 transition/timeline 配置，默认 action interrupt policy 配置，`FullBodyActionRequestGate`, `FullBodyActionInterruptGate`, `PlayerLocomotionController`, `UnifiedCharacterStateMachineTests`, `ActionInterruptPolicyDataTests`
- Related changes: 依赖并收紧 `add-configurable-state-interrupt-windows` 中的 TurnBack 请求仲裁链路
