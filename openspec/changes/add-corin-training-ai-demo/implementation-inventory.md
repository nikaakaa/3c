# Corin 训练 AI 实现清单

## 用途

本文记录apply期间已经存在的代码与资产事实。它不是验收结果，也不把半迁移资产视为正式配置。最终真相仍由Agent v16重新导出的Snapshot、generated AI Program和完成后的tasks决定。

## Standalone Actor基线

| Actor | ActorId | Control Source现状 | Presentation Role | World Binding |
|---|---|---|---|---|
| 玩家Corin | `LocalActor` | Player Control Source | `LocalOwner`，序列化值`1` | `Corin.LocalBody` |
| 训练敌人 | `corin-training-enemy` | prefab已半迁移为AI Control Source，但引用失效Definition | `SimulatedActor`，序列化值`2` | `Corin.TrainingEnemyBody` |

玩家的`ActionTarget`由`SessionActorActionTargetInputProvider`提供，目标在Standalone scene中绑定训练敌人。玩家输入与AI perception都必须消费同一Session提交后的Committed Observation，不允许直接读对方Transform。

## 公共角色链

- Character Definition GUID：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- Character Program Id：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- 当前Character Program Hash：`8c497024dda307ffc4955f6ce5bc93684d236e149905cff871c44fd6f2f57672`。
- 训练敌人继续复用Corin Definition、Program、Projection、Unity WorldSolver、Body Presentation和SimulatedActor表现角色。
- AI只负责生成Character Input与Action Request，不拥有移动、Action admission、Timeline、MotionWarp或动画播放。

## 半迁移AI资产

| 资产 | GUID | 当前问题 | 正式处理 |
|---|---|---|---|
| `CorinTrainingAIControllerDefinition.asset` | `2d17195dbd81b36419e464bf91f40f49` | `m_Script`为`fileID: 0`，Unity无法按正式类型加载 | 编译恢复后正式删除并由Agent bootstrap重建 |
| `CorinTrainingAIController.AIRootTree.asset` | `85b261b17e94e1c4784c45be082f5101` | 属于失效Definition事务的旧Graph | 与Definition一起删除重建 |
| `CorinTrainingAIPerceptionProfile.asset` | `348860615c73c1245a95f319607280e1` | 保存候选`LocalActor`，但属于旧事务 | 与Definition一起删除重建 |
| `CorinTrainingAIControllerDefinition.AIIntentProgram.asset` | `0dc26e12d289a1541a65ce02afec7169` | Character Program Hash仍为`a2ad26b6...`，与当前`8c497024...`不一致 | 删除后由同一次Agent apply重新发布 |

正式`AIControllerDefinition`脚本已拆到同名文件，MonoScript GUID为`1a4eb2a77d60d924091085c3f2954831`。禁止手工把该GUID写进旧Definition YAML；必须让Unity加载类型后由正式bootstrap创建新资产。

## 训练敌人Prefab现状

- prefab GUID：`92b30edb28014fe8a1c58b893df987d4`。
- 已添加`AICharacterControlSource`，但仍引用失效Definition GUID `2d17195dbd81b36419e464bf91f40f49`。
- 怪兽FBX已成为`EnemyVisualRoot`，Host的Animancer、VisualRoot和Foot Placement已指向该根，旧Corin VisualRoot已停用。
- 怪兽Animator未绑定Controller，`ApplyRootMotion`为false。
- Foot Placement Composition仍引用fileID `6592207573670226370`；该组件脚本GUID `ea61987482202cc4fbc93feabb171635`已不存在，属于Missing/Passthrough残留。
- 正式迁移必须删除Missing组件，在怪兽根配置自己的`CharacterFootPlacementRig`、两条禁用自动Update的`LimbIK`和`FinalIKLimbFootPlacementSolver`，并让Composition只引用同根正式组件。

## Unity前置阻断

2026-07-22完成Agent Snapshot v16迁移后，命令行定向构建`ThirdPersonClient.Editor.csproj`的首轮错误为：Unity生成的`ThirdPersonClient.Runtime.csproj`仍引用已删除的`Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/Animancer/AnimancerPlaybackAdapter.cs`。该次构建没有进入Agent Editor源码编译，不能据此宣称Agent程序集已经编译通过。

恢复任务后读取Unity Console，实时前置已推进为`Animation/BlendStack/AnimationBlendStackRuntime.cs:670`的`CS1002`与`CS1513`。`CorinPlayableRootTree`的Missing Type记录是公共程序集未完成编译的连锁结果。AI owner不修改该文件，也不根据Missing Type状态重建AI资产。

该断点属于并行AnimGraph迁移和Unity工程清单刷新，不属于AI change。AI owner不得恢复旧Adapter、修改动画公共文件或手工维护Unity生成的csproj。Unity重新生成工程并恢复零编译错误前，不删除或重建旧AI资产，也不修改prefab组件引用，避免Unity在类型未加载时再次生成坏MonoScript或Missing组件。

Agent Snapshot静态合同已经迁移为v16：Exporter不再读取Layer、TransitionLibrary、transition asset或easing；只读输出PoseGraph、BlendLibrary、Rig、AnimationChannel到PoseSlot和producer source identity。OpenSpec strict validation与`git diff --check`已经通过，但正式Agent export、validate和AI资产重建仍等待Unity类型加载恢复。

不依赖动画公共程序集的构建结果：

- `ThirdPersonSimulation.Core`与`ThirdPersonSimulation.Float32`：0 warning，0 error。
- `BTSMTL`与`BTSMTL.Editor`：0 error；仅Unity Test Framework产生2条第三方warning。
- 两次构建均使用规定参数，并在完成后立即执行`dotnet build-server shutdown`。

## 恢复后的固定执行顺序

1. 确认Unity零编译错误且`AIControllerDefinition`正式类型可加载。
2. 通过AssetDatabase删除失效Definition、旧RootTree、旧Perception和过期generated Program。
3. 执行`bootstrap_ai_controller`创建正式根资产。
4. 执行Agent v16 `export_snapshot -> dry_run_patch -> apply same patch -> export_snapshot -> validate`。
5. 发布与当前Character Program匹配的AIIntentProgram。
6. 重绑训练敌人Control Source，并配置正式FinalIK Foot Placement组件。
7. 保存prefab与scene，domain reload后再次导出和校验Agent Snapshot。
