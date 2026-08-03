> 串行位置：本change是`openspec/character-pipeline-serial-execution.md`阶段1。当前只收口剩余10项Document v2可复用基础；不得在这里扩展Presentation editable、Document v3 schema、Pose UI、Corin资产或产品Build。v3升级只由`refactor-pose-graph-to-btsmtl-authoring-domain`继续完成。

## 1. v2边界与删除清单

- [x] 1.1 固定`btsmtl-agent-authoring-document.v2`唯一schema常量。
- [x] 1.2 固定`.btsmtl/`文档包目录后缀。
- [x] 1.3 固定CharacterController与AIController domain discriminator。
- [x] 1.4 列出v1单文件Document模型的全部生产点。
- [x] 1.5 列出v1单文件Document模型的全部消费点。
- [x] 1.6 列出`.btsmtl.json.sync`基线文件的全部读写点。
- [x] 1.7 列出`manage_btsmtl_agent_authoring`全部注册与调用点。
- [x] 1.8 列出当前Snapshot exporter可输出的全部Node类型。
- [x] 1.9 列出`AgentNodeEmitterRegistry`可创建的全部Node类型。
- [x] 1.10 列出GraphLink handler现有Flow与Property Edge能力。
- [x] 1.11 列出Document Reconciler现有unsupported分支。
- [x] 1.12 建立v1、Patch、Macro、bootstrap、旧tool和兼容入口删除清单。

## 2. 文档包manifest与同步模型

- [x] 2.1 定义`AgentAuthoringPackageManifest`。
- [x] 2.2 在manifest中定义schemaVersion。
- [x] 2.3 在manifest中定义domain。
- [x] 2.4 在manifest中定义rootIdentity。
- [x] 2.5 在manifest中定义规范文件清单。
- [x] 2.6 定义`AgentAuthoringPackageSync`。
- [x] 2.7 在sync中定义baseSourceRevision。
- [x] 2.8 在sync中定义baseEditableHash。
- [x] 2.9 在sync中定义baseContextHash。
- [x] 2.10 删除业务正文中的sync header。
- [x] 2.11 定义package projection结果。
- [x] 2.12 定义package load state。
- [x] 2.13 定义package publish结果。
- [x] 2.14 定义package path到业务entity path的映射规则。

## 3. 可编辑领域分片模型

- [x] 3.1 定义`editable/controller.json`模型。
- [x] 3.2 定义`editable/blackboard.json`模型。
- [x] 3.3 定义Character `editable/actions.json`模型。
- [x] 3.4 定义AI `editable/ai/perception.json`模型。
- [x] 3.5 定义Graph目录identity编码规则。
- [x] 3.6 定义Timeline目录identity编码规则。
- [x] 3.7 定义`graph.json`根模型。
- [x] 3.8 定义Graph owner entityId。
- [x] 3.9 定义Graph owner slot。
- [x] 3.10 定义稳定Graph kind。
- [x] 3.11 定义稀疏Node模型。
- [x] 3.12 定义Node stable/local identity。
- [x] 3.13 定义Node kind。
- [x] 3.14 定义Node typed properties。
- [x] 3.15 定义Flow Edge目标模型。
- [x] 3.16 定义Property Edge目标模型。
- [x] 3.17 定义Edge endpoint逻辑port key。
- [x] 3.18 定义Graph reference模型。
- [x] 3.19 定义`layout.json`模型。
- [x] 3.20 定义`timeline.json`模型。
- [x] 3.21 定义`curves.json`模型。
- [x] 3.22 定义Curve完整payload模型。
- [x] 3.23 定义`local:<meaningful-id>`语法。
- [x] 3.24 禁止Node kind与Graph kind原地变更。

## 4. 系统anchor模型

- [x] 4.1 定义`@root` anchor。
- [x] 4.2 定义`@enter` anchor。
- [x] 4.3 定义`@exit` anchor。
- [x] 4.4 定义`@any` anchor。
- [x] 4.5 定义`@onEnter` anchor。
- [x] 4.6 定义`@onExit` anchor。
- [x] 4.7 定义`@timelineEnter` anchor。
- [x] 4.8 定义`@result` anchor。
- [x] 4.9 为每个Graph kind声明允许anchor。
- [x] 4.10 为每个anchor声明逻辑port。
- [x] 4.11 禁止anchor出现在editable nodes集合。
- [x] 4.12 禁止anchor拥有layout与properties。
- [x] 4.13 建立Unity系统Node到anchor的导出映射。
- [x] 4.14 建立anchor到当前Unity系统Node的解析映射。

## 5. Authoring Capability Catalog

- [x] 5.1 建立`AgentAuthoringCapabilityCatalog`唯一owner。
- [x] 5.2 定义Node capability descriptor。
- [x] 5.3 为descriptor定义稳定kind。
- [x] 5.4 为descriptor定义允许Graph kind。
- [x] 5.5 为descriptor定义typed property schema。
- [x] 5.6 为descriptor定义正式默认值。
- [x] 5.7 为descriptor定义逻辑Flow ports。
- [x] 5.8 为descriptor定义逻辑Property ports。
- [x] 5.9 为descriptor定义资产引用类型。
- [x] 5.10 为descriptor定义create lowering。
- [x] 5.11 为descriptor定义configure lowering。
- [x] 5.12 为descriptor定义delete lowering。
- [x] 5.13 定义Graph kind descriptor。
- [x] 5.14 为Graph kind定义owner slot。
- [x] 5.15 为Graph kind定义anchor集合。
- [x] 5.16 为Graph kind定义Node capability集合。
- [x] 5.17 为Graph kind定义inline/shared ownership规则。
- [x] 5.18 将`AgentNodeEmitterRegistry`登记迁入统一catalog。
- [x] 5.19 将Snapshot exporter节点识别迁入统一catalog。
- [x] 5.20 将Graph policy校验迁入统一catalog。
- [x] 5.21 将Node handler解析迁入统一catalog。
- [x] 5.22 将Validator capability读取迁入统一catalog。
- [x] 5.23 删除C# type name外部解析。
- [x] 5.24 删除显示名与类型别名外部解析。
- [x] 5.25 对不可完整往返capability返回`authoring_capability_incomplete`。

## 6. 只读context catalog

- [x] 6.1 定义`context/node-catalog.json`。
- [x] 6.2 从唯一capability catalog投影Node kind。
- [x] 6.3 投影typed property、默认值与逻辑port。
- [x] 6.4 定义`context/graph-kinds.json`。
- [x] 6.5 投影Graph kind、owner slot与anchor。
- [x] 6.6 定义`context/asset-catalog.json`。
- [x] 6.7 投影当前Definition可引用Input identity。
- [x] 6.8 投影当前Definition可引用Action identity。
- [x] 6.9 投影当前Definition可引用Timeline identity。
- [x] 6.10 投影当前Definition可引用Blackboard identity。
- [x] 6.11 投影AI Perception与受控Character合同。
- [x] 6.12 定义`context/dependencies.json`。
- [x] 6.13 投影Graph、Timeline、owner与shared asset依赖。
- [x] 6.14 投影影响编辑决策的Presentation摘要。
- [x] 6.14.1 删除MotionMatchingSelectionInput、AnimationSelection port与Pose Graph MarkerSync旧摘要。
- [x] 6.14.2 把SelectedPosePlayer与BlendStack投影为state-local provider/source owner。
- [x] 6.14.3 把Motion Matching限制为PoseState provider摘要并排除Gameplay playback字段。
- [x] 6.14.4 分离ActionPlaybackInput与AnimationSlot的Action AnimationChannel owner摘要。
- [x] 6.14.5 校验AnimationSlot是有限Action channel唯一consumer。
- [x] 6.14.6 从Action producer摘要删除Blend Space旧字段。
- [x] 6.14.7 从Action producer摘要删除固定Timeline的sourceKind字段。
- [x] 6.15 投影影响编辑决策的Body Motion摘要。
- [x] 6.16 投影generated product identity与stale摘要。
- [x] 6.17 排除Foot Analysis generated payload。
- [x] 6.18 排除Program与Projection payload。
- [x] 6.19 排除runtime state、instance id与时间戳。
- [x] 6.20 拒绝AI修改context文件。
- [x] 6.20.1 保持Presentation context不进入Reconciler与Mutation lowering。

## 7. Strict multi-file codec

- [x] 7.1 建立manifest strict parser。
- [x] 7.2 建立sync strict parser。
- [x] 7.3 建立controller strict parser。
- [x] 7.4 建立blackboard strict parser。
- [x] 7.5 建立actions strict parser。
- [x] 7.6 建立AI perception strict parser。
- [x] 7.7 建立Graph strict parser。
- [x] 7.8 建立layout strict parser。
- [x] 7.9 建立Timeline strict parser。
- [x] 7.10 建立Curve strict parser。
- [x] 7.11 建立context catalog strict parser。
- [x] 7.12 拒绝重复JSON属性。
- [x] 7.13 拒绝未知字段。
- [x] 7.14 拒绝未知kind、property、port和anchor。
- [x] 7.15 拒绝非法stable/local identity。
- [x] 7.16 拒绝非有限数值与非法Curve。
- [x] 7.17 建立逐文件canonical writer。
- [x] 7.18 固定UTF-8无BOM。
- [x] 7.19 固定字段与entity顺序。
- [x] 7.20 固定数值格式。
- [x] 7.21 省略正式默认值。
- [x] 7.22 省略空集合与无关字段。
- [x] 7.23 计算逐文件semantic hash。
- [x] 7.24 计算editableHash。
- [x] 7.25 计算contextHash。
- [x] 7.26 计算整包documentHash。
- [x] 7.27 拒绝manifest之外的未登记JSON文件。
- [x] 7.28 删除v1单文件codec。
- [x] 7.29 删除v15-v17 Patch reader与converter。

## 8. 目录Document Store

- [x] 8.1 保留Unity项目级`AgentAuthoring/Documents`唯一根目录。
- [x] 8.2 从domain、root path与root identity计算root-key。
- [x] 8.3 计算确定性`.btsmtl/`目录路径。
- [x] 8.4 禁止调用方传入任意package path。
- [x] 8.5 禁止目录扫描寻找替代package。
- [x] 8.6 实现package存在性查询。
- [x] 8.7 实现manifest文件清单加载。
- [x] 8.8 实现完整package只读加载。
- [x] 8.9 实现staging目录创建。
- [x] 8.10 将全部规范文件写入staging。
- [x] 8.11 严格重读staging package。
- [x] 8.12 校验staging documentHash。
- [x] 8.13 实现当前package rollback目录切换。
- [x] 8.14 实现staging到正式package原子切换。
- [x] 8.15 实现发布失败恢复上一package。
- [x] 8.16 清理成功后的rollback目录。
- [x] 8.17 清理失败后的staging目录。
- [x] 8.18 保持package位于Assets之外。
- [x] 8.19 删除v1单文件与sidecar写入路径。
- [x] 8.20 删除Patch inbox、clipboard和watcher路径。

## 9. Graph与Timeline exporter

- [x] 9.1 建立Character v2 package exporter。
- [x] 9.2 建立AI v2 package exporter。
- [x] 9.3 导出controller分片。
- [x] 9.4 导出blackboard分片。
- [x] 9.5 导出Character actions分片。
- [x] 9.6 导出AI perception分片。
- [x] 9.7 按stable Graph identity创建Graph目录。
- [x] 9.8 从capability catalog导出Node kind。
- [x] 9.9 只导出当前kind有效properties。
- [x] 9.10 将系统Node转换为anchor endpoint。
- [x] 9.11 导出Flow Edge完整目标集合。
- [x] 9.12 导出Property Edge完整目标集合。
- [x] 9.13 导出Graph owner与slot。
- [x] 9.14 导出Graph reference。
- [x] 9.15 将Node位置写入layout分片。
- [x] 9.16 按stable Timeline identity创建Timeline目录。
- [x] 9.17 将Track、Clip、Marker写入timeline分片。
- [x] 9.18 将Curve payload写入curves分片。
- [x] 9.19 从Node输出删除C# typeName。
- [x] 9.20 从Node输出删除nodeTypeDisplayName。
- [x] 9.21 从Node输出删除重复propertyPorts。
- [x] 9.22 从Node输出删除无意义空配置字段。
- [x] 9.23 生成全部context catalog。
- [x] 9.24 生成manifest规范文件清单。
- [x] 9.25 生成sync基线。

## 10. 确定性layout

- [x] 10.1 定义Graph kind到布局方向的正式规则。
- [x] 10.2 定义拓扑层级计算规则。
- [x] 10.3 定义同层identity排序规则。
- [x] 10.4 定义Node间距常量。
- [x] 10.5 保留现有Node显式位置。
- [x] 10.6 为新Node缺失位置生成确定性位置。
- [x] 10.7 禁止自动布局改写未受影响现有Node。
- [x] 10.8 将layout变化纳入editableHash。
- [x] 10.9 从Program/Projection发布判定中排除纯layout变化。

## 11. Live revision与同步状态

- [x] 11.1 保留domain-aware live source revision入口。
- [x] 11.2 从revision排除generated Character Program。
- [x] 11.3 从revision排除Presentation Projection。
- [x] 11.4 从revision排除generated AIIntentProgram。
- [x] 11.5 保留独立current context hash入口。
- [x] 11.6 从当前package计算editableHash。
- [x] 11.7 从当前package校验contextHash。
- [x] 11.8 定义Clean。
- [x] 11.9 定义TreeDirty。
- [x] 11.10 定义DocumentDirty。
- [x] 11.11 定义Conflict。
- [x] 11.12 将context文件变化判为`readonly_context_modified`。
- [x] 11.13 禁止保存可编辑dirty布尔值。
- [x] 11.14 保持revision与状态查询不build、不publish。

## 12. Checkout与rebase

- [x] 12.1 将checkout切换为v2 package exporter。
- [x] 12.2 无package时显式checkout创建。
- [x] 12.3 Clean时显式checkout规范刷新。
- [x] 12.4 TreeDirty且editable未改时显式checkout刷新。
- [x] 12.5 DocumentDirty时checkout保留现有package。
- [x] 12.6 Conflict时checkout拒绝覆盖。
- [x] 12.7 checkout返回package绝对路径。
- [x] 12.8 checkout返回整包状态与hash摘要。
- [x] 12.9 checkout不修改Unity资产。
- [x] 12.10 checkout不触发Program或Projection build。
- [x] 12.11 将rebase切换为整包基线。
- [x] 12.12 rebase刷新当前Unity context分片。
- [x] 12.13 rebase保留AI editable分片。
- [x] 12.14 rebase更新三项base identity。
- [x] 12.15 rebase后重新推导DocumentDirty。
- [x] 12.16 rebase不修改Unity资产或generated product。

## 13. Reconciler目标状态闭包

- [x] 13.1 将Reconciler输入切换为v2 package projection。
- [x] 13.2 建立跨文件stable identity索引。
- [x] 13.3 建立跨文件local identity symbol表。
- [x] 13.4 校验Graph owner存在且slot合法。
- [x] 13.5 计划新Graph创建。
- [x] 13.6 计划Graph删除。
- [x] 13.7 计划Node创建。
- [x] 13.8 计划Node typed property更新。
- [x] 13.9 计划Node删除。
- [x] 13.10 拒绝Node kind原地改变。
- [x] 13.11 计划Flow Edge创建。
- [x] 13.12 计划Flow Edge删除。
- [x] 13.13 计划Flow Edge endpoint变化。
- [x] 13.14 计划Property Edge创建。
- [x] 13.15 计划Property Edge删除。
- [x] 13.16 计划Property Edge endpoint变化。
- [x] 13.17 解析anchor endpoint。
- [x] 13.18 计划Graph reference更新。
- [x] 13.19 计划ConditionRule目标状态。
- [x] 13.20 计划StateMachine与State目标状态。
- [x] 13.21 计划Timeline与Track目标状态。
- [x] 13.22 计划Clip与Marker目标状态。
- [x] 13.23 计划完整Curve替换。
- [x] 13.24 计划Blackboard目标状态。
- [x] 13.25 计划Action与Input binding目标状态。
- [x] 13.25.1 将Character Input节点的`inputId`与Action Request节点的`requestId`定义为必填typed property。
- [x] 13.25.2 从正式Node binding导出并重建Input与Request节点目标状态。
- [x] 13.25.3 将Input与Request节点新增、改名、移动和binding变化对账为同一typed Mutation。
- [x] 13.25.4 在Mutation preflight中校验Definition identity与Input值类型，并通过正式binding API应用。
- [x] 13.26 计划AI Definition与Perception目标状态。
- [x] 13.27 计算创建与引用绑定顺序。
- [x] 13.28 计算Edge断开与entity删除顺序。
- [x] 13.29 收集全部serialized transaction owner。
- [x] 13.30 输出跨文件entity source map。
- [x] 13.31 输出最小planned diff。
- [x] 13.32 保持Reconciler不修改Unity对象。

## 14. Mutation handler闭包

- [x] 14.1 将Node create/configure接入统一capability catalog。
- [x] 14.2 将Graph kind校验接入统一capability catalog。
- [x] 14.3 为Flow Edge保留正式`BaseGraph.Link`。
- [x] 14.4 为Flow Edge删除保留正式`BaseGraph.UnLink`。
- [x] 14.5 为Property Edge保留正式`BaseGraph.LinkProperty`。
- [x] 14.6 增加Property Edge正式断开Mutation。
- [x] 14.7 增加Property Edge preflight解析。
- [x] 14.8 增加Property Edge endpoint重接lowering。
- [x] 14.9 增加新Graph正式owner创建Mutation。
- [x] 14.10 增加Graph reference正式配置Mutation。
- [x] 14.11 保持StateMachine handler正式API。
- [x] 14.12 保持StateBehavior handler正式API。
- [x] 14.13 保持Timeline handler正式API。
- [x] 14.14 保持AI handler正式API。
- [x] 14.15 保持Compiler不拥有Undo、dirty、rollback与SaveAssets。
- [x] 14.16 删除operation字符串catalog解析。
- [x] 14.17 删除前序operation output合同。

## 15. Dry-run与apply

- [x] 15.1 将dry-run切换为整包加载。
- [x] 15.2 dry-run严格校验manifest与文件清单。
- [x] 15.3 dry-run计算live source revision与current context hash。
- [x] 15.4 dry-run拒绝TreeDirty。
- [x] 15.5 dry-run拒绝Conflict。
- [x] 15.6 dry-run执行v2 Reconciler。
- [x] 15.7 dry-run执行Mutation preflight。
- [x] 15.8 dry-run返回documentHash。
- [x] 15.9 dry-run返回plan hash。
- [x] 15.10 dry-run返回跨文件planned diff与诊断。
- [x] 15.11 dry-run不dirty、save、build或publish。
- [x] 15.12 将apply切换为整包expected document hash。
- [x] 15.13 apply重新加载并计算documentHash。
- [x] 15.14 apply拒绝hash变化。
- [x] 15.15 apply拒绝source revision或context变化。
- [x] 15.16 apply重新建立等价Mutation Plan。
- [x] 15.17 apply校验plan hash。
- [x] 15.18 apply建立唯一Undo事务。
- [x] 15.19 apply执行typed handlers。
- [x] 15.20 apply执行domain Validator。
- [x] 15.21 apply失败时回滚全部owner。
- [x] 15.22 apply成功时dirty实际owner。
- [x] 15.23 apply成功时保存正式Unity资产。
- [x] 15.24 Character apply只保存正式authoring，Program与Projection保持stale直到独立显式Build。
- [x] 15.25 AI apply显式发布正式AIIntentProgram。
- [x] 15.26 AI generated product发布失败时回滚。
- [x] 15.27 从最终Unity树导出完整v2 package。
- [x] 15.28 将local identity替换为stable identity。
- [x] 15.29 原子发布最终package。
- [x] 15.30 package发布失败时不报告Clean。
- [x] 15.31 apply成功时返回新hash与Clean。

## 16. 五个生命周期MCP工具

- [x] 16.1 删除`ManageBtsmtlAgentAuthoringMcpTool`。
- [x] 16.2 删除MCP `action`参数。
- [x] 16.3 注册`btsmtl.checkout_document`。
- [x] 16.4 为checkout定义严格input schema。
- [x] 16.5 为checkout定义结构化output schema。
- [x] 16.6 为checkout声明行为annotations。
- [x] 16.7 注册`btsmtl.rebase_document`。
- [x] 16.8 为rebase定义严格input schema。
- [x] 16.9 为rebase定义结构化output schema。
- [x] 16.10 为rebase声明行为annotations。
- [x] 16.11 注册`btsmtl.dry_run_document`。
- [x] 16.12 为dry-run定义严格input schema。
- [x] 16.13 为dry-run定义结构化output schema。
- [x] 16.14 为dry-run声明行为annotations。
- [x] 16.15 注册`btsmtl.apply_document`。
- [x] 16.16 为apply定义严格input schema。
- [x] 16.17 为apply定义结构化output schema。
- [x] 16.18 为apply声明行为annotations。
- [x] 16.19 注册`btsmtl.validate`。
- [x] 16.20 为validate定义严格input schema。
- [x] 16.21 为validate定义结构化output schema。
- [x] 16.22 为validate声明行为annotations。
- [x] 16.23 将五个薄handler接入同一application service。
- [x] 16.24 让业务/schema失败返回tool execution error。
- [x] 16.25 在错误中返回code、path、message与suggestion。
- [x] 16.26 在成功结果中返回绝对package path与hash摘要。
- [x] 16.27 禁止MCP结果嵌入完整package JSON。
- [x] 16.28 拒绝旧tool名与旧action alias。
- [x] 16.29 拒绝`patch_json`与任意document path参数。
- [x] 16.30 不注册任何Node、Edge、Timeline或JSON patch工具。
- [x] 16.31 在生命周期工具说明中声明Presentation provider/source与Action channel只读边界。

## 17. Editor Window

- [x] 17.1 将Window路径显示改为package目录。
- [x] 17.2 显示manifest schema与root identity。
- [x] 17.3 显示Clean、TreeDirty、DocumentDirty与Conflict。
- [x] 17.4 显示editable、context与document hash。
- [x] 17.5 保留显式checkout按钮。
- [x] 17.6 保留显式rebase按钮。
- [x] 17.7 保留显式dry-run按钮。
- [x] 17.8 保留显式apply按钮。
- [x] 17.9 保留显式validate按钮。
- [x] 17.10 显示跨文件entity诊断。
- [x] 17.11 显示planned与applied diff摘要。
- [x] 17.12 删除Window内JSON正文编辑器。
- [x] 17.13 删除Patch与Intent输入模式。
- [x] 17.14 禁止selection、focus或domain reload自动执行生命周期。
- [x] 17.15 禁止Window复制application service。

## 18. 旧路径清理与文档同步

- [x] 18.1 删除v1`AgentAuthoringDocument`单文件根模型。
- [x] 18.2 删除v1单文件Store。
- [x] 18.3 删除v1 sidecar baseline模型。
- [x] 18.4 删除v1 external reader与writer。
- [x] 18.5 删除`AgentControllerIntent`外部入口。
- [x] 18.6 删除`AgentMacroLibrary`。
- [x] 18.7 删除`AgentMacroCoverageEvaluator`。
- [x] 18.8 删除外部Patch IR与宽operation模型。
- [x] 18.9 删除bootstrap AI root入口。
- [x] 18.10 删除clipboard、快捷键、inbox与watcher残留。
- [x] 18.11 删除Node级或局部Graph工具说明。
- [x] 18.12 更新`add-corin-training-ai-demo`为v2 package工作流。
- [x] 18.13 删除其v17 Snapshot/Patch任务。
- [x] 18.14 删除其bootstrap依赖。
- [x] 18.15 更新`openspec/project.md`的Agent authoring口径。
- [x] 18.16 更新`btsmtl-agent-authoring`技能主流程。
- [x] 18.17 更新技能current-contract schema与代码地图。
- [x] 18.18 更新Editor工具说明与错误文案。
- [x] 18.19 删除现行文档中的Document v1和单tool action口径。

## 19. 旧Presentation结构迁移生命周期

- [x] 19.1 为checkout返回旧inline Pose Graph类型化诊断。
- [x] 19.2 实现唯一GraphCatalog迁移服务与Inspector入口。
- [x] 19.3 注册精确Definition的`character.migrate_legacy_pose_state_graphs`。
- [x] 19.4 返回迁移数量、保存状态、资产revision和typed failure。
- [x] 19.5 拒绝selection、扫描、未知参数与自动Build。
- [x] 19.6 同步技能、current contract与Corin最终迁移步骤。
