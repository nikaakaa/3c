# Corin 训练 AI 实现清单

## 用途

本文记录apply后的唯一资产、Document、Prefab与运行链事实。最终验收仍以canonical Document、generated AI Program和`tasks.md`对账结果为准。

## Standalone Actor

| Actor | ActorId | Control Source | Presentation Role | World Binding |
|---|---|---|---|---|
| 玩家Corin | `LocalActor` | Player Control Source | `LocalOwner` | `Corin.LocalBody` |
| 训练敌人 | `corin-training-enemy` | `AICharacterControlSource`，identity为`ai-controller/Corin.TrainingAI` | `SimulatedActor` | `Corin.TrainingEnemyBody` |

玩家的`ActionTarget`由`SessionActorActionTargetInputProvider`显式绑定训练敌人。训练AI的Perception显式绑定`LocalActor`。两者都消费Session上一轮提交的Committed Observation，不读取对方Transform。

## 公共角色链

- Character Definition GUID：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- Character Program Id：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- Generated Character Program文件当前Hash：`c76a7ba1cd31f8caf54e350cea52b07345b4e2a07841d8598e28528a2c8e088d`；Corin Character Definition的`m_SimulationProgram`当前为空，未正式绑定该产品。
- 训练敌人的目标装配仍是复用Corin Definition、Program、Projection、Unity WorldSolver、Body Presentation与SimulatedActor表现角色；在Character产品重新发布前，该目标链暂时不可运行。
- AI只生成`CharacterSimulationInput`与Action Request，不拥有移动求解、Action admission、Timeline、MotionWarp或动画播放。

## 正式AI资产

| 资产 | GUID | 正式身份 |
|---|---|---|
| `CorinTrainingAIControllerDefinition.asset` | `2d17195dbd81b36419e464bf91f40f49` | `AIControllerDefinition`，ControllerId为`Corin.TrainingAI` |
| `CorinTrainingAIController.AIRootTree.asset` | `85b261b17e94e1c4784c45be082f5101` | `AIControllerTree` root |
| `CorinTrainingAIPerceptionProfile.asset` | `348860615c73c1245a95f319607280e1` | 候选仅`LocalActor`，排序为`DistanceThenActorId` |
| `Generated/CorinTrainingAIControllerDefinition.AIIntentProgram.asset` | `4a1f9229c9bc9874f8ace0637e158dcd` | `ai-intent:Corin.TrainingAI` |

Definition与Perception的失效对象已通过正式Unity资产生命周期删除并按同一路径重建；有效RootTree资产壳保留stable identity，Graph语义只通过Agent Document v3写入。旧过期Program已删除，并由apply重新发布，不存在兼容资产或第二份配置。

AI Program：

- Program Hash：`0f0f0ae2c622023e12406f03404b69fe4a6990a3746540f988c69920a3347750`。
- Source Revision：`4129a0c707532866a946bb306595514bc437e714c90d120a92ef0ac55be92ffa`。
- AI Program保存的Character Program Hash：`6c3a1421c560fb33c89e4ca4beaf37d4b602782203a6df7e8af1b477a144cdf1`，与当前generated Character Program文件Hash不一致。

## Canonical Document

- 包路径：`AgentAuthoring/Documents/AIController/Corin.TrainingAI-e3e8f0c55937419b.btsmtl`。
- 最终Document Hash：`d5abffc93f23704a05c33d0864174c84b1aa9274789b73eb9c2e53387622e924`。
- exact-hash apply结果：`applied=true`、`saved=true`、`syncState=Clean`。
- 历史apply后的Validator曾完成1次AI Program编译与1次语义校验。2026-08-03复核时，AI Validator因受控Character Definition没有绑定Float32 Program而失败；当前AI Program不能视为有效产物。
- controlled Character capability只存在于只读context；AI editable不拥有Character Presentation mutation。

Blackboard与行为：

- Controller scope：`CurrentTarget`、`AttackRange=2.5`、`AttackCooldownTicks=36`。
- Graph scope：`StopMove=(0,0)`。
- 行为链：选择显式候选 -> 写CurrentTarget -> 写ActionTarget -> 距离外输出目标方向MoveAxis -> 距离内输出zero MoveAxis -> 单次提交Attack -> 等待36 ticks后产生新activation。
- 条件边保留完整ConditionRuleGraph与AbortPolicy；Tree不包含Character Action、Timeline、Motion或Transform节点。

## 训练敌人Prefab

- prefab：`Assets/Prefabs/Characters/RuntimeProfiles/AI/CorinStandaloneTrainingEnemy.prefab`，GUID为`92b30edb28014fe8a1c58b893df987d4`。
- 根组件使用`AICharacterControlSource`并绑定正式Definition，不再保存Neutral Control Source。
- ActorId仍为`corin-training-enemy`，Character Definition、InitialBody、World binding、Projection与SimulatedActor角色不变。
- `EnemyVisualRoot`是唯一激活表现根；旧`VisualRoot`停用。
- Host的Animancer、VisualRoot与Foot Placement都指向`EnemyVisualRoot`。
- 怪兽Animator不绑定Controller，`ApplyRootMotion=false`。
- 怪兽根配置正式Rig v3、`CharacterAnimationRigBinding`、`CharacterWorldAwarePresentationBinding`与同一Pose Plan中的FootPlacement operation；不存在Missing、Passthrough、Final IK或图外solver引用。
- prefab不拥有玩家Camera或设备输入。

## 清理与边界

- Corin Character RootTree不包含AI节点脚本引用；AI决策只存在于独立AI RootTree。
- 项目不存在Corin试验Bot MonoBehaviour、Transform直写、Tag/名称/Scene查询、临时Patch、迁移器、YAML writer或Neutral fallback。
- Local Float32 composition接受`CommittedObservation | TransactionalState`。不支持该能力集合的网络composition会在Active前拒绝训练AI配置，不会退回Neutral输入。

## 当前阻断

- `ThirdPersonClient.Runtime.csproj`与`ThirdPersonClient.Editor.csproj`均已使用规定参数构建通过，结果都是0 warning、0 error；每次构建后已立即执行`dotnet build-server shutdown`。
- Corin Character正式Validator报告全部可达Timeline Clip与Pose Source Binding的Foot Analysis artifact因当前Rig/Calibration输入identity变化而stale。
- `separate-foot-placement-planning-and-leg-ik`仍需完成11.10 artifact重建、11.11/11.12 Character产品发布和11.13/11.14生成Plan对账。
- 上述依赖完成后，必须重新发布AIIntentProgram并再次执行AI Document Validator；不得手工把旧Character Program绑回Definition，也不得让训练敌人回退Neutral。
