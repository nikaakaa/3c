## 1. 基线与迁移清单

- [ ] 1.1 记录当前`AgentAuthoringSchema.Version`、current spec版本和工作区版本差异。
- [ ] 1.2 列出`AgentControllerIntent`全部生产与消费点。
- [ ] 1.3 列出`AgentMacroLibrary`全部生产与消费点。
- [ ] 1.4 列出`AgentMacroCoverageEvaluator`全部生产与消费点。
- [ ] 1.5 列出`AgentPatchIR`全部JSON入口。
- [ ] 1.6 列出`AgentPatchOperation`全部字段与operation catalog。
- [ ] 1.7 列出Patch lowerer、typed command、compile session与handler之间的所有权边界。
- [ ] 1.8 列出Character与AI Snapshot exporter当前source revision来源。
- [ ] 1.9 列出`AgentPatchAuthoringService`全部action与事务顺序。
- [ ] 1.10 列出MCP bridge、Editor Window和技能中的旧action名称。
- [ ] 1.11 列出`bootstrap_ai_controller`全部调用点与当前业务依赖。
- [ ] 1.12 列出`add-corin-training-ai-demo`所有v16 Patch与bootstrap依赖。
- [ ] 1.13 确认Presentation、Body Motion、Foot Analysis和generated product继续只读。
- [ ] 1.14 建立旧Intent、Macro、Patch、bootstrap和兼容入口删除清单。

## 2. Document schema与领域模型

- [ ] 2.1 定义`btsmtl-agent-authoring-document.v1`唯一schema常量。
- [ ] 2.2 定义Document domain discriminator。
- [ ] 2.3 定义Document root identity字段。
- [ ] 2.4 定义service-owned sync header。
- [ ] 2.5 定义`baseSourceRevision`。
- [ ] 2.6 定义`baseContentHash`。
- [ ] 2.7 定义Character editable root。
- [ ] 2.8 定义AI editable root。
- [ ] 2.9 定义Graph、Node与Edge document entity。
- [ ] 2.10 定义StateMachine、State、Transition与Condition document entity。
- [ ] 2.11 定义Blackboard declaration document entity。
- [ ] 2.12 定义Timeline、Track、Clip、Marker与Curve document entity。
- [ ] 2.13 定义Action、Input、MotionWarp与lifecycle document entity。
- [ ] 2.14 定义AI Definition、Perception、Observation、Memory与Intent document entity。
- [ ] 2.15 定义已有entity stable authoring identity规则。
- [ ] 2.16 定义新entity document-local identity规则。
- [ ] 2.17 定义read-only context root。
- [ ] 2.18 定义capability和catalog版本身份。
- [ ] 2.19 定义unsupported/read-only entity保留规则。
- [ ] 2.20 删除外部`AgentControllerIntent`模型。
- [ ] 2.21 删除外部`AgentPatchIR`模型。
- [ ] 2.22 删除外部宽`AgentPatchOperation`模型。
- [ ] 2.23 删除v16/v17外部schema reader、writer与alias。

## 3. 严格JSON与规范化codec

- [ ] 3.1 建立Document唯一strict parser。
- [ ] 3.2 拒绝未知JSON字段。
- [ ] 3.3 拒绝重复JSON属性。
- [ ] 3.4 拒绝非法domain和entity discriminator。
- [ ] 3.5 拒绝缺失必需字段。
- [ ] 3.6 拒绝AI修改sync header。
- [ ] 3.7 拒绝AI修改read-only context。
- [ ] 3.8 建立Document唯一canonical writer。
- [ ] 3.9 固定UTF-8无BOM输出。
- [ ] 3.10 固定属性顺序。
- [ ] 3.11 固定entity排序。
- [ ] 3.12 固定浮点与curve数值格式。
- [ ] 3.13 规范化空集合与可空字段表示。
- [ ] 3.14 计算editable正文canonical hash。
- [ ] 3.15 计算完整Document semantic hash。
- [ ] 3.16 保证仅缩进或换行变化不改变semantic hash。
- [ ] 3.17 删除Document路径上的`JsonUtility`宽松解析。

## 4. Document Store

- [ ] 4.1 定义Unity项目级`AgentAuthoring/Documents`唯一根目录。
- [ ] 4.2 确保Document根位于`Assets/`之外。
- [ ] 4.3 定义domain子目录。
- [ ] 4.4 定义root-key规范算法。
- [ ] 4.5 从显式domain、root path与root identity计算唯一Document路径。
- [ ] 4.6 禁止调用方传入任意Document路径。
- [ ] 4.7 禁止按文件名或目录扫描寻找Document。
- [ ] 4.8 实现Document只读加载。
- [ ] 4.9 实现Document临时文件写入。
- [ ] 4.10 实现Document原子替换。
- [ ] 4.11 保证写入失败不破坏上一份Document。
- [ ] 4.12 将Document工作目录排除版本控制与Player内容。
- [ ] 4.13 删除任何Patch inbox、clipboard或watcher路径。

## 5. Live authoring revision

- [ ] 5.1 定义domain-aware authoring revision接口。
- [ ] 5.2 收集Character Definition正式可写字段。
- [ ] 5.3 收集Character全部可达Graph与StateMachine作者内容。
- [ ] 5.4 收集Character ConditionRuleGraph作者内容。
- [ ] 5.5 收集Character inline/shared Timeline作者内容。
- [ ] 5.6 收集Track、Clip、Marker和registered Curve作者内容。
- [ ] 5.7 收集Agent可写Input、ActionProfile和Blackboard依赖。
- [ ] 5.8 收集AI Definition与RootTree作者内容。
- [ ] 5.9 收集AI Blackboard、Perception与Intent作者内容。
- [ ] 5.10 收集受控Character input/request只读合同身份。
- [ ] 5.11 使用stable identity与canonical字段顺序计算revision。
- [ ] 5.12 从revision输入中排除generated Character Program。
- [ ] 5.13 从revision输入中排除Presentation Projection。
- [ ] 5.14 从revision输入中排除generated AIIntentProgram。
- [ ] 5.15 保证revision计算不调用build、publish或AssetDatabase保存。
- [ ] 5.16 让Character Snapshot使用live authoring revision。
- [ ] 5.17 让AI Snapshot使用live authoring revision。

## 6. Canonical Document Exporter

- [ ] 6.1 建立Character Document exporter。
- [ ] 6.2 建立AI Document exporter。
- [ ] 6.3 复用Graph topology与stable identity投影。
- [ ] 6.4 投影完整可写StateMachine结构。
- [ ] 6.5 投影完整可写Timeline结构。
- [ ] 6.6 投影完整可写Blackboard结构。
- [ ] 6.7 投影完整可写AI Perception与Intent结构。
- [ ] 6.8 投影只读Input、Action和Capability catalog。
- [ ] 6.9 投影只读Presentation identity。
- [ ] 6.10 投影只读Body Motion identity与参数。
- [ ] 6.11 投影只读Foot Analysis状态但不复制generated payload。
- [ ] 6.12 投影generated product identity与stale诊断。
- [ ] 6.13 把Document初始正文hash写入sync header。
- [ ] 6.14 把live source revision写入sync header。
- [ ] 6.15 保证重复导出未变化树产生相同canonical正文与hash。

## 7. 同步状态与checkout

- [ ] 7.1 定义`Clean`状态。
- [ ] 7.2 定义`TreeDirty`状态。
- [ ] 7.3 定义`DocumentDirty`状态。
- [ ] 7.4 定义`Conflict`状态。
- [ ] 7.5 从当前source revision计算treeChanged。
- [ ] 7.6 从当前Document正文hash计算documentChanged。
- [ ] 7.7 禁止序列化可编辑dirty布尔值。
- [ ] 7.8 实现无Document时显式checkout创建。
- [ ] 7.9 实现Clean状态显式checkout规范刷新。
- [ ] 7.10 实现TreeDirty且Document未改时显式checkout刷新。
- [ ] 7.11 实现DocumentDirty时checkout保留现有文件。
- [ ] 7.12 实现Conflict时checkout拒绝覆盖。
- [ ] 7.13 checkout response返回Document绝对路径。
- [ ] 7.14 checkout response返回同步状态和基线身份。
- [ ] 7.15 保证checkout不dirty或保存Unity资产。
- [ ] 7.16 保证checkout不触发Character或AI Program build。

## 8. Document Reconciler

- [ ] 8.1 定义`AgentDocumentReconciler`唯一入口。
- [ ] 8.2 建立当前Snapshot stable identity索引。
- [ ] 8.3 建立Document local identity symbol表。
- [ ] 8.4 校验Document root与当前root一致。
- [ ] 8.5 校验Document context版本与当前capability一致。
- [ ] 8.6 识别未变化现有entity并跳过命令。
- [ ] 8.7 识别现有entity可写字段变化。
- [ ] 8.8 识别Document新entity。
- [ ] 8.9 识别完整目标集合中被删除的可写entity。
- [ ] 8.10 拒绝删除read-only或unsupported entity。
- [ ] 8.11 确定StateMachine与State创建顺序。
- [ ] 8.12 确定Node与flow/property edge创建顺序。
- [ ] 8.13 确定Transition与ConditionRule创建顺序。
- [ ] 8.14 确定Timeline、Track、Clip、Marker与Curve创建顺序。
- [ ] 8.15 确定Blackboard declaration与引用绑定顺序。
- [ ] 8.16 确定AI Definition引用、Perception、Memory与Intent更新顺序。
- [ ] 8.17 确定edge重接与entity删除顺序。
- [ ] 8.18 保持现有entity stable authoring identity。
- [ ] 8.19 为新entity生成planning symbol。
- [ ] 8.20 输出唯一immutable `AgentMutationPlan`。
- [ ] 8.21 输出entity路径到typed command的source map。
- [ ] 8.22 输出最小planned diff。
- [ ] 8.23 禁止Reconciler修改Unity对象。

## 9. 内部Mutation链迁移

- [ ] 9.1 将`AgentPatchCommandKind`改名为`AgentMutationKind`。
- [ ] 9.2 将`AgentPatchCommand`类型族改名为`AgentMutation`类型族。
- [ ] 9.3 将`AgentPatchCommandPlan`改名为`AgentMutationPlan`。
- [ ] 9.4 将`AgentPatchCompileSession`改名为`AgentMutationSession`。
- [ ] 9.5 将`AgentPatchPreparation`改名为Document preparation结果。
- [ ] 9.6 将`AgentPatchCompiler`改名为Document mutation compiler facade。
- [ ] 9.7 将handler catalog迁移到Mutation命名。
- [ ] 9.8 保持StateMachine handler调用正式authoring API。
- [ ] 9.9 保持StateBehavior handler调用正式authoring API。
- [ ] 9.10 保持GraphLink handler调用正式authoring API。
- [ ] 9.11 保持Timeline handler调用正式authoring API。
- [ ] 9.12 保持AI handler调用正式AI authoring API。
- [ ] 9.13 保持TransactionOwnerCollector覆盖全部owner。
- [ ] 9.14 保持Compiler不拥有Undo、dirty、rollback或SaveAssets。
- [ ] 9.15 删除operation字符串catalog外部解析。
- [ ] 9.16 删除前序operation output外部合同。
- [ ] 9.17 删除旧Patch类型alias和兼容wrapper。

## 10. Dry-run与hash锁定

- [ ] 10.1 实现`dry_run_document` service入口。
- [ ] 10.2 dry-run重新加载确定性Document路径。
- [ ] 10.3 dry-run重新计算live source revision。
- [ ] 10.4 dry-run重新计算同步状态。
- [ ] 10.5 dry-run拒绝TreeDirty。
- [ ] 10.6 dry-run拒绝Conflict。
- [ ] 10.7 dry-run严格解析Document。
- [ ] 10.8 dry-run执行Reconciler。
- [ ] 10.9 dry-run执行Mutation preflight。
- [ ] 10.10 dry-run返回canonical document hash。
- [ ] 10.11 dry-run返回plan hash。
- [ ] 10.12 dry-run返回planned diff与metrics。
- [ ] 10.13 dry-run使用Document entity路径报告诊断。
- [ ] 10.14 保证dry-run不dirty、save或build。

## 11. Apply事务与反向同步

- [ ] 11.1 实现`apply_document` service入口。
- [ ] 11.2 apply要求非空expected document hash。
- [ ] 11.3 apply重新计算当前document hash。
- [ ] 11.4 apply拒绝hash变化。
- [ ] 11.5 apply重新计算live source revision。
- [ ] 11.6 apply拒绝tree revision变化。
- [ ] 11.7 apply拒绝TreeDirty与Conflict。
- [ ] 11.8 apply重新建立等价Mutation Plan。
- [ ] 11.9 apply校验plan hash与dry-run结果一致。
- [ ] 11.10 apply收集完整Character transaction owner。
- [ ] 11.11 apply收集完整AI transaction owner。
- [ ] 11.12 apply注册唯一Undo group。
- [ ] 11.13 apply执行typed handler mutation。
- [ ] 11.14 apply执行domain正式Validator。
- [ ] 11.15 任一mutation或validation错误时完整回滚。
- [ ] 11.16 成功时dirty实际touched owner。
- [ ] 11.17 成功时保存正式Unity资产。
- [ ] 11.18 Character apply显式发布正式Program与Projection。
- [ ] 11.19 AI apply显式发布正式AIIntentProgram。
- [ ] 11.20 generated product发布失败时回滚Unity事务。
- [ ] 11.21 从最终Character树重新导出canonical Document。
- [ ] 11.22 从最终AI树重新导出canonical Document。
- [ ] 11.23 写回真实stable identity与规范顺序。
- [ ] 11.24 更新base source revision与base content hash。
- [ ] 11.25 原子替换Document文件。
- [ ] 11.26 Document写回失败时不得报告Clean或完整成功。
- [ ] 11.27 apply成功response返回新revision、hash与Clean状态。

## 12. Conflict与显式rebase

- [ ] 12.1 实现`rebase_document` service入口。
- [ ] 12.2 rebase只允许现有合法Document。
- [ ] 12.3 rebase重新导出当前树规范投影。
- [ ] 12.4 rebase返回树与Document差异诊断。
- [ ] 12.5 rebase不自动修改Document editable body。
- [ ] 12.6 rebase要求显式确认当前树为新基线。
- [ ] 12.7 rebase只更新base source revision。
- [ ] 12.8 rebase把当前树canonical正文hash写为base content hash。
- [ ] 12.9 rebase保留AI目标正文。
- [ ] 12.10 rebase后重新推导DocumentDirty。
- [ ] 12.11 rebase不修改Unity资产。
- [ ] 12.12 rebase不触发build或publish。

## 13. MCP与Editor Window

- [ ] 13.1 保留唯一`manage_btsmtl_agent_authoring`工具。
- [ ] 13.2 增加`checkout_document` action。
- [ ] 13.3 增加`rebase_document` action。
- [ ] 13.4 增加`dry_run_document` action。
- [ ] 13.5 增加`apply_document` action。
- [ ] 13.6 保留只读`validate` action。
- [ ] 13.7 为apply增加`expected_document_hash`参数。
- [ ] 13.8 从MCP参数删除`patch_json`。
- [ ] 13.9 删除`export_snapshot` action。
- [ ] 13.10 删除`dry_run_patch` action。
- [ ] 13.11 删除`apply_patch` action。
- [ ] 13.12 删除`bootstrap_ai_controller` action。
- [ ] 13.13 拒绝全部旧action alias。
- [ ] 13.14 response返回Document绝对路径。
- [ ] 13.15 response返回同步状态、revision与hash。
- [ ] 13.16 response保留机器可读report与diff。
- [ ] 13.17 Window显示明确domain与root path。
- [ ] 13.18 Window显示Document路径与同步状态。
- [ ] 13.19 Window提供显式checkout按钮。
- [ ] 13.20 Window提供显式rebase按钮。
- [ ] 13.21 Window提供显式dry-run按钮。
- [ ] 13.22 Window提供显式apply按钮。
- [ ] 13.23 Window提供显式validate按钮。
- [ ] 13.24 删除Window Intent输入模式。
- [ ] 13.25 删除Window Patch JSON输入模式。
- [ ] 13.26 禁止selection或focus自动切换root并执行操作。
- [ ] 13.27 禁止Window或MCP复制application service生命周期。

## 14. Domain能力与Validator迁移

- [ ] 14.1 让Character Document覆盖当前全部正式可写State操作。
- [ ] 14.2 让Character Document覆盖当前全部正式可写Transition与Condition操作。
- [ ] 14.3 让Character Document覆盖当前全部正式可写Action lifecycle操作。
- [ ] 14.4 让Character Document覆盖当前全部正式可写Timeline操作。
- [ ] 14.5 让Character Document覆盖MotionWarp操作语义。
- [ ] 14.6 让Character Document覆盖Animation Channel操作语义。
- [ ] 14.7 让Character Document覆盖Marker Sync操作语义。
- [ ] 14.8 让Character Document覆盖registered Curve Channel操作语义。
- [ ] 14.9 让Character Document覆盖Action target与Input binding语义。
- [ ] 14.10 让AI Document覆盖Definition binding语义。
- [ ] 14.11 让AI Document覆盖Blackboard与Perception语义。
- [ ] 14.12 让AI Document覆盖Shared Flow/Value节点语义。
- [ ] 14.13 让AI Document覆盖Observation、Memory与Intent语义。
- [ ] 14.14 保持AI Graph禁止Character execution、Timeline和Transform副作用。
- [ ] 14.15 把Compile Report路径从operation id迁移为Document entity path。
- [ ] 14.16 把Synthesis评估从Macro coverage迁移为Document业务结构coverage。
- [ ] 14.17 保持通用Validator不硬编码Corin或业务状态名。
- [ ] 14.18 保持Presentation、Body Motion和Foot Analysis写入拒绝。

## 15. 清理旧路径

- [ ] 15.1 删除`AgentMacroLibrary`。
- [ ] 15.2 删除`AgentMacroCoverageEvaluator`。
- [ ] 15.3 删除`AgentPatchIdentityBinder`旧外部Patch绑定路径。
- [ ] 15.4 删除旧Patch JSON utility入口。
- [ ] 15.5 删除旧operation catalog外部schema描述。
- [ ] 15.6 删除旧Patch Editor Window状态与按钮。
- [ ] 15.7 删除MCP `patch_json`工具说明。
- [ ] 15.8 删除clipboard、快捷键、Patch inbox与文件watcher残留。
- [ ] 15.9 删除v16/v17 converter、reader、writer和alias。
- [ ] 15.10 确认没有第二个Graph、Timeline或AI mutation service。
- [ ] 15.11 确认runtime程序集不引用Document模型或Store。
- [ ] 15.12 确认Document目录不进入Player、Bundle或generated Program。

## 16. Active change与文档同步

- [ ] 16.1 重写`add-corin-training-ai-demo`的v16 Agent工作流描述。
- [ ] 16.2 把其Snapshot任务改为显式checkout Document。
- [ ] 16.3 把其Patch生成任务改为编辑正式Document。
- [ ] 16.4 把其same Patch apply任务改为same document hash apply。
- [ ] 16.5 把其re-export任务改为apply后canonical Document反向同步。
- [ ] 16.6 删除其`bootstrap_ai_controller`依赖。
- [ ] 16.7 更新`openspec/project.md`的Agent authoring当前链路。
- [ ] 16.8 更新`btsmtl-agent-authoring`技能主流程。
- [ ] 16.9 更新技能`current-contract.md`的schema、action和代码地图。
- [ ] 16.10 更新Editor工具说明与机器诊断文案。
- [ ] 16.11 删除文档中所有把v16/v17 Patch描述为外部正式入口的现行口径。

## 17. 编译与正式工具校验

- [ ] 17.1 使用规定参数编译受影响的BTSMTL Editor程序集。
- [ ] 17.2 使用规定参数编译受影响的Character Editor程序集。
- [ ] 17.3 每次dotnet build后立即执行`dotnet build-server shutdown`。
- [ ] 17.4 通过正式工具对Character root执行`checkout_document`。
- [ ] 17.5 通过正式工具对AI root执行`checkout_document`。
- [ ] 17.6 对未修改Document确认同步状态为Clean。
- [ ] 17.7 对修改后的Character Document执行`dry_run_document`。
- [ ] 17.8 对修改后的AI Document执行`dry_run_document`。
- [ ] 17.9 对需要迁移的真实Document执行同hash `apply_document`。
- [ ] 17.10 apply后确认Document已从最终树规范化并回到Clean。
- [ ] 17.11 对Character root执行正式`validate`。
- [ ] 17.12 对AI root执行正式`validate`。
- [ ] 17.13 运行`openspec validate refactor-agent-authoring-to-synced-json-document --strict --no-interactive`。
- [ ] 17.14 核对全部旧Patch、Macro、bootstrap和自动触发路径已删除。
- [ ] 17.15 核对tasks状态与唯一Document闭环一致。

