# Design: Corin 训练 AI 演示

## Context

Standalone已经注册玩家和`corin-training-enemy`两个Actor。训练敌人当前使用Neutral Control Source，因此具备正式Body、碰撞和表现但不移动。AI核心安装后只需替换Input producer，不应创建Enemy Character Runtime或改写Corin动作树。

## Behavior Tree

```text
Root
  -> Read Configured Player Target
  -> Write CurrentTarget
  -> Write ActionTargetSnapshot
  -> Selector
       -> If Distance > AttackRange
            -> Write MoveAxis toward target
       -> If Attack request eligible
            -> Write zero MoveAxis
            -> Submit Attack once
       -> Write zero MoveAxis
```

AI只决定输入。Attack是否准入、进入哪段状态、窗口、后摇、MotionWarp与动画仍由Corin Character Program决定。

## Target Ownership

`AIPerceptionProfile`显式保存玩家ActorId。AI和玩家ActionTarget provider都从同一个`CommittedActorObservationSnapshot`读取逻辑Body。训练AI不称目标为Enemy或Opponent，也不从Scene、Tag、名称、Camera或最近距离扫描中发现目标。

## Request Lifecycle

`SubmitActionRequest`按节点activation只提交一次。持续Running不会每Tick重复Attack；重新攻击由Tree中显式重入或冷却条件形成新activation。阈值和重入数据属于AI Blackboard或Definition，不硬编码在节点类。

## Movement Boundary

AI输出目标平面方向的MoveAxis。Character Program负责Locomotion，WorldSolver负责碰撞，Presentation负责动画。没有寻路时AI可能被墙挡住，这是演示边界，不通过Transform、teleport或关闭碰撞绕过。

## Asset Authoring

资产修改固定为：

```text
Agent v15 export_snapshot
  -> dry_run_patch
  -> apply same patch
  -> export_snapshot
  -> validate
  -> compile AIIntentProgram
```

不保留Patch文件监听、一次性migrator、YAML写入或Neutral fallback。训练敌人prefab只在AI资产与Program全部可用后原子迁移Control Source引用。

## Tradeoffs

### 显式单目标

优点是观察来源和业务含义清楚，可以验证完整输入闭环。代价是不能自动选择2v2vE敌我；阵营与目标评分需要独立能力。

### 直线接近

优点是不会把AI Controller与导航系统绑死。代价是遇到复杂障碍会停住；这不是穿墙或碰撞错误。

### 只迁移Standalone

优点是先验证Local核心，不提前决定Authority Bot或Rollback Bot所有权。代价是三个网络产品暂时不能带该AI Actor，配置时必须明确拒绝。

