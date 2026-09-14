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
- bf0c3a36d 更新作者技能使用规范：同步核心合同驱动、短入口、阶段文件和局部结果的使用规则。
- 已修复通用 C# 值编码器对可为空无运行时类型值的空值处理，并修复 Timeline 属性值正式类型包装和临时 catalog 变量命名。

## 输出迁移状态

此前多文件版本的 Attack 基线为 21 个文件、254719 字节、入口 819 行。该布局不是本轮最终结果。

本轮已经清理 Attack 专属目录中的旧入口、Conditions/Stages/Timelines 源码和空布局目录，等待同一 Unity Editor 会话恢复后由正式 btsmtl.export_code 从当前资产完整重建。当前未把上一次旧编译入口产生的中间输出当作最终源码统计，也未勾选第 4～5 节任务。

正式重导完成后需要记录同一导出范围的总字节数、入口字节数、阶段局部文件数及一次局部修改涉及的文件，不能只报文件数量。

## 当前编译与连接边界

已确认：

- BTSMTL.Timeline.Tree.csproj：最近一次编译为 0 个错误、17 个已有警告。
- ThirdPersonClient.Runtime.csproj：最近一次成功编译为 0 个错误、1 个警告。
- Editor 侧 CodeGeneration 源在使用已有 Unity 程序集作为引用时通过静态编译；当前完整项目仍受并行 Simulation/Pose 状态迁移的外部编译错误影响，未宣称全项目通过。
- Unity Editor 会话已经恢复并注册为 `3C_Client@e852139597e42532`。正式 `btsmtl.export_code` 已对 Attack 执行并返回 8 个文件、总计 198,414 字节，其中入口 1,608 字节；这次调用仍使用当时已加载的旧生成器程序集。
- 静态编译中间导出时发现短入口缺少命名空间闭合，源码已在 `cf715b0b9` 修复；最新生成器还没有在 Unity 中重新加载，因此当前生成目录不能作为最终输出归档。
- Unity 当前 Console 仍有并行 Fixed Simulation 源码的 1 个编译错误：`CharacterPipelineDefinitionFixedAbilityExtensions.cs:9` 找不到 `FixedGameplayAbilityExecutionData`，阻止 `ThirdPersonClient.Editor` 加载最新生成器。未修改并行 Simulation/Pose 文件、未修改 SessionState、未注入脚本、未启动第二个 Unity 实例。

因此当前缺少的是 Unity 编译恢复后用最新程序集重新导出并编译全部已采用 C# 根，不是另建一个生成路径来绕过该边界。

## 验收边界

本次不新增测试、回放或手动验证任务。静态编译、Unity Console、正式 export_code/generate_assets、Play/E2E 分别表示不同证据；在 Unity 会话恢复并完成全量重导前，不报告本变更完成。
