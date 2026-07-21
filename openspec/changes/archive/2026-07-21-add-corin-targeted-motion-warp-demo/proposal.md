# Change: 增加 Corin 目标输入、MotionWarp 与训练敌人闭环

## Why

`add-program-motion-modifier-warping` 已经安装 Timeline authoring、Semantic IR、Float32/Fixed Program、跨 Tick state、WorldSolver 前 Modifier 与 Live Debug链路；纯 Timeline Authoring Preview只展示和编辑MotionWarp，不执行Gameplay Warp。当前 Corin 资产仍然没有任何 `MotionWarpClip`，Attack Profile 仍声明 `ActionTargetRequirement.None`，Graph 中也没有 `ActionTargetSnapshot` Blackboard declaration 或正式目标来源。现状只能证明底层能力可以编译，不能在 Standalone Gameplay 中对一个真实 Session Actor观察攻击距离和朝向修正。

直接把 Attack Profile 改成 `SnapshotRequired` 也不成立。Corin 的 Attack Profile 被 Attack1 到 Attack5 和现有网络测试产品共享；如果某个 Scene 没有目标提供者，所有攻击都会在准入阶段被拒绝。这会把“有目标时修正攻击、无目标时仍能原地攻击”的普通动作游戏语义错误地变成“没有锁定目标就不能攻击”。

当前 `CharacterPipelineHost` 又固定创建 Unity Input System adapter、本地相机和 Local Owner Presentation，无法在同一 Local Session 中干净注册一个使用同一 Program、但输入恒为空且没有相机所有权的训练敌人。为 Demo 复制一个 Enemy Runtime、直接读取敌人 Transform 或在 Character 脚本里写 Blackboard 都会形成第二条 Gameplay 路径。

本 change 建立一个最小但正式的纵向闭环：目标候选作为 portable typed input 进入 InputDerived Blackboard；动作准入和提交读取同一候选；ActionInstance 冻结目标；MotionWarp修正已选中的 Action MotionCurve；第二个 Corin 作为同 Session 的静止模拟 Actor 提供目标 Body。该闭环不实现命中、伤害或完整 AI，但为后续玩家锁定、Bot Intent 和中立怪复用同一输入与 Actor 边界。

## What Changes

- 将 `ActionTargetRequirement` 扩展为 `None`、`OptionalSnapshot`、`SnapshotRequired` 三种类型化策略。
- 明确 `OptionalSnapshot` 语义：有候选目标时 ActionInstance 固定保存目标并允许 MotionWarp；无目标时动作仍可激活，Warp 显式保持源 MotionCurve 不变并输出可诊断原因。`None` 动作继续禁止包含 MotionWarp。
- 为 Float32 与 Fixed `CharacterSimulationInput` 增加类型化 `ActionTargetSnapshot` input value，并同步更新 canonical codec、GameplayHash、ServerAuthoritative input command、DeterministicRollback input payload和neutral input生成。
- 正式启用 Blackboard `InputDerived` sync policy：声明必须显式保存 input value id，Program Layout 在 composition 阶段建立 input-to-state binding，每个 Evaluate 在 Graph control 前把当前 input value写入匹配的 Character-scope Blackboard slot。
- 增加窄 `ICharacterActionTargetInputProvider` 合同。Unity 玩家输入 source只消费显式配置的provider，不扫描Scene、Tag、名称或Transform registry。
- 增加显式 Session Actor目标provider：作者绑定目标 `ActorId`/Character host，provider读取该 Actor最近一次已提交的逻辑 Body pose并生成目标候选，不读取VisualRoot或动画Transform。
- 将 `CharacterPipelineHost` 的输入创建与表现角色拆成显式策略：玩家使用Unity Input System + Local Owner Presentation；训练敌人使用Neutral Input + Simulated Actor Presentation。两者继续注册到同一个 `SimulationSessionHost`，复用同一 Program、Kernel、WorldSolver、Animation Projection和Presenter。
- Neutral input adapter依据Program的正式input catalog生成零连续值和空request，不复制Graph逻辑，也不按Corin input id硬编码。
- 扩展 Agent v11 Snapshot/Patch/typed command/handler/validator：支持 `ActionTargetSnapshot` Blackboard类型、InputDerived input id、`CanActivateAction`目标绑定、ActionProfile target requirement，以及现有MotionWarp Track/Clip配置的完整合同文档。
- 为 Corin RootTree 创建唯一 Character-scope `ActionTargetSnapshot` Blackboard declaration，并让全部 Attack `CanActivateAction` 与 Attack1..Attack5 `ActivateActionInstance`引用同一 declaration。
- 将 Corin Attack Profile配置为`OptionalSnapshot`；Dodge保持`None`。
- 在 Attack1..Attack5 Timeline 的主 Action MotionCurve上分别创建显式MotionWarp Track/Clip。初始窗口、source、position/yaw mode、offset、weight、clamp和累计曲线全部可在Timeline Inspector继续调节；后摇MotionCurve不参与Warp。
- 在 `StandaloneGameplay` 中增加一个同 Session的静止训练敌人Actor，并让玩家目标provider显式绑定该Actor。训练敌人没有AI、攻击、命中或伤害旁路。
- 更新Program/State/Input/Agent schema与generated Corin Semantic IR、Float32 Program、Fixed Program、Projection及产品identity，不保留旧reader或兼容输入格式。
- 更新BTSMTL Agent authoring技能合同到实际schema v11，并记录MotionWarp与Action target全部正式操作。

## Scope

### In Scope

- Action target requirement与ActionInstance目标捕获语义。
- Float32/Fixed typed input、InputDerived Blackboard和对应codec/hash。
- Unity目标provider、玩家/neutral输入策略与模拟角色表现策略。
- Agent v11 target/Blackboard/ActionProfile/MotionWarp authoring闭环。
- Corin Attack1..Attack5 MotionWarp配置。
- Standalone Gameplay中的第二个静止训练敌人Actor。
- MotionWarp target、source、progress、request和Solver result诊断链。

### Out of Scope

- 最近目标、屏幕目标、软锁定、锁定切换和复杂目标评分。
- 敌人AI、行为树意图、寻路、攻击、格挡、受击和死亡。
- Hitbox、命中检测、GameplayResult、伤害和Health变化。
- 客户端目标合法性反作弊、服务端射线或距离复核。
- 垂直MotionWarp、障碍绕行、动态追踪当前Action目标。
- 新增独立Enemy Runtime、Character脚本直写Transform/Blackboard或Scene搜索fallback。

## Impact

- Affected specs:
  - `character-action-activation-flow`
  - `character-input-pipeline`
  - `character-pipeline-blackboard`
  - `character-motion-warp-authoring`
  - `character-pipeline-runtime`
  - `agent-character-controller-synthesis`
  - 新增 `character-targeted-motion-warp-demo`
- Affected runtime:
  - `ThirdPersonSimulation.Core` 的Action target requirement与Program语义。
  - Float32/Fixed input contracts、codec、input runtime、Program layout和MotionWarp runtime。
  - ServerAuthoritative与DeterministicRollback canonical input编码。
  - Unity Character input source、Actor registration、Character host和Presentation factory调用边界。
- Affected authoring/tooling:
  - Pipeline Blackboard declaration metadata与Graph Data Catalog。
  - Agent v11 snapshot/patch/lowerer/handler/validator/MCP合同。
  - Corin Attack Profile、RootTree、Attack1..Attack5 Timeline与generated products。
  - `StandaloneGameplay`与Corin训练敌人prefab/scene instance。
- Breaking changes:
  - Input value schema、canonical codec、GameplayHash和两个Numeric Target ABI随typed target payload升级。
  - Agent schema继续唯一使用现行v11，并在同一typed command合同内增加目标authoring字段与操作；不增加旧schema reader或converter。
  - `CharacterPipelineHost`旧的固定Unity输入/Local Owner装配字段迁移到显式策略后直接删除。

## Current Spec Comparison

- `character-action-activation-flow`当前只允许`None`和`SnapshotRequired`，并要求配置MotionWarp的动作必须强制有目标。本change增加显式`OptionalSnapshot`，同时保留`None + MotionWarp`非法和`SnapshotRequired`缺目标拒绝。
- `character-motion-warp-authoring`当前把所有缺目标情况视为配置错误。该要求需要收窄为：`None`动作配置Warp仍在发布前拒绝；`OptionalSnapshot`动作无目标时按正式策略保留source，不属于静默禁用或fallback。
- `character-input-pipeline`当前只包含Bool/Scalar/Vector输入和request，没有目标候选。新增payload必须进入同一`CharacterSimulationInput`，不能建立Target专用packet。
- `character-pipeline-blackboard`已经声明`InputDerived` sync policy，但当前没有runtime consumer。本change赋予它唯一正式含义和显式input id，不另建外部Blackboard写入口。
- `character-pipeline-runtime`当前Host固定拥有Unity设备输入和相机表现，不能表达同Session的neutral simulated actor。本change只拆装配策略，不改变Program/Session/WorldSolver主链。
- `agent-character-controller-synthesis`、`openspec/project.md`与实际工具已经统一为v11，并已支持Animation Marker、Foot Placement与MotionWarp基础操作；当前缺口是Agent不能创建ActionTargetSnapshot declaration、保存InputDerived input id、配置CanActivate target或修改target requirement。本change在v11唯一合同内补齐这些能力，不重复迁移schema版本。
- `openspec/project.md`明确当前没有目标registry、命中solver或跨角色GameplayResult。本change只建立显式单目标输入与训练敌人，不宣称完整Combat closure。

## Dependencies And Parallel Work

- 依赖已安装的`add-program-motion-modifier-warping`、`character-action-activation-flow`、compiled Simulation Program和统一Presentation链。
- 已完成的`refactor-deterministic-rollback-input-propagation`是本change的正式输入基线；实施必须直接扩展其最终Captured/Relayed Explicit/Predicted/Canonical/Confirmed身份、request timing与codec，不恢复旧input delay或旧canonical host语义。
- 已完成的`refactor-timeline-authoring-preview-to-presentation-only`是本change的正式Preview基线；MotionWarp只通过Live Debug观察真实Gameplay执行，不恢复隔离Preview Simulation Session。
- `add-timeline-animation-marker-sync`已把Agent合同提升到v11，本change直接在该唯一版本上扩展；双方修改Corin Timeline时必须串行使用最新snapshot与source revision。
- `add-btsmtl-ai-controller-authoring`可在独立AI Input模块内并行，但替换训练敌人的Neutral Control Source时必须基于本change最终装配。
- 已完成的`add-predictive-foot-placement-presentation-pass`是当前Host与Presentation Factory基线，本change直接保留其Foot Placement装配，不恢复旧表现路径。
- 不依赖正式命中、伤害或AI change。

## Success Criteria

- Standalone Gameplay启动后，同一`SimulationSessionHost`拥有玩家与训练敌人两个正式Actor。
- 训练敌人使用同一Corin Program、WorldSolver和Presentation链，输入恒为空且不拥有玩家相机。
- 玩家每个输入帧从显式目标provider获得训练敌人最近提交的逻辑Body pose，并通过typed input写入唯一InputDerived Blackboard declaration。
- Attack准入查询与Attack1..Attack5激活读取同一目标候选；每段ActionInstance固定保存激活时pose。
- Attack1..Attack5主MotionCurve各有一个合法MotionWarpClip，后摇MotionCurve不被Warp。
- 有目标时角色在配置窗口内按曲线修正平面位置与yaw，最终碰撞仍由WorldSolver裁决。
- 没有目标provider或目标不可用时，`OptionalSnapshot` Attack仍播放原始MotionCurve；Dodge和其它`None`动作行为不变。
- Local、ServerAuthoritative和DeterministicRollback输入codec都能稳定携带或明确缺省同一目标payload，不存在Target专用网络旁路。
- Agent能够导出并正式修改target declaration、admission、activation、profile与Warp配置，Corin `validate`和Program compile通过。
- 旧Host固定输入装配、Agent target authoring合同缺口、缺失target的隐式运行错误和任何Transform查找路径均不存在。
