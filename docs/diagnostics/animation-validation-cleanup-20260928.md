# 动画调用链重复校验清理

## 范围

按用户要求检查动画资源、播放器、同步相位、Blend Stack、Pose 图和足部曲线采样的结构校验调用。删除已建立约束的重复检查与无调用方旧合同；不修改 IK 算法、技能参数、动画数据和其它窗口的改动。

## 本轮修改

### 同步相位

`AnimationClipPhasePlan` 构造、普通动画来源的资源加载，以及 BlendSpace 相位计划创建已经检查完整相位表。`Forward`、`Inverse` 不再每次扫描全部相位节点与覆盖区间。时间、相位和覆盖范围的本次计算规则保持原样。

### 混合计划

原链路：写入单条贡献和骨骼权重 → `ValidateInactivePage` 完整检查 → `CommitInactivePage` 再完整检查 → Job 构造时再次完整检查。

现链路：写入入口确认单值 → `ValidateInactivePage` 确认完整性、贡献唯一性和总权重预算 → 提交已验证页面 → Job 使用它。

- 验证后现有 `RequirePreparing` 已禁止继续写入，因此提交时不再重复扫描。
- 已由写入入口确认的单条合法性、单个骨骼权重范围，不在页面验证阶段重复检查；页面仍检查是否写齐和总权重预算。
- 删除 Job 中重复的贡献计数、唯一性、权重预算、逐骨骼汇总检查。保留与另一份来源缓冲区对应的 SourceCaptureIndex 检查及绑定对应关系。
- 帧头在构造时确认约束；后续包装不再逐层调用相同校验。删除无调用方的 `RequireValidLayout`。

### Native 旧合同

引用审查发现 `CharacterPoseGraphNativeBinding`、`AnimationPoseNativeAggregateLayout`、`CharacterPoseOperationCompletionPage` 及两个依赖它们的子绑定构造函数仅互相引用，现行运行链没有创建或调用它们。它们为内部运行时 Native 合同，无序列化声明；同时核查 Assets/Packages 的调用与反射名称，没有外部使用。

删除这套旧合同，共 466 行。此项是死代码清理，不计作运行时性能收益；现行直接 Native 绑定入口保留。

## 已检查的其它路径

| 路径 | 当前责任边界 |
| --- | --- |
| 足部观测采样 | 上一提交已删除每帧深度校验；资源目录加载时校验曲线与事件表 |
| 足部预测曲线 | 动画播放器使用 `SamplePrepared`，未走公开 `Sample` 的完整资源校验入口 |
| ACL 描述、Rig 和 Track 绑定 | 加载、准备和绑定资源时检查 |
| Pose 图结构、惯性化策略 | 图准备、Handler 创建时检查 |
| IK 调参数据 | 准备调参候选时检查，不属于每帧求解扫描 |
| 混合曲线 | 编译/加载时检查分段，运行时直接求值 |
| 帧完成代次、页面所有权、动态贡献预算 | 为现行提交协议和当前计算结果，未当作静态配置检查删除 |

## 验证

`git diff --check` 通过。Unity Bee 最终编译记录中，`ThirdPersonClient.Runtime.dll` 的 Csc exitcode=0；`ThirdPersonClient.Editor.dll` exitcode=1，失败来自另一个窗口正在修改的 `Editor/Performance/ThirdPersonPerformanceCaptureWorkflow.cs`，包括第 942 行的 CS0106。该文件未纳入本次修改或提交。

因此动画 Runtime 源码已编译通过，但整项目仍有外部编辑器编译阻塞，不能声称 Unity 全量编译通过。没有新增测试、Play/replay 或性能测量。
