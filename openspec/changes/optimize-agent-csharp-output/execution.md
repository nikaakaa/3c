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
- 已修复通用 C# 值编码器对可为空无运行时类型值的空值处理，并修复 Timeline 属性值正式类型包装和临时 catalog 变量命名。

## 输出迁移状态

此前多文件版本的 Attack 基线为 21 个文件、254719 字节、入口 819 行。该布局不是本轮最终结果。

本轮已经清理 Attack 专属目录中的旧入口、Conditions/Stages/Timelines 源码和空布局目录；随后用已加载的最新生成器完成了一次有效 Attack 正式重导。当前目录保留这次 8 文件结果，但最后的导入裁剪源码尚未再次正式重导，也未勾选第 4～5 节任务。

此前旧程序集导出的 8 个中间文件已在静态诊断后清理；当前目录中的 Attack C# 是 182,305 字节的有效静态编译结果，不把它冒充为完成全部根对象迁移。

正式重导完成后需要记录同一导出范围的总字节数、入口字节数、阶段局部文件数及一次局部修改涉及的文件，不能只报文件数量。

## 当前编译与连接边界

已确认：

- BTSMTL.Timeline.Tree.csproj：最近一次编译为 0 个错误、17 个已有警告。
- ThirdPersonClient.Runtime.csproj：最近一次成功编译为 0 个错误、1 个警告。
- 核心 Program 删除前，当前 Attack 生成目录参与的 Editor 静态编译曾为 0 个错误、32 个已有警告；该结果不代表当前完整项目状态。
- 当前 `ThirdPersonClient.Editor.csproj` 静态编译失败，错误集中在并行 Simulation/Pose 状态迁移后的核心类型缺失，未宣称全项目通过。
- 此前中间 Attack 生成文件的静态编译曾发现 19 个 `timelineCatalog*` 未解析名称；这是旧程序集输出没有应用 `80055079b` 提升规则的结果，未把该旧输出当作新生成器编译证据。
- Unity Editor 会话已经恢复并注册为 `3C_Client@e852139597e42532`。正式 `btsmtl.export_code` 已用当时的最新生成器对 Attack 返回 8 个文件、总计 182,305 字节，其中入口 980 字节；文件均已写入并通过静态编译。
- 历史中间导出曾发现短入口缺少命名空间闭合和跨阶段 `timelineCatalog*` 提升缺口，源码分别已在 `cf715b0b9` 与 `80055079b` 修复；最后的无用导入裁剪已在源码提交，尚未由 Unity 最新程序集再次重导。
- Unity 当前唯一目标实例为 `3C_Client@e852139597e42532`，处于非Play、非编译、可用状态；Console 仍返回 `ProgramExecutionLayout`、`SimulationProgramCatalog`、`BlackboardInputStateBinding` 和 `TypedStateAddress` 未解析。`59cad96bd` 已明确删除这些旧 Program/Core 类型并允许旧消费者暂时编译失败，不能在本变更内恢复旧载体、添加占位类型或建立兼容路径；`034dde874` 只修复了一个独立扩展的命名空间引用。当前脏的 Fixed/Pose/Slate 并行改动未被本任务继续重构，未修改 SessionState、未注入脚本、未启动第二个 Unity 实例。

因此当前缺少的是 Unity 编译恢复后用最新程序集重新导出并编译全部已采用 C# 根，不是另建一个生成路径来绕过该边界。

## 验收边界

本次不新增测试、回放或手动验证任务。静态编译、Unity Console、正式 export_code/generate_assets、Play/E2E 分别表示不同证据；在 Unity 会话恢复并完成全量重导前，不报告本变更完成。
