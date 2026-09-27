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
