# 2026-09-04：基线启动与 Solver 运行时引用

## 实验边界

本步先恢复可用的实验入口，不改变 Foot、Pelvis、Goal 或 FBBIK 求解公式。输入仍为 `43357ff3cd384e5cba75d2c31175b116` 的1044帧，通过正式 `character.fixed_input_trace` 的 `diagnostic_replay_start` 启动，使用现有 `character-foot-ik/full` 采样。

## 修复前事实

- 原渲染 CS0192 已由其它工作修复，本步不修改渲染文件。
- 首次请求在准备阶段观察到0/1044帧，随后Unity进程发生重启；该轮不计为基线。
- 重新连接同一项目后，第二次请求仍未开始消费输入，最终返回 `Canonical Fixed input diagnostic-replay timed out while starting Gameplay Lab.`，没有本轮Replay Proof或新采样包。
- Unity日志两次出现 `Serialization depth limit 10 exceeded at 'RootMotion.FinalIK::FBIKChain.reachSmoothing'`。完整层级为 `FullBodyBipedIK.solver → IKSolverFullBody.spineMapping → IKMappingSpine.spine → BoneMap.solver`，后三项继续递归。
- 状态返回的03:55采样目录是上轮缓存路径，不是本次实验产物。未清除、重分析或覆盖该包。

## 当前候选

`IKMapping.BoneMap.solver`仅作为运行时反向引用，Transform与IndexedBone两种 `Initiate` 都会设置它。给该字段加 `System.NonSerialized`，阻止Unity域重载沿反向引用复制整个Solver。没有修改初始化赋值、骨骼映射、权重或数值求解。

该修改预期消除明确的序列化循环；回放超时是否一并消失需单独验证，不能仅凭同屏出现就认定同一原因。

## 验证状态

候选尚待加载和原入口复跑。新域重载必须不再产生上述序列化错误；随后确认1044帧消费、采样封口和正式Proof。缺任一项不计为有效A/B基线，不继续叠加Foot行为改动。

候选写入后，Console请求返回2秒未响应，正式脚本Refresh请求等待30秒超时，没有重复发起刷新。之后从真实窗口标题与正式磁盘作业确认：另一任务的Performance Player构建 `0171530e4faf49d898ca17645caf4048` 正在占用同一Unity，窗口为Incremental Player Build，日志仍在编译C++。不能把MCP缓存的idle状态当作编辑器已释放。

本任务发起的回放已通过正式stop清理并退出Play；不停止另一任务的构建。旧采样未覆盖、未重分析。本次没有有效基线、没有候选采样、没有A/B通过结论。

代码核对另发现 `IKMappingLimb.solver` 也持有同类型运行时反向引用。它尚未修改；下一步需一并核对对应初始化与域重载行为，不能只凭第一个报错字段消失就宣布完整修复。当前候选只保存已定位的BoneMap循环入口，不勾选Foot行为任务。

恢复顺序：确认上述构建结束且编辑器空闲 → 确认脚本Refresh／编译实际完成 → 核对新的域重载日志与运行引用初始化 → 用同一个1044帧输入重新建立有效基线。只有此闭包成功后才开始下一项Foot行为实验。

## 构建释放后的继续核对

15:06:42，Performance构建以GameplayLabSessionVariantDefinition已销毁错误结束。随后Unity回到Edit空闲状态，新的域重载日志不再以BoneMap作为递归入口，而明确列出 `FullBodyBipedIK.solver → IKSolverFullBody.limbMappings → IKMappingLimb.solver` 的重复链，最终在 `Point.transform` 超出深度。

第二个最小修改将 `IKMappingLimb.solver` 同样标记为System.NonSerialized。它由原Initiate方法赋值，供现有HasParent等逻辑使用；保留赋值和计算顺序。两条反向引用的修复作为同一个序列化边界闭包验证。

## 第二个候选的加载结果

Unity进程重新出现后，已确认Edit空闲、无回放，清理Console成功并通过正式入口请求脚本Refresh／compile。请求在域重载时断连；日志显示脚本编译后的程序集重载，卸载旧程序集时仍记录原Limb循环错误，不能将这条旧实例记录当作新版本验证结果。

随后Unity进程退出，当前Editor.log记录Native Crash Reporting及SIGSEGV。原生栈顶为 `ShaderLab::Program::GetMatchingSubProgram → ShaderLab::ShaderState::FindSubProgramsToUse → ApplyShaderState → ApplyMaterialPass → ScriptableRenderContext::Submit`，上层为URP相机／编辑器重绘。没有启动新的候选回放，也没有采样或Proof。

该栈定位到渲染提交，但尚未定位具体shader、材质或造成崩溃的改动；不将其归因于IK，不擅改渲染配置绕过。第二个候选继续标为未验证。恢复条件变为：先让编辑器能够稳定完成域重载和当前场景渲染，再验证两条运行时引用及1044帧基线。
