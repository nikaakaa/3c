本清单负责把公共定义移出 TreeDesigner、保留具体运行实现、迁出有效编辑能力并删除被替代的旧 UI。只在原目录拆文件不算完成。每个切片同时处理生产者、直接消费者、程序集、实际受影响的资产引用和旧位置；不新增测试或手动验证任务，不建立兼容路径或新运行器。当前全部为待实施任务。

## 0. 类型、依赖与迁移范围

- [ ] 0.1 按 design.md 的 A1-C6 建立类型/成员清单，记录职责、原位置、目标位置、程序集、直接消费者、注册和资产/生成引用；按当前 diff 列出实际共享文件冲突
- [ ] 0.2 明确公共定义、具体运行实现和编辑功能三类归属，记录公共定义的完整依赖闭包，排除具体图类型、领域运行器和 Editor 反向依赖
- [ ] 0.3 为迁移类型建立 namespace/assembly 精确映射，保留脚本 GUID、序列化字段、VariableId、端口身份、默认值、owner、generation、provenance 和 fact projection；没有完整去向的合法内容不得进入删除批次
- [x] 0.4 列明必须保持的业务边界：BaseGraph 运行参数、PropertyPort 求值、技能编译、原生 Pose 执行与 Timeline 调用协议；需要改变这些合同的对象单独报告

## 1. 黑板公共定义与具体实现分离（A）

- [x] 1.1 在 `Runtime/BTSMTL/Authoring` 建立 `BTSMTL.Authoring` 公共程序集与职责命名空间，将 scope/lifetime、输入绑定、事实投射数据等独立定义移入 `Blackboard`，具体消费者单向引用公共层
- [x] 1.2 将 `PipelineBlackboardVariableReference` 的字段与值规则移入公共层；从 `BaseExposedProperty` 读取字段的创建职责留在具体声明侧，统一切换调用并删除旧耦合构造入口，不新增第二份 reference
- [x] 1.3 按成员拆分 policy：只依赖公共值的规则进入公共层，具体声明校验与对象读取留在 TreeDesigner；规则结果和字段含义保持不变
- [x] 1.4 将 `IPipelineBlackboardRuntimeAccess` 归入 `TreeDesigner/Scripts/Blackboard` 的具体运行接入文件，保留现有签名语义；`BaseExposedProperty` 及泛型实现继续唯一拥有原声明存储
- [x] 1.5 按依赖拆分 `ExposedProperty_Extension.cs` 和 `ExposedPropertyUtility.cs`，具体声明查找和类型映射留在具体实现，独立规则随公共定义归位，不复制同名方法
- [x] 1.6 更新 `BaseGraphAuthoring`、`NestedGraphValidation`、黑板 Editor adapter、`CharacterPipelineAuthoringContext`、技能编译器及生成 authoring 的直接引用，并在本切片完成实际受影响的序列化身份迁移

## 2. 作者字段、能力与端口分离（B）

- [x] 2.1 将 `GraphAuthoringCapabilityCatalog.cs`、`GraphAuthoringDomainContracts.cs` 中的独立定义及其依赖闭包迁入 `Runtime/BTSMTL/Authoring/Graph`，按字段、端口、能力和文档合同拆文件；具体提供者留所属领域
- [x] 2.2 同步自有类型的正式 namespace、assembly、直接引用和实际受影响的序列化映射；保留字段顺序、显示名、颜色、端口身份、排序、动态端口策略及唯一语义来源
- [x] 2.3 保持 `PropertyPort`、`PropertyEdge`、`BaseAttributes`、`BaseNode` 和 `BaseGraphAuthoring` 的具体连接与求值职责，不把具体运行端口搬入公共描述层或复制到 Editor
- [x] 2.4 将 `PropertyPortAuthoringService.cs` 归位到 `TreeDesigner/Editor/Scripts/Authoring`，作为具体图的 Editor 读取服务消费公共描述；不让共享 Editor 为此反向依赖 TreeDesigner
- [x] 2.5 更新 `BtsmtlSharedGraphAuthoringAdapters`、Pose capability projector、技能 Graph authoring、C#作者入口和能力目录消费者，保持原领域业务规则与正式写入入口

## 3. 有效编辑合同与面板迁出（C）

- [ ] 3.1 在 `Editor/GraphAuthoring` 建立独立 `BTSMTL.Authoring.Editor` 程序集，避免归入父目录 Character Editor；公共集成只引用公共定义及必要原生框架，具体领域 Editor 单向消费它
- [ ] 3.2 从 `GraphAuthoringProjectionCanvas.cs` 提取 `IGraphAuthoringClipboardCodec`、`GraphAuthoringProjectionCanvasBinding` 及有效描述到 `Contracts`；旧 `GraphAuthoringProjected*View` 与 Canvas partial 进入删除清单，不搬迁保留
- [ ] 3.3 从 `GraphAuthoringStateMachineProjection.cs`、`GraphAuthoringStateMachineContracts.cs` 提取有效绑定和页面合同；旧状态机视图与 GraphView partial 进入删除清单，原生状态机编辑行为保持
- [ ] 3.4 将正式入口仍使用的 Details、状态机 Details、Navigator、Bottom Dock 和 Data Catalog 的独立面板归入共享 Editor；具体业务数据源保留所属领域，清理无消费者的附属显示
- [ ] 3.5 按成员拆分 `GraphAuthoringEditorShell`、共享工作区注册、Clipboard controller 与具体 adapters，迁出有效 Selection、Undo、资源选择、Clipboard、生命周期和租约释放能力；不把旧窗口装配整文件保留
- [ ] 3.6 同步 `CharacterPoseGraphWorkspace`、`CharacterPoseCanvasBinding`、`CharacterPoseCanvasCommands`、`CharacterPoseCanvasEditorWriteSession`、`CharacterPoseDocumentCanvas`、`CharacterPoseGraphAuthoringAdapter` 及其它原生入口；共享消费者统一修改，继续写入真实 owner

## 4. 旧窗口、画布与入口清理（C5/C6）

- [ ] 4.1 完整记录 `BaseTreeWindow`、`SubTreeWindow`、`TreeBrowserWindow`、`NodeReferenceWindow`、`TreeWindowUtility`、`BaseTreeInspector` 和 `RuntimeDebugSourceNavigator` 的调用者、静态注册、资产打开、菜单及资源引用
- [ ] 4.2 将有效 Inspector 打开、调试定位、导航和下钻接入对应正式入口；删除无业务消费者的旧入口，合法内容无等价去向时报告具体缺口，不删入口掩盖问题
- [ ] 4.3 在有效合同及入口迁出后，删除被替代的旧窗口、`GraphAuthoringCanvasView`、旧投影视图、独有节点/端口/边视图和回调，处理其所有 partial 文件，不保留改名或换目录后的旧画布
- [ ] 4.4 清理旧 UI 对应的 `.meta`、UXML/USS、资源字符串、菜单、静态注册和程序集引用；共享面板仍使用的资源先迁出并准确命名，不整目录删除
- [ ] 4.5 保留仍有有效职责的 BaseGraph、BaseNode、PropertyPort、Graph/Tree/Node 运行实现及第三方 NodeCanvas、FlowCanvas、Slate，不增加 MovedFrom、旧类型别名、兼容窗口或运行时迁移器

## 5. 引用、交付记录与规格收口

- [ ] 5.1 清理各切片留下的旧类型引用、重复文件和确实已空的程序集；公共 Runtime 不依赖 TreeDesigner/Editor，公共 Editor 不依赖具体领域 Editor，具体适配不倒灌公共层
- [ ] 5.2 保持 Timeline 的既有 BaseGraph 参数与运行协议，只同步本次直接迁出类型的引用；交付声明、编辑合同、具体实现的保留/归位/删除清单和资产身份处理记录
- [ ] 5.3 记录实际触及的 0 GC 边界及未决缺口，不把文件整理写成全项目性能达标；不为绕过分配添加包装对象或第二执行路径
- [ ] 5.4 按实际完成范围同步本 change 的 delta 与直接受影响主 spec，包含遗漏的工作区区域和旧 GraphView 条款；保留设计中列明的历史运行合同冲突，不代替其它 change 完成或归档

## 6. 小步提交

- [x] 6.1 A 黑板公共定义、具体接入及其直接消费者作为完整切片提交，中文说明范围与身份处理
- [x] 6.2 B 作者字段/端口定义及具体 Editor 服务归位单独提交
- [ ] 6.3 C 公共编辑合同、面板和原生入口接线单独提交
- [ ] 6.4 C5/C6 旧 UI 删除及规格收口单独提交；实际共享文件冲突或业务去向缺失停在受影响切片，不用临时桥接继续
