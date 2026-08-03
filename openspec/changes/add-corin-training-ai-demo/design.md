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

AI只决定输入。Attack是否准入、进入哪段Gameplay状态、窗口、后摇与MotionWarp仍由Corin Character Program决定；有限Action动画由Program确认的Timeline playback进入AnimationSlot，持续Locomotion动画在动画职责重构安装后由PoseStateMachine根据committed Body/Intent选择。AI不得直接选择两类动画。

## Target Ownership

`AIPerceptionProfile`显式保存玩家ActorId。AI和玩家ActionTarget provider都从同一个`CommittedActorObservationSnapshot`读取逻辑Body。训练AI不称目标为Enemy或Opponent，也不从Scene、Tag、名称、Camera或最近距离扫描中发现目标。

## Request Lifecycle

`SubmitActionRequest`按节点activation只提交一次。持续Running不会每Tick重复Attack；`WaitTicks`消费Controller-scope冷却值，完成后由Loop产生新的攻击activation。阈值和重入数据属于AI Blackboard或Definition，不硬编码在节点类。

## Movement Boundary

AI输出目标平面方向的MoveAxis。Character Program负责Locomotion，WorldSolver负责碰撞，Presentation负责动画。没有寻路时AI可能被墙挡住，这是演示边界，不通过Transform、teleport或关闭碰撞绕过。

## Enemy Presentation

训练敌人继续使用Corin Character Definition和Presentation Projection，但Host的VisualRoot改为怪兽FBX实例。怪兽与Corin均使用Generic `Bip001`骨架路径，因此同一Animancer输出可以驱动该VisualRoot；旧Corin VisualRoot在训练敌人prefab中显式停用，不能形成第二个动画驱动。

首版不声明怪兽专用Timeline动画映射。Foot Placement走正式ordered Pose Plan合同：怪兽VisualRoot配置Rig v3、`CharacterAnimationRigBinding`与`CharacterWorldAwarePresentationBinding`，FootPlacement operation在同帧上游Component Pose上执行Planner与解析式Limb Pose Solver。它可以复用现有Profile与Calibration，但左右腿Physical chain与Calibration identity必须和怪兽Rig严格一致。

本change不得新增Passthrough、NoOp、Disabled、Final IK或图外solver来伪造生命周期完成。若怪兽骨架无法通过Rig v3、Calibration与world-aware stage校验，必须停止并报告Presentation资产缺口；禁止回退Animator Controller、跳过Pose Plan或双写动画。

## Asset Authoring

资产修改固定为：

```text
Agent Document v3 package checkout
  -> 编辑唯一AI editable正文
  -> dry_run_document
  -> apply_document(same document hash)
  -> canonical Document反向同步
  -> validate
  -> publish AIIntentProgram
```

不保留Document文件监听、Patch、一次性migrator、YAML写入或Neutral fallback。训练敌人prefab只在AI资产与Program全部可用后原子迁移Control Source引用。

AIController Document context只读投影受控Character的Definition、Input/Request capability、Program identity与必要Presentation capability，不复制或修改Character Presentation配置。CharacterController Document v3中的Profile与PoseGraph editable由共享Presentation Mutation唯一处理；本change既不扩展也不旁路该写链。

当前半迁移资产不能原地修补。Unity公共程序集恢复零编译错误、`AIControllerDefinition`同名脚本类型可加载后，必须通过正式资产创建入口一次性重建Definition、RootTree、Perception和generated Program，再对已有合法Definition执行Document checkout。Document工具不创建不存在的root，不得恢复`bootstrap_ai_controller`旁路。不得保留旧GUID兼容、手工补`m_Script`、直接改Graph YAML或让prefab继续指向失效Definition。

`AIControllerDefinition`必须位于同名独立C#文件。UnityEngine.Object authoring类型不得与另一个可创建ScriptableObject共享脚本文件后仍假定其MonoScript identity稳定；Definition在domain reload后必须继续由AssetDatabase解析为`AIControllerDefinition`，否则正式Agent根不存在。

## Tradeoffs

### 显式单目标

优点是观察来源和业务含义清楚，可以验证完整输入闭环。代价是不能自动选择2v2vE敌我；阵营与目标评分需要独立能力。

### 直线接近

优点是不会把AI Controller与导航系统绑死。代价是遇到复杂障碍会停住；这不是穿墙或碰撞错误。

### 只迁移Standalone

优点是先验证Local核心，不提前决定Authority Bot或Rollback Bot所有权。代价是三个网络产品暂时不能带该AI Actor，配置时必须明确拒绝。

### 复用Corin Projection并为怪兽配置正式FootPlacement Pose节点

优点是只替换表现骨架，AI输入、Character Program、Timeline事实、动画生命周期与Foot Placement执行边界仍是唯一链路。代价是需要为怪兽骨架显式配置Rig v3双腿Physical chain，并且首版动作仍来自Corin Projection，不是怪兽FBX中的专用攻击组；怪兽专用动作和Foot Placement参数调校需要独立Presentation variant change，不能塞回AI Tree。

