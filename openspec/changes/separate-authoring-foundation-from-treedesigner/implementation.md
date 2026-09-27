# 公共作者定义拆分实施记录

## 2026-09-28：A 黑板定义

项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`。按用户要求不运行 replay、不新增测试、不建立 worktree。只使用该项目的 Unity 实例 `e852139597e42532`；代码修改前退出 Play，每个完整编辑批次后恢复自动刷新并请求正式编译。

### 类型与消费者

以下路径相对 `Assets/GameScripts/Main`。

| 对象 | 原归属 | 当前归属与依赖 |
| --- | --- | --- |
| PipelineBlackboardVariableScope、PipelineBlackboardVariableLifetime、PipelineBlackboardFactProjectionKind | TreeDesigner / ExposedProperty.cs | `BTSMTL.Authoring.Blackboard` / `Runtime/BTSMTL/Authoring/Blackboard`，枚举序号保持 |
| PipelineBlackboardInputBinding、PipelineBlackboardFactProjection | 同上 | 同一公共目录，原序列化字段、构造规则与值语义保持 |
| PipelineBlackboardVariableReference | 同上，构造函数依赖具体声明 | 公共目录保存原四个字段与类型匹配规则；`BaseExposedProperty.CreateBlackboardReference` 提取原字段后调用公共构造函数 |
| PipelineBlackboardVariablePolicy | 同上，公共规则与声明校验混合 | scope/lifetime 纯规则进入公共目录；声明校验迁入具体实现的 PipelineBlackboardDeclarationPolicy |
| IPipelineBlackboardRuntimeAccess | 同上 | `TreeDesigner/Scripts/Blackboard`，继续属于 BTSMTL.TreeDesigner，原签名语义保持 |
| PipelineBlackboardFactProjectionPolicy | 同上 | 原校验主体归入 PipelineBlackboardDeclarationPolicy.TryValidateFactProjection，旧类删除 |
| BaseExposedProperty 及泛型/具体声明 | ExposedProperty.cs | 原位置、类型身份、声明字段与对象生命周期保持 |
| ExposedProperty_Extension.cs | 具体声明的 Editor partial | 留在具体声明侧；文件只有具体声明字段和回调，没有可以迁出的独立黑板规则 |
| ExposedPropertyUtility.cs | TreeDesigner/Scripts/Utility | 移至 TreeDesigner/Scripts/ExposedProperty，保留原 .meta GUID、类型缓存和显示映射，不复制实现 |

23 个原有 C# 使用文件同步公共 namespace；直接程序集为 BTSMTL.TreeDesigner、BTSMTL.TreeDesigner.Editor、ThirdPersonClient.Runtime、ThirdPersonClient.Editor。新增 BTSMTL.Authoring 无项目程序集引用，不引用 TreeDesigner 或 Editor。Simulation/Core 未增加 Unity 作者层引用。

消费者涵盖旧声明节点、原生技能黑板声明和编辑 adapter、技能语义编译器、CharacterPipelineAuthoringContext、C# 作者 API、源码输出 adapter 及已有生成作者文件。源码输出 adapter 同步导入公共 namespace，避免只有已生成源码能编译。当前 A 切片涉及的既有文件没有其它未提交修改；没有覆盖 Pose、Timeline、Simulation 的并行改动。

### 序列化身份与资产范围

这 7 个公共类型从 `TreeDesigner.<类型>, BTSMTL.TreeDesigner` 迁至 `BTSMTL.Authoring.Blackboard.<类型>, BTSMTL.Authoring`。原有 ExposedProperty 脚本 GUID 保留；拆出的纯类型使用新脚本 GUID，具体声明仍由原类型持有。

迁移前对 Assets 中 `.asset`、`.prefab`、`.unity`、`.json` 搜索上述类型名及限定名，没有发现需要重写的显式类型身份引用。现有声明以原字段形状内联保存这些数据，本切片不改资产内容、变量 ID、声明 owner、generation、provenance 或默认值。此结果仅覆盖 A 类型，不提前宣称 B/C 的序列化范围已完成。

### 业务与分配边界

BaseGraph 参数、PropertyPort 求值、技能编译规则、原生 Pose 执行、Timeline 调用协议保持原实现。本切片移动枚举、数据及既有纯规则；变量引用仍是相同字段的 struct，由既有声明创建入口传入原字符串和类型身份，不增加包装对象、集合、闭包或运行转换器。具体声明校验主体保持，未处理其它 ValueRuntime 的缓存、短路或分配行为，不宣称全项目 0 GC 达标。

### 后续切片

B 的公共依赖闭包为 GraphAuthoringCapabilityCatalog.cs 与 GraphAuthoringDomainContracts.cs 中的现有定义，具体图端口和编辑读取服务仍属于 TreeDesigner。C 要拆开绑定合同与旧 GraphView partial，沿 Inspector、调试导航、静态注册和资源引用确认有效入口去向；这些范围尚未完成，不能据本记录删除旧 UI。

### A 编译结果

目标 Unity 完成本次脚本编译及 Domain Reload，回到 Edit/idle，Console error 查询为 0 条。公共程序集与四个直接程序集引用已由 Unity 编译接收。本切片没有运行 Play、replay 或测试。

## 2026-09-28：B 作者字段与端口描述

`GraphAuthoringCapabilityCatalog.cs` 的 26 个现有类型按字段（12）、端口及端口组合（7）、能力/命令/子面板目录（7）拆入 `Runtime/BTSMTL/Authoring/Graph`，分别由 FieldContracts.cs、PortContracts.cs、CapabilityCatalog.cs 拥有。GraphAuthoringDomainContracts.cs 的领域身份、文档、节点/边描述和 mutation 合同完整归入 DomainContracts.cs。全部类型从 `TreeDesigner.Authoring` 迁至 `BTSMTL.Authoring.Graph`，所属程序集从 BTSMTL.TreeDesigner 改为 BTSMTL.Authoring，字段、枚举值、显示描述和方法主体保持。

原 Catalog 与 DomainContracts 脚本 .meta GUID 随主要文件保留；新拆字段、端口文件使用新 GUID。扫描 Assets 未发现旧 namespace 的显式资产类型引用，因此没有重写资产。65 个原有使用文件切换 namespace，Timeline.Tree.Editor 补直接公共程序集引用。PropertyPortAuthoringService 连同原 .meta 进入 TreeDesigner/Editor/Scripts/Authoring，继续服务具体图的 BaseNode/PropertyPort，不让公共 Editor 依赖它。

PropertyPort、PropertyEdge、BaseAttributes、BaseNode、BaseGraphAuthoring 的运行实现未修改。公共程序集没有 TreeDesigner、BaseGraph、BaseNode、PropertyPort 或 UnityEditor 引用。原有字段、端口组合、Capability 注册方法直接迁移，没有新增运行包装或另一份端口规则。

### B 编译与共享改动

`dotnet build BTSMTL.Authoring.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false` 成功，0 警告、0 错误；结束后已执行 `dotnet build-server shutdown`。首次使用 --no-restore 时缺少该新项目的 NuGet assets，按正式项目执行正常 restore/build 后成功。

目标 Unity 全量编译在另一工作的 CharacterAnimationInputContract.cs 两处新增 Vector3/Quaternion 参数声明中报 CS7036，缺少 defaultValue 参数；本次未修改该文件或参数业务。此时不能宣称全项目编译通过，后续切片仍需核对目标编译。共享文件只修改 namespace 引用；提交按 HEAD 应用相同 namespace 替换，其余原有未提交业务改动保留在工作区，不纳入本次提交。

## 2026-09-28：C 公共编辑合同与原生消费者

建立 `Editor/GraphAuthoring/BTSMTL.Authoring.Editor.asmdef`，唯一项目引用为 BTSMTL.Authoring。Details、状态机 Details、Navigator、Bottom Dock、状态机合同迁入对应目录，Projection/StateMachine binding、Clipboard 合同、Undo/Selection 绑定和面板宿主从混合文件中提取。38 个迁出类型改用 `BTSMTL.Authoring.Editor`，没有 TreeDesigner、BaseTree/BaseGraph/BaseNode 或 GraphView 依赖。24 个原有引用文件同步，其中公共源文件随 .meta 迁移、提取的新文件单独分配 GUID。

共享 Details/Navigator 的 UXML 与 USS 随原 GUID 迁至新 Editor 资源目录，资源名改为 GraphAuthoringDetails/GraphAuthoringNavigator，更新正式加载路径和样式引用。Pose workspace、Canvas binding、命令、Clipboard、状态机与 Details adapter 消费同一迁出合同，没有复制节点、资产或 mutation。

目标 Unity 完成此次编译和 Domain Reload，Edit/idle，Console error 为 0 条。B 阶段外部 Pose 参数错误在此时已不再阻挡编译，本次未修改它的业务代码。没有运行 Play/replay 或新增测试。

旧 GraphDataCatalog 的使用链仅进入旧窗口及其源注册；旧 UI 清理时连同无消费者附属代码收口。Blend Space 仍使用窗口装配与自己的 GraphView，这部分移交动画编辑领域，避免误删有效样本编辑；目前尚未执行这部分旧窗口清理。

## 2026-09-28：C5/C6 旧 UI 清理与最终交付

### 最终业务链路

公共 Runtime 为 `BTSMTL.Authoring`：输入仍是原有黑板值、字段、端口和能力定义，输出是供具体图、技能作者、编译器和 Editor 读取的同一份数据。它没有项目程序集依赖，不引用 TreeDesigner 或 UnityEditor。具体声明仍由 BaseExposedProperty 拥有；BaseGraph 参数、PropertyPort 连接与求值、技能编译、原生 Pose 执行和 Timeline 协议没有改写。

公共 Editor 为 `BTSMTL.Authoring.Editor`，唯一项目引用是 BTSMTL.Authoring。原生 Pose 的绑定和领域 adapter 读取公共合同，Details、状态机 Details、Navigator、Selection、Undo 和只读面板合同继续服务现有工作区；写入仍进入原有领域 mutation 和真实 owner。没有新增运行器、包装数据、兼容窗口或备用配置。

Blend Space 仍编辑原来的样本坐标与资产。它实际使用的窗口布局、资源选择、搜索、剪贴板和生命周期装配归入 `Editor/CharacterPipeline/Authoring/Animation/BlendSpace`，正式类型为 `BlendSpaceWorkspaceWindow`、`BlendSpaceClipboardBinding`。其专有 GraphView 接口和资源随领域归位，不进入公共 Editor；原有脚本和资源 GUID 保留。未使用的工具栏扩展注册表删除。

### 删除对象与入口证据

完整公共类型映射、当前消费者、领域迁名、保留运行类型及旧文件/类型删除清单保存在同目录 `migration-map.json`，覆盖 93 个公共类型。清单以当前代码为准；前述 A/B/C 为阶段记录，不代表已删除代码仍存在。

| 原对象 | 原消费者/注册 | 最终处理 |
| --- | --- | --- |
| BaseTreeWindow、SubTreeWindow、TreeBrowserWindow、NodeReferenceWindow、TreeWindowUtility | 旧窗口互相调用，旧资产 Inspector、打开回调与菜单 | 目标项目 BaseTreeAsset 资产查询为 0；全部旧入口及其专有资源删除，原生技能/Pose/Timeline 打开链保持 |
| RuntimeDebugSourceNavigator 的两个 OpenGraph 重载 | 方法以 BaseTree/BaseGraph 打开旧窗口，无现行调用者 | 删除两方法；原生技能定位和 Timeline 定位保留 |
| GraphAuthoringCanvasView 及 Projection/StateMachine partial、节点/端口/边视图 | 旧 TreeDesigner 窗口 | 合同迁出后删除全部旧画布实现，不换目录保存 |
| PropertyPortAuthoringService、BtsmtlSharedGraphAuthoringAdapters、旧 capability/role/context/source | 仅旧窗口与旧数据目录注册 | B 阶段服务先归位，最终随唯一消费者删除；混合状态机 adapter 文件只删除 8 个旧 Btsmtl 类型，保留 Pose 类型 |
| GraphDataCatalog 及源、旧 Bottom Dock 的 descriptor/catalog/presenter、GraphAuthoringPageStack | 已删除旧工作区，没有有效原生消费者 | 删除；保留现行工作区依赖的 IGraphAuthoringReadOnlyPanel、DetailsHost 和 NavigatorHost |
| NodeViewAttribute、TreeWindowAttribute 与其注册 | 为旧节点视图和旧窗口选择 UI 类型 | 删除定义及注册标记；运行节点、图和端口实现保持 |
| BTSMTL.TreeDesigner.Editor | 剩余内容均为上述旧 UI | 迁出有效能力后删除程序集、目录及全部直接引用 |

删除批次的 99 个 .meta GUID 在现有资产、Prefab、场景、UXML、USS、JSON 和 asmdef 中没有残留引用。公共定义没有检出显式旧 namespace/assembly 序列化身份引用；原有内联字段、VariableId、端口身份、默认值、owner、generation、provenance 和 fact projection 保持。没有批量重建资产或加入 MovedFrom。

### 编译、资源与规格结果

目标 Unity 实例 `e852139597e42532` 完成本次正式编译及 Domain Reload，返回 Edit/idle，Console error 为 0。运行时反射确认公共 Runtime/Editor 类型分别属于 BTSMTL.Authoring、BTSMTL.Authoring.Editor，旧 BTSMTL.TreeDesigner.Editor 未加载；BlendSpaceWorkspaceWindow 位于动画 Editor。GraphAuthoringDetails、GraphAuthoringNavigator、BlendSpace/Workspace 三个 UXML 均成功加载和克隆，BlendSpace/Canvas 样式成功加载。

本次没有运行 Play、replay 或新增测试；上述证据覆盖编译、程序集与资源接线，不替代用户端到端操作。运行层仅拆定义与既有规则，变量引用仍是原字段 struct；未增加运行热路径分配，不把此次整理声称为全项目 0 GC 证明。

八份直接受影响主规格已同步，移除强制依赖旧资产窗口、旧 GraphView 和旧页面栈的条款，保留实际原生入口与项目面板合同。更广的 BaseGraph 唯一结构措辞，以及 graph-authoring-domain-framework 中 Pose IR/Compiler 与 native-flowcanvas-pose-runtime 的历史冲突，继续明确记录在 design.md；本次没有切换 Pose 执行方式，也不替其它 change 归档。

### 共享文件与提交

A、B、C 已分别提交为 `7ff71c785`、`921e73ead`、`36d33dcb6`。C5/C6 与规格收口作为第四个切片提交。共享的 CharacterPoseNodeDefinitionModule、CharacterPoseGraphProjectionValidator、CharacterPoseGraphAuthoringAdapter、CharacterPoseAuthoringPortProjection 只移除旧 Editor namespace 引用；已有业务修改保持，提交只包含本次差异。其它窗口产生的编译错误和改动没有纳入本次修复。
