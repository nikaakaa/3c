本清单只负责 TreeDesigner 自有作者定义、编辑辅助和旧 UI 的归位与清理。每个条目必须同时处理生产者、直接消费者和旧入口；不新增运行时求值器、兼容别名、第二编辑入口或性能重构。手动验收不写入本清单。

## 0. 实施前冻结

- [ ] 0.1 建立 A1-C5 文件清单：记录类型、当前文件、目标文件、程序集、直接消费者、资产/生成引用和唯一修改者；把当前主目录未提交改动列为切片前置条件
- [ ] 0.2 为每个待迁移类型标记序列化身份、VariableId、端口身份、默认值、owner、generation、provenance 和 fact projection 字段；没有完整去向的类型不得进入删除批次
- [ ] 0.3 把需要改变业务合同的对象单独列为冲突：`BaseGraph` 参数、`PropertyPort` Runtime 语义、Pose 共享集成和 Timeline 调用协议保持原 owner，不在本 change 内改写

## 1. 黑板声明归位（A）

- [ ] 1.1 从 `Runtime/BTSMTL/TreeDesigner/Scripts/ExposedProperty/ExposedProperty.cs` 拆出 `PipelineBlackboardVariableScope`、`PipelineBlackboardVariableLifetime`、`PipelineBlackboardFactProjectionKind`、`PipelineBlackboardInputBinding`、`PipelineBlackboardFactProjection`、`PipelineBlackboardVariableReference` 和对应 policy 到 `Scripts/Blackboard/PipelineBlackboardContracts.cs`
- [ ] 1.2 将 `IPipelineBlackboardRuntimeAccess` 的声明与黑板合同放入 A1 目标文件，保留 `BaseExposedProperty`、泛型属性类及其序列化字段在 `ExposedProperty.cs`
- [ ] 1.3 按实际调用迁移 `ExposedProperty_Extension.cs` 与 `ExposedPropertyUtility.cs`：黑板查找和事实投射进入 `Scripts/Blackboard`，属性类型映射继续留在 `Scripts/ExposedProperty`；不复制同名方法
- [ ] 1.4 更新 `BaseGraphAuthoring`、`NestedGraphValidation`、`BtsmtlSkillBlackboardEditorAdapter`、`CharacterPipelineAuthoringContext`、技能黑板编译器和生成 authoring 的直接引用；保留原序列化身份和 owner 语义

## 2. 作者能力与端口归位（B）

- [ ] 2.1 将 `GraphAuthoringCapabilityCatalog.cs` 中字段值/约束/可见性/端口描述类型拆到 `Scripts/Authoring/FieldContracts.cs` 和 `PortContracts.cs`，能力、命令和子面板目录留在 `CapabilityCatalog.cs`
- [ ] 2.2 保持 B1 类型的 namespace、字段顺序、显示名、颜色、排序和动态端口策略；只改变文件归属，不改变节点注册、连接规则和投影结果
- [ ] 2.3 盘点 `PropertyPort.cs`、`PropertyEdge.cs`、`BaseAttributes.cs`、`BaseNode`、`BaseGraphAuthoring` 的 Runtime 连接状态、端口身份和值来源；形成逐项去向表，保留没有完整替代者的实现
- [ ] 2.4 将 `PropertyPortAuthoringService.cs` 移到 `Editor/GraphAuthoring`，让它只读取 B2 的 Runtime 类型和属性；共享适配器、Pose 编辑器和旧窗口直接改为使用该 Editor 服务
- [ ] 2.5 更新 `BtsmtlSharedGraphAuthoringAdapters`、Pose capability projector、技能 Graph authoring 和所有 `GraphAuthoringCapabilityCatalog` 消费者；不在 Editor 复制 `PropertyPort`、`PropertyEdge` 或端口缓存

## 3. 投影与编辑辅助归位（C）

- [ ] 3.1 从 `GraphAuthoringProjectionCanvas.cs` 抽出 `IGraphAuthoringClipboardCodec`、`GraphAuthoringProjectionCanvasBinding`、搜索项和投影数据合同到 `Editor/GraphAuthoring/GraphAuthoringProjectionContracts.cs`
- [ ] 3.2 将投影节点、端口、边和 `GraphAuthoringCanvasView` 迁到 `Editor/GraphAuthoring/Projection`；保留现有 NodeCanvas port 类型、布局持久化和连接策略
- [ ] 3.3 将 `GraphAuthoringDetailsPresenter`、`GraphAuthoringStateMachineProjection`、`GraphDataCatalog` 及其直接数据源迁到 `Editor/GraphAuthoring/Details`、`Projection`、`Catalog`；不把业务字段重新定义一份
- [ ] 3.4 将 `BtsmtlGraphAuthoringAdapters`、`BtsmtlSharedAuthoringWorkspaceRegistry`、`GraphAuthoringClipboardController` 的有效适配和租约释放逻辑迁到 `Editor/GraphAuthoring/Adapters`
- [ ] 3.5 更新 `CharacterPoseGraphWorkspace`、`CharacterPoseCanvasBinding`、`CharacterPoseCanvasCommands`、`CharacterPoseCanvasEditorWriteSession`、`CharacterPoseDocumentCanvas` 和 `CharacterPoseGraphAuthoringAdapter` 的直接引用；C4 共享文件由一个集成人统一修改
- [ ] 3.6 更新 `GraphAuthoringEditorShell` 的 Details、Navigator、Selection、Undo、资源选择、C# authoring 和只读观察接线；原生 Pose/FlowCanvas/Slate 入口继续使用同一份迁出实现

## 4. 旧 UI 清理（C5）

- [ ] 4.1 按 0.1 的消费者清单逐一确认 `BaseTreeWindow.cs`、`SubTreeWindow.cs`、`TreeBrowserWindow.cs`、`NodeReferenceWindow.cs`、`TreeWindowUtility.cs` 及其回调是否仍被当前入口使用
- [ ] 4.2 将仍被使用的窗口能力先迁入 C3/C4；删除确认无消费者且无合法资产/菜单引用的旧窗口、旧 GraphView、独有节点视图、菜单和回调
- [ ] 4.3 保留 `BaseGraph`、`BaseNode`、`PropertyPort`、运行时 Graph/Tree/Node 类型以及第三方 NodeCanvas、FlowCanvas、Slate；不得按目录名整批删除
- [ ] 4.4 删除旧 UI 后清理对应 `.meta`、asmdef 引用、菜单入口和静态注册；不添加 `MovedFrom`、兼容类、运行时迁移器或旧新开关

## 5. 引用、程序集和规格收口

- [ ] 5.1 只对实际归位类型调整 namespace、asmdef 和直接 using；保留脚本 GUID，精确处理受影响的序列化/生成引用，不重建无影响资产
- [ ] 5.2 清理本 change 产生的旧类型引用、重复文件和空程序集依赖；Timeline 仅更新直接受影响的类型引用，保留现有 BaseGraph 参数和运行协议
- [ ] 5.3 交付文件保留/归位/删除清单、消费者迁移清单、资产身份处理记录和未决冲突；说明触及的 0 GC 边界，不把目录整理写成全项目 0 GC 完成
- [ ] 5.4 只更新本 change 及直接受影响的主 spec；不替 Pose、Timeline、0 GC 或其它未归档 change 勾选完成

## 6. 小步提交

- [ ] 6.1 A 黑板合同单独提交，提交信息使用中文并包含迁移范围
- [ ] 6.2 B 作者能力/端口单独提交，运行端口与 Editor 描述归属清楚
- [ ] 6.3 C 投影/编辑辅助单独提交，Pose 共享消费者与旧 UI 清理分界明确
- [ ] 6.4 C5 删除和规格收口单独提交；若发现共享未提交改动或语义缺口，停在对应切片并记录具体文件，不用临时桥接继续
