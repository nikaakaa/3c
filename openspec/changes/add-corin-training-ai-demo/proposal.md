# Change: 增加 Corin 训练 AI 演示闭环

## Why

AI核心和Agent Document目录包分别解决“系统能运行AI”和“工具能安全写AI资产”，但都不应夹带具体业务样例。Standalone Gameplay已经有玩家Corin与训练敌人，原始业务基线由Neutral Control Source驱动；当前apply工作区已出现AI Control Source、怪兽VisualRoot和AI资产的半迁移结果，但Definition MonoScript失效、generated Program过期且Foot Placement仍有Missing组件，因此不能视为正式闭环。本change必须用正式Agent Document事务整套重建AI资产，并继续复用同一Corin Character Program、WorldSolver和Presentation。

本change使用正式root创建入口建立Corin Training AI Definition、Tree与Perception，再通过完成升级后的Agent Document v3目录包配置Tree并发布AIIntentProgram，把现有训练敌人从Neutral迁移为AI。Patch与Document不得并存。行为只包括显式目标、直线接近、停止和普通Attack请求，不冒充阵营、寻路、命中、伤害或完整怪物AI。

## What Changes

- 创建Corin Training AI Controller Definition、AIControllerTree与AIPerceptionProfile。
- 显式绑定玩家ActorId作为唯一候选目标，不按Team、Tag、名称或距离扫描选择敌我。
- 配置Controller-scope CurrentTarget、AttackRange和攻击重入所需可调变量。
- 配置目标获取、ActionTargetSnapshot、距离外MoveAxis、距离内停止和Attack request分支。
- 增加通用WaitTicks语义，使攻击冷却由AI Tree显式编排，而不是由节点runtime硬编码。
- 补齐AI Agent Document只读投影中的Loop、Compare、ConditionRuleGraph和AbortPolicy，使工具可完整复核行为条件。
- 将`AIControllerDefinition`拆为同名独立Unity脚本资产，保证Definition经过domain reload后仍能解析为正式类型。
- 通过Agent Document v3执行package checkout、通用文件工具编辑`editable/**/*.json`、dry-run、同hash apply、canonical反向同步和validate，不直接编辑Graph YAML。
- AIController package只读引用受控Character的Input/Request与Presentation capability；CharacterController package中的Presentation editable由`refactor-pose-graph-to-btsmtl-authoring-domain`唯一拥有，本change不重复定义。
- 编译并绑定Corin AIIntentProgram。
- 将现有`corin-training-enemy`Control Source从Neutral迁移为AI，删除其Neutral序列化绑定。
- 将训练敌人的VisualRoot迁移为`Assets/AssetArt/Animation/ZZZ/敌人/怪兽/怪兽.fbx`，由同一Animancer正式表现链驱动。
- 保持训练敌人的Corin Definition、Character Program、Projection、WorldSolver、Body binding和SimulatedActor Presentation角色不变。

## 动画职责重构关系

本change固定在`refactor-pose-graph-to-btsmtl-authoring-domain`完成Document v3与Corin正式产品发布之后实施。训练AI只输出Character Input，并直接复用最终Corin PoseStateMachine、Pose source、AnimationSlot、Rig与Presentation Projection；不得生成旧BaseLocomotion Selection、独立动画路径或按实施顺序分支。

## Scope

### In Scope

- Corin训练AI具体authoring资产与generated AI Program。
- Standalone训练敌人Control Source迁移。
- 直线接近、停止、普通攻击请求和显式目标快照。
- 训练AI与玩家目标provider共用Committed Observation。
- 训练敌人怪兽VisualRoot、Generic Rig v3绑定与正式FootPlacement world-aware operation配置。

### Out of Scope

- Team、Faction、动态目标搜索、仇恨和威胁评分。
- NavMesh、DotRecast寻路、动态避障和绕障。
- Hitbox、命中、伤害、受击、死亡和完整Combat结果。
- ServerAuthoritative、DotRecast Authority或DeterministicRollback AI。
- 怪兽专用Character Program、怪兽专用Timeline动画映射和怪兽脚底IK调校。

## Impact

- Affected specs:
  - `character-targeted-motion-warp-demo`
  - `agent-ai-controller-synthesis`
  - 新增`corin-training-ai-demo`
- Affected assets:
  - Corin Training AI Definition、Tree、Perception Profile与generated Program。
  - Corin Standalone训练敌人prefab/runtime profile。
- Affected code:
  - 通用AI WaitTicks语义及Agent Document v3 AI投影完整性。
  - AIControllerDefinition Unity脚本资产所有权。
- Breaking changes:
  - Standalone训练敌人不再使用Neutral Control Source。
  - AI配置失败时直接拒绝Session，不回退Neutral。

## Current Spec Comparison

- `character-targeted-motion-warp-demo`当前明确规定训练敌人使用Neutral Input并且样例不包含AI。本change将该业务事实更新为最小AI演示，但继续保留“非完整Combat、非完整敌人AI”的边界。
- `add-btsmtl-ai-controller-authoring`只提供通用AI核心，不拥有Corin资产。
- Agent Document v3工具只提供通用authoring合同，不拥有业务Patch或训练敌人配置。

## Dependencies And Sequencing

- 硬依赖`add-btsmtl-ai-controller-authoring`完成AI运行与Local Session能力。
- 硬依赖`refactor-pose-graph-to-btsmtl-authoring-domain`完成唯一Agent Document v3资产写入口。
- 依赖已经完成的`add-corin-targeted-motion-warp-demo`提供同Session训练敌人、ActionTargetSnapshot和SimulatedActor表现。
- 不得在前置未完成时使用YAML、迁移器、临时菜单或MonoBehaviour AI抢跑。
- 本change位于`openspec/character-pipeline-serial-execution.md`的Rollback闭合后独立内容队列，不修改已经锁定的Corin角色、动画或Rollback产品闭包。

## Success Criteria

- Corin Training AI结构由Agent Document v3正式事务写入并可反向canonical同步。
- AI Definition经过Unity domain reload后仍保持有效MonoScript引用和类型身份。
- 训练敌人使用AI Control Source且不存在Neutral fallback绑定。
- 训练AI读取同一Committed Observation中的显式玩家Actor。
- 目标距离外输出MoveAxis，攻击距离内停止并按一次性activation提交Attack。
- Agent Document v3可以直接审查Loop、距离比较、条件子图和中止策略。
- Standalone训练敌人只显示怪兽VisualRoot，且仍由同一Host、Animancer和Presentation Projection驱动。
- 最终移动、状态、Timeline、MotionWarp、碰撞和动画继续由Corin Character Program、WorldSolver与Presentation处理。
- 资产不包含Team、Tag、名称搜索、NavMesh、Transform移动或Combat伪结果。
