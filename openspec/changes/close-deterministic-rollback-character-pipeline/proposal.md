# Change: 闭合 Deterministic Rollback 角色产品管线

## Why

DeterministicRollback的网络模型、Relay产品、Fixed Program、Snapshot/Replay、Fixed KCC与双Client Demo已经接入，Gameplay Lab也已经有Local Fixed与Rollback两个显式Variant。但当前没有active change拥有最后一段统一收口：Character authoring完成迁移后，如何从同一Corin Definition发布Fixed Program与Presentation Projection，以及如何让Gameplay Lab和Rollback Product引用现有同一环境、KCC与collision artifact并发布可运行产品。

继续把这段工作分散到PoseGraph、KCC或Network Build change会产生多个资产装配真相。本change只负责最终产品闭包，不新增网络模型、第二Fixed Host、第二场景作者源或自动Build。

## What Changes

- 以完成迁移的Corin CharacterPipelineDefinition作为唯一Character产品根。
- 通过显式Fixed Build Request从同一validated Semantic IR独立发布Fixed Program与target-neutral Presentation Projection。
- 让Gameplay Lab的Local Fixed Variant与DeterministicRollback Variant引用同一Corin Fixed Program、KCC配置、可见环境和collision artifact identity。
- 让Rollback Network Test Product只消费该正式Variant与既有双Peer/Relay产品合同，不在Build adapter中重建第二份角色或世界配置。
- 锁定现有正式KCC与collision identity，不重新接入KCC，也不让产品闭包修改KCC算法。
- 修复MovingTurn回归：只允许RunLoop以固定180°短Root Motion进入，Timeline完成后立刻回到正式Locomotion，并删除Gameplay输入转向与Pose RootOrientationWarp形成的双重朝向路径。
- 删除旧Host、旧Rollback专用角色装配、重复Collision authoring、旧Fixed wrapper与stale product manifest。

## Scope

### In Scope

- Corin Fixed Program与Presentation Projection的同源发布。
- Gameplay Lab Local Fixed/Rollback Variant的共享资产闭包。
- Rollback Peer Scene、KCC、collision artifact与Product manifest identity对账。
- 现有KCC与collision identity的产品引用对账。
- 双Client加纯.NET Relay Server的既有Build/Run入口收口。

### Out of Scope

- 新网络同步算法、第三个Peer、Authority Bot或Rollback AI。
- Blend Space独立演示、Motion Matching独立演示、Action Animation Workspace。
- 命中、伤害、combat rewind、lag compensation或完整2v2vE比赛规则。
- 自动Build、selection触发Build、Play Mode自动修复或运行时fallback。
- 新增测试；用户负责Unity端到端运行。

## Dependencies And Sequencing

- 先完成`refactor-agent-authoring-to-synced-json-document`仍被v3复用的基础任务。
- 再完成动画控制运行边界、Pose共享authoring逻辑、Document v3、共享UI、Action Workspace、全部已存在动画能力接入、一次Corin迁移，以及同源Pose IR、Fixed Program与Projection发布。
- 最后执行本change的环境、Variant与Network Test Product闭包，直接消费已经接入的正式KCC。
- `add-character-presentation-blend-space`、`add-character-motion-matching-pose-source`与`add-corin-training-ai-demo`的独立业务内容保留价值，但不属于Rollback关键路径；Action Workspace属于迁移前置，不在此列。
- 本change不重建Rollback模型，不重接KCC算法，只把已经能运行的Rollback装配重新锁定到新生成产品。唯一串行关系见`openspec/character-pipeline-serial-execution.md`。

## Current Spec Comparison

- current `deterministic-rollback-two-client-demo`已经要求同一Corin Semantic Artifact、Fixed Program、Projection、环境Prefab与Collision Artifact，但没有规定Gameplay Lab两个Variant必须共享同一正式资产闭包。
- current `gameplay-network-test-build-workflow`已经规定Build/Run分离与原子产品发布，本change只补充Rollback adapter必须消费正式Variant，不能在adapter内部复制配置。
- current Fixed KCC已经进入Rollback组合；本change只核对并复用现有KCC identity，不拥有KCC算法或Motor接入。
- `openspec/project.md`已经指出Local Fixed Gameplay Lab与DeterministicRollback尚缺统一闭包，本change成为该缺口的唯一owner。

## Success Criteria

- Local Fixed与Rollback Variant引用同一Corin Fixed Program、Projection、KCC与collision artifact identity。
- Rollback Build只从精确Definition和精确Variant生成产品，不扫描Scene或选择资产。
- 两个Peer与Relay manifest锁定相同SemanticHash、ProgramHash、ProjectionRevision、CollisionWorldHash与KccId。
- 旧Fixed wrapper、旧KCC identity、旧collision artifact与旧Product manifest被明确删除或拒绝。
- Run只启动既有产品，不触发Build、迁移、烘焙或配置修复。
