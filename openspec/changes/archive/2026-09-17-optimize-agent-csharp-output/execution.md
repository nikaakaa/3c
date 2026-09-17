# 实施记录

## 范围

本次只改 Agent C# authoring 的输出上下文、源码组织、写盘、响应和薄适配；正式 Graph、FSM、Timeline、Pose、Motion、Camera 模型与运行规则不由本变更接管。

## 代码链

- BtsmtlAuthoringCodeExportContext 记录正式对象、类型化外部引用、局部文件边界和阶段语句。
- Skill 与 Timeline 的成员、默认/覆盖语义、连接配置、owner/placement 和创建分派由正式作者合同提供，适配器只负责把当前实例值编码成 C# 调用。
- BtsmtlAuthoringCodeSourceBuilder 生成短入口、根局部实现和按正式维护边界划分的阶段局部文件；私有对象留在局部变量中，跨局部连接才进入最小 *Parts 结果。
- 写盘器逐文件比较 UTF-8 内容；不变文件不写，正常修改不触碰已有 .meta，明确生成目录内的退役源码、文件 .meta、空目录和目录 .meta 会被清理。
- btsmtl.export_code 与 btsmtl.generate_assets 仍是唯一两个显式作者入口；局部文件只是同一入口的编译依赖，不产生第二入口或中转模型。

## 已完成源码

- 6dc126b0a 补齐作者核心描述合同：新增 Skill 字段/连接合同、图闭包 placement owner、Timeline 属性/引用合同及必要程序集依赖。
- b78b5309f 压缩作者局部生成输出：删除按 emission 阶段和连续块展开的包装，改为根构建、阶段构建和收尾连接；移除无用外部资源分区接口。
- 80055079b 修正跨阶段辅助变量提升：把生成过程中分配的 Timeline 合同目录局部变量纳入类型和文件边界，避免收尾代码引用阶段局部名称。
- bf0c3a36d 更新作者技能使用规范：同步核心合同驱动、短入口、阶段文件和局部结果的使用规则。
- 本轮修复分文件 using 裁剪：保留生成入口所需的 `ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration` 正式上下文命名空间，避免新局部文件因入口命名空间不同而无法编译。
- 本轮把 Pose 只在最终收口阶段赋值的图对象归并到单一 `Graphs.cs`，删除没有独立局部语句的空 `Parts` 文件。

## 输出迁移状态

此前多文件版本的 Attack 基线为 21 个文件、254719 字节、入口 819 行；本轮最终正式重导后的同范围 Attack 为 8 个文件、182406 字节、入口 895 字节。

2026-09-17 已用公共 `btsmtl.export_code` 对明确采用的 7 个根全部重导，响应 `diagnostics=[]`：

| 根 | C# 文件 | 总字节 | 入口字节 |
| --- | ---: | ---: | ---: |
| Corin Attack Ability | 8 | 182406 | 895 |
| Corin Dodge Back Ability | 3 | 19158 | 557 |
| Corin Dodge Forward Ability | 3 | 19398 | 569 |
| Corin Attack Admission | 2 | 1701 | 438 |
| Corin Dodge Admission | 2 | 1682 | 437 |
| Corin Animation EventGraph | 2 | 23716 | 495 |
| Locomotion PoseGraph | 4 | 66727 | 631 |
| 合计 | 24 | 314788 | — |

导出器在各自专属目录内删除了旧的 `Conditions`、`Stages`、`Timelines` 以及 Pose 的 9 个空 `Graph0`—`Graph8` 壳文件；Pose 从 12 个 C# 文件、71940 字节收口为 4 个文件、66727 字节。入口只组合 `Root`、真实局部结果和最终连接；生成目录内没有 `GenerationState`、`BuildCreate`、`BuildConfigure`、`EmitNodeConfiguration` 或 `EmitClipConfiguration`。

Timeline 的 `MotionCurve` 现在只保留类型化 `RootMotionCurveAsset` 引用、正式 `curveId` 和源区间；没有输出源曲线的逐帧关键帧、采样数组或烘焙缓存。源码中仍出现的 `Keyframe` 只属于不能由源资产恢复的独立作者曲线，例如 `animation.ease-in/out` 和 `motion-warp.yaw-progress`。源码中仍出现的 UUID 文本是正式节点、图、轨道、边、声明或 owner 的稳定 identity，用于再次生成时恢复正式关系；它们不是变量名后缀、诊断 hash 或源素材副本，不能按字符串形状一律删除。

## 当前编译与连接边界

已确认：

- `ThirdPersonClient.Editor.csproj` 已按项目要求执行 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false`，结果为 0 个错误、32 个既有警告；随后已执行 `dotnet build-server shutdown`。
- 导出删除旧局部文件后，使用 Unity 正式 `Assets/Open C# Project` 菜单刷新工程文件，未手改 `.csproj`，因此静态工程与当前生成目录一致。
- Unity 实例为 `3C_Client@e852139597e42532`，当前非 Play、非编译、非导入、`ready_for_tools=true`；`read_console(types=["error"])` 返回 0 条。
- 当前自定义工具注册表按 `btsmtl.` 过滤只有 `btsmtl.export_code` 与 `btsmtl.generate_assets` 两个作者入口；没有旧 Document/Agent 生命周期工具。
- 工作区仍有并行领域和场景改动；本轮只触碰 authoring 输出器、明确 Generated 目录、skill 与本 change 文档，没有替换或回退其它改动。

## 验收边界

本次不新增测试、回放或手动验证任务；本轮只执行 `export_code` 和静态编译，没有调用 `generate_assets`、Play 或产品 Build。静态编译与 Console 只证明作者源码和编辑器程序集当前可编译，不替代用户自己的资产生成和端到端验收。
