# 跨踏面落地与输出脚掌支撑修正（2026-09-28）

## 问题证据

沿用手动采样 `b173cb6e53594670915f08aa88ba4f7a`，详见 `corin-foot-b173-review-20260928.md`。右脚 15598 首次落地，中点查询命中 0.36m；脚尖已经跨入 0.54m 踏面。15599 脚尖约 0.533m，随后在 Landing 残差衰减中降至约 0.410m。原接触目标与硬下界同时使用低级中点，未覆盖脚尖。

## 修改

### 接触校验使用脚跟与脚尖

`CharacterFootPlacementQueryShape.Sole` 在同一个 WorldQueryBackend 内拆成两个既有 Sphere + Collider.Raycast 查询；每点沿用正式层、坡度、距离和预分配命中数组。只有两点都获得有效支撑才创建该双点落地结果。

落地支持点仍表示脚底中点的位置，不把脚水平拉到脚尖或脚跟。每个命中点减去其相对中点偏移，得到该接触点需要的中点高度；选择需要抬高更多的一侧，同时保留它的 Surface 和 Normal。选中命中的原始位置仍记录于 QuerySelection；合成后的中点目标记录于 LandingPoint。

CurrentContactVerification 正式使用该查询形状。FutureLanding 仍是已发布的预测中点查询：当前落脚事件只提供 RootLocalLanding，没有提供未来脚跟/脚尖完整姿态，不能把当前足部姿态假装成未来精确姿态。观察键纳入量化后的两点偏移，查询诊断发布形状和偏移。

### 输出前按实际目标姿态确认支撑

`CharacterFootSoleSupportQuery` 是持有既有查询接口和正式设置的值类型，随 StateEvaluation 注入；无新增 MonoBehaviour、第二物理后端或配置。Lifecycle 使用与 BuildRequest 相同的脚掌旋转和权重计算待输出的脚跟、脚尖位置，通过现有 CurrentSupportProbe 正式接口查询。

`ResolveOutputSupportConstraint` 计算两点都不低于各自踏面的最低高度，和已有有效包络/接触下界取更高者，只沿 ComponentUp 修正。由 FootPlacementWeight 换算回未加权 correction，使部分权重下的有效目标也使用同一几何口径。释放、未锁定支撑、摆腿和落地都经过该出口；抑制输出、无有效目标和非 grounded 不发起查询。

未来 PreparedPlant 的只读诊断下界不覆盖真实输出支撑约束。最终约束修正仍通过 ApplyHardConstraint 写回历史；Releasing 额外同步其残差，避免下帧继续从约束前的高度起算。

### 诊断

正式全量足部采样新增 `foot/output-support`，包含待约束输出姿态的脚跟/脚尖位置、命中高度、表面身份、拒绝原因和所需位移。输出阶段的 `SafetyFloorOwner=OutputFootprint` 标识本轮足部几何约束。该观察对应最终高度夹取前的位置；最终夹取量继续记录于原 safety-floor 字段，不能把观察位置误叫作已经夹取后的点。

## 性能与边界

新增正常路径工作为每个有输出的脚两个探针，每帧两脚最多四个额外 SphereCast，使用现有 Collider.Raycast 支撑面确认及预分配数组。接触首帧的单点查询改为双点。无新增运行时集合、委托、闭包或托管分配。

保护对象是校准脚跟/脚尖，而不是鞋网格逐顶点；物理求解器若因腿长不可达未到目标，仍可能有实际骨骼误差。任一点没有有效地面时不捏造双点支撑，拒绝原因保留在采样中。

本轮优先修复错误低接触目标与释放防穿空档，不重写水平动画，不修改包络构建、骨盆、动画资源或 ACL。没有调整末级下降速度，也没有把包络末段 6mm/18cm 的几何急降写成已经修复；这项落地时机问题仍需在正确足部支撑目标上评估。硬约束可能引入补高，需要用户手测观察，而不是以“编译成功”宣称画面消除抖动。

## 验证

按用户要求不运行 replay、不新增或执行测试。Editor 项目编译结果与 Unity 加载状态在交付时补记。只提交本任务文件，保留其他窗口改动。

### 编译记录

最终 `ThirdPersonClient.Editor.csproj` 构建通过：0 错误、91 警告；日志 `tmp/foot-sole-support-build.log`。构建结束已执行 `dotnet build-server shutdown`。Unity 在 Edit 且不编译时发起刷新，域重载期间 MCP 暂时断开；恢复后的加载结论以交付记录为准。
