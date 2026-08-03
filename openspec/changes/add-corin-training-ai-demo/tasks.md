## 1. 基线与依赖

- [x] 1.1 读取本change的proposal、design、tasks和全部spec delta。
- [x] 1.2 确认add-btsmtl-ai-controller-authoring全部任务完成。
- [x] 1.3 在`refactor-pose-graph-to-btsmtl-authoring-domain`完成后确认当前唯一Agent schema为Document v3目录包，CharacterController与AIController复用同一事务链。
- [x] 1.4 记录Standalone玩家和训练敌人的ActorId、Control Source与Presentation Role。
- [x] 1.5 记录训练敌人当前Neutral source序列化owner和全部引用。
- [x] 1.6 记录Corin Character Definition、Program、Projection、World binding与Presentation引用。
- [x] 1.7 记录玩家ActionTarget provider使用的Committed Observation binding。
- [x] 1.8 盘点现有半迁移Corin AI资产、失效MonoScript、过期Program、Missing组件、MonoBehaviour Bot和Character RootTree AI SubTree，并确定正式删除重建范围。

## 2. Corin AI Definition与Perception

- [x] 2.1 对已有合法AI Definition执行`btsmtl.checkout_document`并确认Document v3目录包中的AI controlled Character context只读。
- [x] 2.2 创建Corin Training AI Controller Definition stable identity。
- [x] 2.3 创建Corin Training AI RootTree stable identity。
- [x] 2.4 将RootTree绑定为AIControllerTree。
- [x] 2.5 绑定Corin CharacterPipelineDefinition。
- [x] 2.6 创建Corin Training AIPerceptionProfile。
- [x] 2.7 在Profile显式绑定玩家ActorId候选。
- [x] 2.8 拒绝Team、Tag、名称、最近距离和Scene搜索配置。
- [x] 2.9 校验Definition、RootTree、Character与Perception引用完整。
- [x] 2.10 将AIControllerDefinition拆为同名独立Unity脚本资产。
- [x] 2.11 在domain reload后确认Definition仍可由AssetDatabase按正式类型加载。

## 3. AI Blackboard与Tree

- [x] 3.1 创建Controller-scope CurrentTarget declaration。
- [x] 3.2 创建Controller-scope AttackRange declaration。
- [x] 3.3 创建攻击重入或冷却所需正式可调declaration。
- [x] 3.4 创建读取Configured Candidate分支。
- [x] 3.5 创建选择显式目标分支。
- [x] 3.6 创建CurrentTarget写入。
- [x] 3.7 创建ActionTargetSnapshot写入。
- [x] 3.8 创建目标距离读取和比较节点。
- [x] 3.9 创建距离外MoveAxis方向输出。
- [x] 3.10 创建攻击距离内zero MoveAxis输出。
- [x] 3.11 创建Attack request一次性提交节点。
- [x] 3.12 配置显式重入条件产生新activation。
- [x] 3.13 保持全部节点属于Shared或AI capability。
- [x] 3.14 确认Tree没有Character Action、Timeline、Motion或Transform节点。
- [x] 3.15 补齐AI Document对LoopStopType、CompareType、ConditionRuleGraph和AbortPolicy的完整投影。
- [x] 3.16 更新Agent技能合同，列出AI Shared节点与BT ConditionRule正式Document entity。
- [x] 3.17 复核Document v3中AI controlled Character context只读，独立Character Presentation域通过共享typed mutation编辑且不进入AI mutation。

## 4. Agent事务与Program

- [x] 4.1 checkout Corin Training AI完整Document v3目录包。
- [x] 4.2 根据stable identity编辑唯一AI editable正文。
- [x] 4.3 对Document执行正式dry-run。
- [x] 4.4 消除Definition、Graph、Blackboard、Perception和Intent诊断。
- [x] 4.5 使用dry-run返回的完全相同document hash执行正式apply。
- [x] 4.6 核对apply后的canonical Document已写回真实identity并回到Clean。
- [x] 4.7 运行`btsmtl.validate`正式Validator。
- [x] 4.8 编译Corin AI Semantic IR。
- [x] 4.9 生成exact-byte Float32 AIIntentProgram资产。
- [x] 4.10 绑定generated Program identity与source revision。
- [x] 4.11 校验AI Program与Corin Character input/request catalog匹配。

## 5. 训练敌人迁移

- [x] 5.1 在训练敌人正式Control Source owner绑定Corin AI Control Source。
- [x] 5.2 绑定ControllerId、AI Program与Character Program。
- [x] 5.3 绑定Committed Observation capability。
- [x] 5.4 删除训练敌人Neutral source序列化引用。
- [x] 5.5 确认不存在AI失败回退Neutral分支。
- [x] 5.6 保持训练敌人ActorId和InitialBody不变。
- [x] 5.7 保持Corin Character Definition和Program不变。
- [x] 5.8 保持Projection、World binding和WorldSolver不变。
- [x] 5.9 保持SimulatedActor Presentation角色和正式ordered Pose Plan合同不变。
- [x] 5.10 保持训练敌人无玩家Camera和设备输入。
- [x] 5.11 更新Standalone Session roster的AI capability要求。
- [x] 5.12 确认不支持AI的Network composition继续在Active前拒绝该配置。
- [x] 5.13 将训练敌人VisualRoot替换为怪兽FBX实例并停用旧Corin VisualRoot。
- [x] 5.14 将Host的Animancer、VisualRoot和Foot Placement引用统一迁移到怪兽VisualRoot。
- [x] 5.15 配置怪兽Generic Bip001 Rig v3、左右腿Physical chain与正式FootPlacement world-aware operation。
- [x] 5.16 确认怪兽Animator无Controller fallback且不申请Root Motion所有权。
- [x] 5.17 删除已废弃或Missing的Passthrough、Final IK与图外solver组件引用，确认Presentation只引用同根Rig v3与World-Aware Binding。

## 6. 诊断与清理

- [x] 6.1 确认AI diagnostics可关联ControllerId、ActorId和Observation Tick。
- [x] 6.2 确认AI输出可关联Character InputSequence和SourceTick。
- [x] 6.3 确认玩家与AI读取同一Committed Observation实例。
- [x] 6.4 删除任何Corin AI试验MonoBehaviour。
- [x] 6.5 删除任何Corin AI Transform、Tag、名称或Scene查询。
- [x] 6.6 删除任何Character RootTree中的AI试验节点。
- [x] 6.7 删除任何临时Patch、迁移器、菜单或YAML写入路径。
- [x] 6.8 更新openspec/project.md和2v2vE文档的训练AI现状。
- [x] 6.9 更新character-targeted-motion-warp-demo current spec的Neutral旧口径。
- [x] 6.10 更新agent-ai-controller-synthesis delta，锁定AI Shared节点与条件边authoring合同。

## 7. 编译与严格校验

- [x] 7.1 使用规定参数构建AI Core与Float32程序集。
- [ ] 7.2 使用规定参数构建Character Runtime与Editor程序集。
- [x] 7.3 使用规定参数构建BTSMTL Runtime与Editor程序集。
- [x] 7.4 每次编译后立即执行dotnet build-server shutdown。
- [x] 7.5 运行Corin AI Program artifact验证。
- [x] 7.6 运行Agent Document v3正式validate。
- [x] 7.7 运行openspec validate add-corin-training-ai-demo --strict --no-interactive。
- [x] 7.8 核对tasks勾选与Corin唯一资产和运行链一致。
