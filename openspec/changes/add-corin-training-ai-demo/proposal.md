# Change: 增加 Corin 训练 AI 演示闭环

## Why

AI核心和Agent v15分别解决“系统能运行AI”和“工具能安全写AI资产”，但都不应夹带具体业务样例。当前Standalone Gameplay已经有玩家Corin与Neutral训练敌人，正好可以作为第一条可观察纵切：只替换训练敌人的Control Source，继续复用同一Corin Character Program、WorldSolver和Presentation。

本change使用正式Agent v15事务创建Corin Training AI Definition、Tree与Perception配置，编译AIIntentProgram，并把现有训练敌人从Neutral迁移为AI。行为只包括显式目标、直线接近、停止和普通Attack请求，不冒充阵营、寻路、命中、伤害或完整怪物AI。

## What Changes

- 创建Corin Training AI Controller Definition、AIControllerTree与AIPerceptionProfile。
- 显式绑定玩家ActorId作为唯一候选目标，不按Team、Tag、名称或距离扫描选择敌我。
- 配置Controller-scope CurrentTarget、AttackRange和攻击重入所需可调变量。
- 配置目标获取、ActionTargetSnapshot、距离外MoveAxis、距离内停止和Attack request分支。
- 通过Agent v15执行export、dry-run、apply、re-export和validate，不直接编辑Graph YAML。
- 编译并绑定Corin AIIntentProgram。
- 将现有`corin-training-enemy`Control Source从Neutral迁移为AI，删除其Neutral序列化绑定。
- 保持训练敌人的Corin Definition、Character Program、Projection、WorldSolver、Body binding和SimulatedActor Presentation不变。

## Scope

### In Scope

- Corin训练AI具体authoring资产与generated AI Program。
- Standalone训练敌人Control Source迁移。
- 直线接近、停止、普通攻击请求和显式目标快照。
- 训练AI与玩家目标provider共用Committed Observation。

### Out of Scope

- Team、Faction、动态目标搜索、仇恨和威胁评分。
- NavMesh、DotRecast寻路、动态避障和绕障。
- Hitbox、命中、伤害、受击、死亡和完整Combat结果。
- ServerAuthoritative、DotRecast Authority或DeterministicRollback AI。
- 新AI节点、新runtime路径或Agent schema修改。

## Impact

- Affected specs:
  - `character-targeted-motion-warp-demo`
  - 新增`corin-training-ai-demo`
- Affected assets:
  - Corin Training AI Definition、Tree、Perception Profile与generated Program。
  - Corin Standalone训练敌人prefab/runtime profile。
- Breaking changes:
  - Standalone训练敌人不再使用Neutral Control Source。
  - AI配置失败时直接拒绝Session，不回退Neutral。

## Current Spec Comparison

- `character-targeted-motion-warp-demo`当前明确规定训练敌人使用Neutral Input并且样例不包含AI。本change将该业务事实更新为最小AI演示，但继续保留“非完整Combat、非完整敌人AI”的边界。
- `add-btsmtl-ai-controller-authoring`只提供通用AI核心，不拥有Corin资产。
- `extend-agent-authoring-for-ai-controller`只提供v15工具，不拥有业务Patch或训练敌人配置。

## Dependencies And Sequencing

- 硬依赖`add-btsmtl-ai-controller-authoring`完成AI运行与Local Session能力。
- 硬依赖`extend-agent-authoring-for-ai-controller`完成v15唯一资产写入口。
- 依赖已经完成的`add-corin-targeted-motion-warp-demo`提供同Session训练敌人、ActionTargetSnapshot和SimulatedActor表现。
- 不得在前置未完成时使用YAML、迁移器、临时菜单或MonoBehaviour AI抢跑。

## Success Criteria

- Corin Training AI资产全部由Agent v15正式事务创建并可重新导出。
- 训练敌人使用AI Control Source且不存在Neutral fallback绑定。
- 训练AI读取同一Committed Observation中的显式玩家Actor。
- 目标距离外输出MoveAxis，攻击距离内停止并按一次性activation提交Attack。
- 最终移动、状态、Timeline、MotionWarp、碰撞和动画继续由Corin Character Program、WorldSolver与Presentation处理。
- 资产不包含Team、Tag、名称搜索、NavMesh、Transform移动或Combat伪结果。

