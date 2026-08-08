## 1. Blackboard authoring 数据模型

- [x] 1.1 删除 `PipelineBlackboardVariableAuthority` 枚举及全部成员引用。
- [x] 1.2 删除 `PipelineBlackboardVariableSyncPolicy` 枚举及全部成员引用。
- [x] 1.3 从 `BaseExposedProperty` 删除 authority/sync policy 序列化字段、属性和默认值。
- [x] 1.4 定义只保存稳定 `InputValueId` 的可选 typed Blackboard Input Binding payload。
- [x] 1.5 将现有 InputValueId 序列化形状迁入 Input Binding payload，不保留平铺镜像。
- [x] 1.6 将 fact projection kind、WindowType、WindowId、Digest 收敛为独立 typed Fact Projection payload。
- [x] 1.7 将基础 declaration 配置入口收敛为 identity/key/scope/lifetime/category，不再接收输入或投影参数。
- [x] 1.8 提供独立 `ConfigureInputBinding` authoring API。
- [x] 1.9 提供独立 `ConfigureFactProjection` authoring API。
- [x] 1.10 删除所有仍以 policy enum 选择 Input Binding 或 Fact Projection 的 helper。

## 2. Blackboard 校验合同

- [x] 2.1 将 InputDerived 校验改名为 Blackboard Input Binding 校验。
- [x] 2.2 校验 Input Binding 只允许 Character scope、Spawn lifetime。
- [x] 2.3 校验 InputValueId 非空、稳定并属于当前 Definition 的唯一 Input catalog。
- [x] 2.4 校验 Blackboard value kind 与 Program input value kind 精确一致。
- [x] 2.5 校验普通 InputProfile 与 Blackboard Input Binding 不重复声明同一 InputValueId。
- [x] 2.6 删除 PresentationOnly authority 等旧输入绑定校验分支。
- [x] 2.7 让 ActionWindow projection 只校验 Bool、Frame/Frame、WindowType、WindowId、Digest 和 Action Context provenance。
- [x] 2.8 删除 ActionWindow 对 `SyncFact` 的依赖和错误文本。
- [x] 2.9 明确拒绝 AI Blackboard 上的 Character Input Binding 与 ActionWindow Fact Projection。
- [x] 2.10 让 declaration 冲突校验比较新基础字段、Input Binding 和 Fact Projection，不比较旧策略。

## 3. 人工 Authoring UI

- [x] 3.1 从 Blackboard Graph Data Catalog editable details 删除 Authority 字段。
- [x] 3.2 从 Blackboard Graph Data Catalog editable details 删除 Sync Policy 字段。
- [x] 3.3 从 Blackboard Graph Data Catalog read-only details 删除 Authority 行。
- [x] 3.4 从 Blackboard Graph Data Catalog read-only details 删除 Sync Policy 行。
- [x] 3.5 让 Input Binding 详情只在 payload 存在或作者显式创建时显示 InputValueId。
- [x] 3.6 让 Fact Projection 详情独立显示 projection kind 和 ActionWindow payload。
- [x] 3.7 更新 Blackboard 新建入口，只创建基础 declaration，不填充网络默认值。
- [x] 3.8 更新 Timeline TreeClip Inspector 的 declaration 摘要和选择逻辑。
- [x] 3.9 更新 Timeline Decision validation 的 local gate 与 projected window 判断。
- [x] 3.10 删除 Inspector、tooltip、搜索文本和诊断中的 `InputDerived`、`SyncFact` 网络策略措辞。
- [x] 3.11 将 Action Runtime/Model Debug 的 HitWindow SyncFact 文案改为实际 `ActionWindowFact` 与 Model coverage。

## 4. Character 与 AI 编译前校验

- [x] 4.1 更新 Character authoring discovery，投影基础 declaration、Input Binding 和 Fact Projection。
- [x] 4.2 更新 Character Graph Validator 的 Blackboard declaration 校验与机器诊断。
- [x] 4.3 更新 Action target eligibility mutation handler，使用独立 Input Binding API。
- [x] 4.4 更新 Timeline Window mutation handler，使用独立 Fact Projection API。
- [x] 4.5 更新 TrainingEnemy authoring builder，删除 authority/sync policy 参数。
- [x] 4.6 更新 AIIntentProgramCompiler，只按 AI scope/lifetime 与 typed value 校验 AI Blackboard。
- [x] 4.7 更新 AI Graph Validator，删除 `LocalOnly + None` 固定策略要求。
- [x] 4.8 删除所有 builder、factory 和 call site 中填充 `LocalOnly`、`ClientPredicted`、`None`、`InputDerived`、`SyncFact` 的代码。
- [x] 4.9 更新 targeted MotionWarp demo 的 ActionTarget 校验和诊断，统一引用正式 Input Binding。

## 5. Semantic IR Frontend

- [x] 5.1 从 Blackboard catalog emission 删除 `Authority` field。
- [x] 5.2 从 Blackboard catalog emission 删除 `SyncPolicy` field。
- [x] 5.3 只为存在的 Input Binding 输出 canonical `InputValueId` field。
- [x] 5.4 只为存在的 Fact Projection 输出 canonical projection payload。
- [x] 5.5 更新 CharacterSimulationCatalogCompiler，按 Input Binding 收集 Blackboard input value。
- [x] 5.6 更新重复 InputValueId 和类型错误的 source location 与机器诊断。
- [x] 5.7 更新 Semantic IR Reader text/JSON，确认不再输出 Authority/SyncPolicy。
- [x] 5.8 提升 Character Semantic Frontend compiler version。
- [x] 5.9 提升 Semantic IR artifact 与 payload format version。
- [x] 5.10 删除旧 Semantic IR field 的 decode、默认补值和兼容分支。

## 6. Float32 与 Fixed Target Program

- [x] 6.1 删除 `ProgramCatalogFieldId.SyncPolicy`。
- [x] 6.2 收敛剩余 `ProgramCatalogFieldId` 数值和 runtime field count。
- [x] 6.3 为可选 InputValueId/Projection 提供严格的 catalog field 查询，不把缺失当作旧格式 fallback。
- [x] 6.4 将 Float32 `BuildInputDerivedBindings` 改为按 InputValueId 建立 Blackboard input binding。
- [x] 6.5 将 Fixed `BuildInputDerivedBindings` 改为按 InputValueId 建立 Blackboard input binding。
- [x] 6.6 将 Float32 `InputDerivedStateBinding` 及执行阶段改为中性 Blackboard Input Binding 命名。
- [x] 6.7 将 Fixed `InputDerivedStateBinding` 及执行阶段改为中性 Blackboard Input Binding 命名。
- [x] 6.8 保持两个 Target 在 Timeline Decision 和 Graph control 前写入同一 transaction。
- [x] 6.9 保持 Float32 ActionWindow Fact Projection runtime 只读取 projection payload。
- [x] 6.10 保持 Fixed ActionWindow Fact Projection runtime 只读取 projection payload。
- [x] 6.11 提升 Float32 Program artifact、program/layout format 与 Target ABI。
- [x] 6.12 提升 Fixed Program artifact、program/layout format 与 Target ABI。
- [x] 6.13 更新 generated wrapper、ProgramCatalog 和 composition identity 的版本门禁。
- [x] 6.14 删除旧 Program artifact reader、字段缺失默认值和 ABI 兼容路径。
- [ ] 6.15 让 Program Fact Projection enum 只包含正式 ActionWindow kind，并以字段缺失唯一表达无投影。

## 7. Agent Document v3 schema

- [x] 7.1 从 Blackboard declaration document model 删除 `authority`。
- [x] 7.2 从 Blackboard declaration document model 删除 `syncPolicy`。
- [x] 7.3 删除旧平铺 `inputId`、projection 和 window payload 字段。
- [x] 7.4 增加稀疏可选 `inputBinding.inputValueId` typed payload。
- [x] 7.5 增加稀疏可选 `factProjection` typed payload。
- [x] 7.6 更新 strict parser，拒绝 authority/syncPolicy、旧平铺字段、空 payload 和未知字段。
- [x] 7.7 更新 canonical writer，省略不存在的 binding/projection 并保持字段顺序稳定。
- [x] 7.8 更新 semantic hash 与 document hash 输入，锁定新 Blackboard schema revision。
- [x] 7.9 更新 Character Snapshot exporter，输出新 declaration 形状。
- [x] 7.10 更新 AI Snapshot exporter，输出新 declaration 形状。
- [x] 7.11 更新 Package Mapper，按新 payload 构造 canonical snapshot。
- [x] 7.12 更新 Document Reconciler，按基础 declaration、Input Binding、Fact Projection 分别对账。
- [x] 7.13 更新 Agent Mutation command 与 immutable plan，删除 authority/sync policy 参数。
- [x] 7.14 更新 Agent Mutation handler，调用三个正式 authoring API。
- [x] 7.15 更新 Agent Validator 和 Report 的字段路径、错误 code 与建议。
- [x] 7.16 让旧 v3 package 在 mutation 前失败并要求显式重新 checkout。
- [x] 7.17 删除旧 package reader、转换器、默认值和兼容 operation 字段。

## 8. Document 事务与 schema normalization

- [x] 8.1 定义 RootTree Blackboard authoring schema revision。
- [x] 8.2 让 checkout 和 canonical Snapshot 记录当前 Blackboard schema revision。
- [x] 8.3 让 Reconciler 在旧 revision 到新 revision时计划完整 typed Blackboard normalization。
- [x] 8.4 让 normalization 只通过正式 declaration/Input Binding/Fact Projection Mutation 写入。
- [x] 8.5 将 schema revision 更新与全部 owner mutation 放入同一 Undo/Save/package publish 事务。
- [x] 8.6 任一 Validator、Save、reverse export 或 package publish 失败时回滚 schema revision 和全部 owner。
- [x] 8.7 normalization 成功后删除 Unity YAML 中旧序列化字段，不保留 `FormerlySerializedAs`。
- [x] 8.8 保证已迁移 revision 的普通 apply 只按目标 diff 工作，不重复全量重写。
- [x] 8.9 让AI纯schema normalization在受控Character Program过期时只保存authoring并保留AIIntentProgram stale，真实AI语义变化仍要求当前Program。

## 9. MCP bridge 与技能合同

- [x] 9.1 更新 MCP bridge 的 Document schema/report 描述，透传新 Blackboard 字段路径。
- [x] 9.2 保持 checkout、rebase、dry-run、apply、validate 五个生命周期工具不变。
- [x] 9.3 确认 bridge 不新增 policy 转换参数或 Blackboard 专用旁路工具。
- [x] 9.4 更新 `.codex/skills/btsmtl-agent-authoring/SKILL.md` 的 Blackboard authoring 合同。
- [x] 9.5 更新技能引用文档中的 Character/AI Blackboard JSON 示例。
- [x] 9.6 删除技能内 `Authority`、`SyncPolicy`、`InputDerived`、`SyncFact` 的 authoring 指导。

## 10. 正式资产迁移

- [x] 10.1 对 `CorinPlayableRootTree.asset` 生成新 Document v3 package。
- [x] 10.2 将 Corin ActionTarget declaration 迁入正式 Input Binding。
- [x] 10.3 将 Corin Hit、IFrame、ComboAccept、RecoveryEarly、RecoveryLate、RecoveryOpen declaration 迁入正式 Fact Projection。
- [x] 10.4 将 Corin Config 和普通本地 declaration 迁为无 binding、无 projection 的基础 declaration。
- [x] 10.5 通过同一 Document v3 apply 原子保存 Corin RootTree 并 reverse export。
- [x] 10.6 对 `TrainingEnemyCharacterRootTree.asset` 生成新 Document v3 package。
- [x] 10.7 将 TrainingEnemy ActionTarget declaration 迁入正式 Input Binding。
- [x] 10.8 通过同一 Document v3 apply 原子保存 TrainingEnemy RootTree 并 reverse export。
- [x] 10.9 对 `CorinTrainingAIController.AIRootTree.asset` 生成新 Document v3 package。
- [x] 10.10 将 Corin Training AI declarations 迁为纯 AI scope/lifetime declaration。
- [x] 10.11 通过同一 Document v3 apply 原子保存 Corin Training AI RootTree 并 reverse export。
- [x] 10.12 对 `TrainingEnemyAIController.AIRootTree.asset` 生成新 Document v3 package。
- [x] 10.13 将 TrainingEnemy AI declarations 迁为纯 AI scope/lifetime declaration。
- [x] 10.14 通过同一 Document v3 apply 原子保存 TrainingEnemy AI RootTree 并 reverse export。
- [x] 10.15 确认四个资产不再序列化 authority/sync policy 或旧平铺 input/projection 字段。
- [x] 10.16 删除迁移过程中产生的旧 Document package 和 staging 内容，只保留新正式 package。

## 11. Generated product 重新发布

- [x] 11.1 通过正式显式 Character Build 发布 Corin Semantic IR。
- [x] 11.2 通过同一 Build transaction 发布 Corin Float32/Fixed Target Program 与 wrapper。
- [ ] 11.3 通过正式显式 Character Build 发布 TrainingEnemy Semantic IR。
- [ ] 11.4 通过同一 Build transaction 发布 TrainingEnemy Float32/Fixed Target Program 与 wrapper。
- [ ] 11.5 通过正式 AI apply/build 发布 CorinTrainingAIController AIIntentProgram。
- [ ] 11.6 通过正式 AI apply/build 发布 TrainingEnemyAIController AIIntentProgram。
- [ ] 11.7 更新引用这些 Program identity 的正式 catalog、manifest 和 Network Test product artifact。
- [ ] 11.8 删除旧 `.csir`、旧 Target Program、旧 wrapper 和旧 AIIntentProgram，不保留并行产物。

## 12. 自动化测试

- [ ] 12.1 建立聚焦 Blackboard authoring/compilation 的 EditMode test assembly，不引用产品运行场景。
- [ ] 12.2 测试基础 declaration 不包含 authority/sync policy 字段或 API 参数。
- [ ] 12.3 测试合法 ActionTarget Input Binding 通过 Character/Spawn、identity 和类型校验。
- [ ] 12.4 测试空、重复、缺失或类型不匹配的 InputValueId 明确失败。
- [ ] 12.5 测试合法 ActionWindow Fact Projection 不依赖 SyncFact 仍生成确定性 projection descriptor。
- [ ] 12.6 测试非法 value kind、scope/lifetime、WindowType、WindowId 或 Action Context 明确失败。
- [ ] 12.7 测试 AI Blackboard 接受合法 AI scope/lifetime 且拒绝 Character Input Binding/Fact Projection。
- [ ] 12.8 测试 Semantic IR canonical round-trip 不包含 `Authority` 或 `SyncPolicy` field。
- [ ] 12.9 测试相同 source 连续编译得到相同 SemanticHash。
- [ ] 12.10 测试 Float32 Target 只按 InputValueId 建立 input-to-state binding。
- [ ] 12.11 测试 Fixed Target 只按 InputValueId 建立 input-to-state binding。
- [ ] 12.12 测试 Float32/Fixed 对缺失输入、错误类型和重复 binding 给出一致失败。
- [ ] 12.13 测试旧 Semantic IR、Float32 Program 和 Fixed Program 在版本门禁处拒绝。
- [ ] 12.14 测试新 Agent Document Blackboard JSON canonical round-trip。
- [ ] 12.15 测试 authority/syncPolicy 和旧平铺 payload 被 strict parser 拒绝。
- [ ] 12.16 测试 Reconciler 为旧 Blackboard schema revision 生成唯一 typed normalization plan。
- [ ] 12.17 测试 normalization 失败时 Unity owner、revision 和 package 全部回滚。
- [ ] 12.18 增加仓库资产静态测试，确保正式 `.asset` 不含旧序列化字段名。

## 13. 最终清理与对账

- [x] 13.1 全仓搜索并删除 `PipelineBlackboardVariableAuthority` 残留。
- [x] 13.2 全仓搜索并删除 `PipelineBlackboardVariableSyncPolicy` 残留。
- [x] 13.3 全仓搜索并删除 `ProgramCatalogFieldId.SyncPolicy` 残留。
- [x] 13.4 全仓搜索并删除 Document `authority/syncPolicy` 业务字段残留。
- [x] 13.5 确认 `InputDerived` 只可出现在历史 archive，不存在于当前代码、资产、技能和 generated product。
- [x] 13.6 确认 `SyncFact` 只可出现在历史 archive，不存在于当前代码、资产、技能和 generated product。
- [x] 13.7 确认 Action lifecycle operation、ActionState、ActionFact 和 terminal reason 链路未被删除或改接。
- [x] 13.8 确认 Network Model 仍只按 fact kind/producer coverage 决定复制，不读取 Blackboard。
- [x] 13.9 对照 current specs、最终代码和四个正式资产，确认不存在旧 reader、fallback、双写或第二 mutation 路径。
