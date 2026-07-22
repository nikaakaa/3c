# Design: Corin 训练 AI 演示

## Context

Standalone已经注册玩家和`corin-training-enemy`两个Actor。训练敌人的原始业务基线使用Neutral Control Source；当前工作区已部分换成AI Control Source与怪兽VisualRoot，但引用的AI Definition无法由Unity加载，generated Program也不匹配当前Character Program，因此现状仍不可运行验收。正式闭环只替换Input producer，不应创建Enemy Character Runtime或改写Corin动作树。

## Behavior Tree

```text
Root
  -> Loop
       -> Sequence
            -> Select Configured Player Target
            -> Write CurrentTarget
            -> Write ActionTargetSnapshot
            -> Selector
                 -> If Distance > AttackRange [Abort Both]
                      -> Write MoveAxis toward target
                 -> If Distance <= AttackRange [Abort Self]
                      -> Write zero MoveAxis
                      -> Submit Attack once
                      -> Wait AttackCooldownTicks
```

AI只决定输入。Attack是否准入、进入哪段状态、窗口、后摇、MotionWarp与动画仍由Corin Character Program决定。

## Target Ownership

`AIPerceptionProfile`显式保存玩家ActorId。AI和玩家ActionTarget provider都从同一个`CommittedActorObservationSnapshot`读取逻辑Body。训练AI不称目标为Enemy或Opponent，也不从Scene、Tag、名称、Camera或最近距离扫描中发现目标。

## Request Lifecycle

`SubmitActionRequest`按节点activation只提交一次。持续Running不会每Tick重复Attack；`WaitTicks`消费Controller-scope冷却值，完成后由Loop产生新的攻击activation。阈值和重入数据属于AI Blackboard或Definition，不硬编码在节点类。

## Movement Boundary

AI输出目标平面方向的MoveAxis。Character Program负责Locomotion，WorldSolver负责碰撞，Presentation负责动画。没有寻路时AI可能被墙挡住，这是演示边界，不通过Transform、teleport或关闭碰撞绕过。

## Enemy Presentation

训练敌人继续使用Corin Character Definition和Presentation Projection，但Host的VisualRoot改为怪兽FBX实例。怪兽与Corin均使用Generic `Bip001`骨架路径，因此同一Animancer输出可以驱动该VisualRoot；旧Corin VisualRoot在训练敌人prefab中显式停用，不能形成第二个动画驱动。

首版不声明怪兽专用Timeline动画映射。Foot Placement仍走正式Pose Post Process合同：怪兽VisualRoot配置自己的`CharacterFootPlacementRig`、两条禁用自动Update的`LimbIK`与现有`FinalIKLimbFootPlacementSolver`，Composition只引用这些同根正式组件。它可以复用现有Profile与Calibration，但左右腿solver骨链必须与怪兽Rig逐Transform一致。

本change不得新增Passthrough、NoOp或Disabled solver来伪造生命周期完成，也不得复用Corin VisualRoot上的LimbIK组件。若怪兽骨架无法通过正式Rig与FinalIK adapter校验，必须停止并报告Presentation资产缺口；禁止回退Animator Controller、跳过Composition或双写动画。

## Asset Authoring

资产修改固定为：

```text
Agent v16 export_snapshot
  -> dry_run_patch
  -> apply same patch
  -> export_snapshot
  -> validate
  -> compile AIIntentProgram
```

不保留Patch文件监听、一次性migrator、YAML写入或Neutral fallback。训练敌人prefab只在AI资产与Program全部可用后原子迁移Control Source引用。

Character Controller Snapshot只读投影Presentation的正式身份边界：Profile、PoseGraph、BlendLibrary、Rig的资产identity与revision，AnimationChannel到PoseSlot映射，以及producer source identity。它不再输出旧Layer、TransitionLibrary、transition asset或easing。Agent Patch仍只修改Character或AI authoring，不获得PoseGraph、BlendLibrary、Rig、PoseSlot或producer source的第二个写入口。AI Snapshot只引用受控Character Definition和Program identity，不复制Character Presentation配置。

当前半迁移资产不能原地修补。Unity公共程序集恢复零编译错误、`AIControllerDefinition`同名脚本类型可加载后，必须通过AssetDatabase正式删除失效Definition、旧RootTree、旧Perception和过期generated Program，再从`bootstrap_ai_controller`开始重建。不得保留旧GUID兼容、手工补`m_Script`、直接改Graph YAML或让prefab继续指向失效Definition。

`AIControllerDefinition`必须位于同名独立C#文件。UnityEngine.Object authoring类型不得与另一个可创建ScriptableObject共享脚本文件后仍假定其MonoScript identity稳定；Definition在domain reload后必须继续由AssetDatabase解析为`AIControllerDefinition`，否则正式Agent根不存在。

## Tradeoffs

### 显式单目标

优点是观察来源和业务含义清楚，可以验证完整输入闭环。代价是不能自动选择2v2vE敌我；阵营与目标评分需要独立能力。

### 直线接近

优点是不会把AI Controller与导航系统绑死。代价是遇到复杂障碍会停住；这不是穿墙或碰撞错误。

### 只迁移Standalone

优点是先验证Local核心，不提前决定Authority Bot或Rollback Bot所有权。代价是三个网络产品暂时不能带该AI Actor，配置时必须明确拒绝。

### 复用Corin Projection并为怪兽配置正式FinalIK

优点是只替换表现骨架，AI输入、Character Program、Timeline事实、动画生命周期与Foot Placement执行边界仍是唯一链路。代价是需要为怪兽骨架显式配置两条LimbIK，并且首版动作仍来自Corin Projection，不是怪兽FBX中的专用攻击组；怪兽专用动作和IK参数调校需要独立Presentation variant change，不能塞回AI Tree。
