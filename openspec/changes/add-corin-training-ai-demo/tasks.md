## 1. 基线与依赖

- [ ] 1.1 读取本change的proposal、design、tasks和全部spec delta。
- [ ] 1.2 确认add-btsmtl-ai-controller-authoring全部任务完成。
- [ ] 1.3 确认extend-agent-authoring-for-ai-controller全部任务完成且唯一schema为v15。
- [ ] 1.4 记录Standalone玩家和训练敌人的ActorId、Control Source与Presentation Role。
- [ ] 1.5 记录训练敌人当前Neutral source序列化owner和全部引用。
- [ ] 1.6 记录Corin Character Definition、Program、Projection、World binding与Presentation引用。
- [ ] 1.7 记录玩家ActionTarget provider使用的Committed Observation binding。
- [ ] 1.8 确认没有现有Corin AI资产、MonoBehaviour Bot或Character RootTree AI SubTree。

## 2. Corin AI Definition与Perception

- [ ] 2.1 通过Agent v15 Snapshot确认可用AI authoring context。
- [ ] 2.2 创建Corin Training AI Controller Definition stable identity。
- [ ] 2.3 创建Corin Training AI RootTree stable identity。
- [ ] 2.4 将RootTree绑定为AIControllerTree。
- [ ] 2.5 绑定Corin CharacterPipelineDefinition。
- [ ] 2.6 创建Corin Training AIPerceptionProfile。
- [ ] 2.7 在Profile显式绑定玩家ActorId候选。
- [ ] 2.8 拒绝Team、Tag、名称、最近距离和Scene搜索配置。
- [ ] 2.9 校验Definition、RootTree、Character与Perception引用完整。

## 3. AI Blackboard与Tree

- [ ] 3.1 创建Controller-scope CurrentTarget declaration。
- [ ] 3.2 创建Controller-scope AttackRange declaration。
- [ ] 3.3 创建攻击重入或冷却所需正式可调declaration。
- [ ] 3.4 创建读取Configured Candidate分支。
- [ ] 3.5 创建选择显式目标分支。
- [ ] 3.6 创建CurrentTarget写入。
- [ ] 3.7 创建ActionTargetSnapshot写入。
- [ ] 3.8 创建目标距离读取和比较节点。
- [ ] 3.9 创建距离外MoveAxis方向输出。
- [ ] 3.10 创建攻击距离内zero MoveAxis输出。
- [ ] 3.11 创建Attack request一次性提交节点。
- [ ] 3.12 配置显式重入条件产生新activation。
- [ ] 3.13 保持全部节点属于Shared或AI capability。
- [ ] 3.14 确认Tree没有Character Action、Timeline、Motion或Transform节点。

## 4. Agent事务与Program

- [ ] 4.1 导出Corin Training AI完整v15 Snapshot。
- [ ] 4.2 根据stable identity生成唯一typed Patch。
- [ ] 4.3 对Patch执行正式dry-run。
- [ ] 4.4 消除Definition、Graph、Blackboard、Perception和Intent诊断。
- [ ] 4.5 使用完全相同Patch执行正式apply。
- [ ] 4.6 重新导出Snapshot并核对planned diff已经落地。
- [ ] 4.7 运行Agent v15 Validator。
- [ ] 4.8 编译Corin AI Semantic IR。
- [ ] 4.9 生成exact-byte Float32 AIIntentProgram资产。
- [ ] 4.10 绑定generated Program identity与source revision。
- [ ] 4.11 校验AI Program与Corin Character input/request catalog匹配。

## 5. 训练敌人迁移

- [ ] 5.1 在训练敌人正式Control Source owner绑定Corin AI Control Source。
- [ ] 5.2 绑定ControllerId、AI Program与Character Program。
- [ ] 5.3 绑定Committed Observation capability。
- [ ] 5.4 删除训练敌人Neutral source序列化引用。
- [ ] 5.5 确认不存在AI失败回退Neutral分支。
- [ ] 5.6 保持训练敌人ActorId和InitialBody不变。
- [ ] 5.7 保持Corin Character Definition和Program不变。
- [ ] 5.8 保持Projection、World binding和WorldSolver不变。
- [ ] 5.9 保持SimulatedActor Presentation和Foot Placement配置不变。
- [ ] 5.10 保持训练敌人无玩家Camera和设备输入。
- [ ] 5.11 更新Standalone Session roster的AI capability要求。
- [ ] 5.12 确认不支持AI的Network composition继续在Active前拒绝该配置。

## 6. 诊断与清理

- [ ] 6.1 确认AI diagnostics可关联ControllerId、ActorId和Observation Tick。
- [ ] 6.2 确认AI输出可关联Character InputSequence和SourceTick。
- [ ] 6.3 确认玩家与AI读取同一Committed Observation实例。
- [ ] 6.4 删除任何Corin AI试验MonoBehaviour。
- [ ] 6.5 删除任何Corin AI Transform、Tag、名称或Scene查询。
- [ ] 6.6 删除任何Character RootTree中的AI试验节点。
- [ ] 6.7 删除任何临时Patch、迁移器、菜单或YAML写入路径。
- [ ] 6.8 更新openspec/project.md和2v2vE文档的训练AI现状。
- [ ] 6.9 更新character-targeted-motion-warp-demo current spec的Neutral旧口径。

## 7. 编译与严格校验

- [ ] 7.1 使用规定参数构建AI Core与Float32程序集。
- [ ] 7.2 使用规定参数构建Character Runtime与Editor程序集。
- [ ] 7.3 使用规定参数构建BTSMTL Runtime与Editor程序集。
- [ ] 7.4 每次编译后立即执行dotnet build-server shutdown。
- [ ] 7.5 运行Corin AI Program artifact验证。
- [ ] 7.6 运行Agent v15正式validate。
- [ ] 7.7 运行openspec validate add-corin-training-ai-demo --strict --no-interactive。
- [ ] 7.8 核对tasks勾选与Corin唯一资产和运行链一致。

