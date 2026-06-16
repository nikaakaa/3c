# Change: FullBody 配置作者入口与目录编排收口

## Why
当前 `Assets/Configs/3C` 已经开始迁到 `StateMachine / Action / Animation / Movement` 等目录，但仍存在旧路径、实验命名资产、Dodge/TurnBack 配置权威重复、以及 `Animacer/Pramater` 这类目录拼写债务。设计者要理解一个动作或一个动画绑定，需要在多个浅 Module 之间跳转，配置 Interface 的记忆成本高，后续新增攻击、跳跃、受击时会继续扩散分裂路径。

## What Changes
- **BREAKING**：默认正式配置必须通过一个角色配置作者入口和一套正式目录蓝图解析；旧 `Statemachine`、`Animacer`、`Pramater`、测试命名 motion profile 不得作为正式入口保留。
- 增加角色配置根的作者视图要求，让设计者能从一个入口追踪 StateMachine、Movement、Action 逻辑、Action 动画、Locomotion 动画、Animancer、Input 和 Camera 配置。
- 明确 `Assets/Configs/3C` 的目标目录编排，目录名必须表达 Module 归属，不能靠历史文件名猜职责。
- 收紧 Dodge 参数权威：动作 motion 参数必须来自正式 Action 逻辑配置，状态机节点不能再并行保存同一 duration/distance。
- 将包含 Dodge 和 TurnBack 的默认请求策略集合从 Dodge-only 命名中移出，改名并归属为 `CorinFullBodyStateRequestPolicySet.asset`。
- 将 `CharacterConfig.asset` 正式迁移到角色目录，目标路径为 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
- 将当前 `Default*` 正式角色资产改为 `Corin + 语义` 命名，保留 `Default` 只用于未来模板资产。
- 本次只将 Generic Animancer transition library 作为 Corin 的正式 rig variant；Humanoid 暂不纳入正式配置闭环。
- 增加配置校验要求，覆盖旧万能字段、重复 alias、测试资产引用、缺失根引用和目录拼写残留。

## Impact
- Affected specs:
  - `fullbody-config-boundaries`
  - `character-config-root`
  - `dodge-action`
  - `action-interrupt-policy-data`
- Affected code:
  - `Assets/Scripts/Character/Config/*`
  - `Assets/Scripts/Character/Action/FullBody/*`
  - `Assets/Scripts/Character/StateMachine/Config/*`
  - `Assets/Scripts/Character/StateMachine/Solver/*`
  - `Assets/Tests/Editor/*`
- Affected assets:
  - `Assets/Configs/3C/CharacterConfig.asset`
  - `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
  - `Assets/Configs/3C/StateMachine/FullBody/CorinFullBodyStateMachine.asset`
  - `Assets/Configs/3C/Action/*`
  - `Assets/Configs/3C/Animation/*`
  - `Assets/Configs/3C/Animacer/*`
  - `Assets/Configs/3C/Input/*`
- Related active changes:
  - `add-configurable-state-interrupt-windows` owns state request timing policy; this change only names and places the policy asset.
  - `refactor-state-action-motion-output` owns action motion output shape; this change requires its resolved action motion data to come from the formal Action config source.
  - `refactor-unified-animancer-presenter` owns runtime presenter unification; this change only reorganizes animation authoring assets and validation.
