# 第五段无输入收尾修复

## 已保存的修改

- Attack5 主体完成转移改为进入 Attack5End，不再直接进入技能出口；作者入口尚未成功生成保存，不能认为运行图已采用。
- 最终 Pipeline 收尾动画从 1.45 秒恢复为 2.016667 秒，与已经修正的 WithWeaponInplace 源一致。通过正式 AnimationClip 作者服务替换、归零水平根位移，保留原资产身份。
- 收尾 FootPlacementWeight 按缺失的 34 帧向后平移，补入首段零权重，保留原收尾权重时序。
- 仅将收尾的 Motion Reference 绑定改为完整的 WithWeaponRootmotion 动画；其他绑定与 IK 求解代码未修改。
- 正式单片段足部烘焙已成功 Apply：122 个采样点，22 条配套曲线，回读状态 Same。左脚 LiftOff=47、Landing=74；双脚均无接触的样本数为 0。

## 证据

- Target：`Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_End_Inplace.anim`
- Analysis Artifact：`db7bb3c0a4f31b08446eab9602f056feadff67656fc0782a2abb6ead2edbd51c`
- Artifact Content：`6fee02c6443dd3d4a549122218f8ab53451b5add15a5d0038c7d3fbe8f72ad71`
- Registered Curves：`807d3fd51bf296bab08297a6ac8ad9e72a07fbf125ab678d3e398850aa504cb1`
- 动画位于仓库忽略的本机美术目录；本次动画修改不包含在 Git 提交中。

## 前次发布中断

`btsmtl.generate_assets` 请求期间 Unity 会话断开，磁盘 Attack Definition 没有变化。随后编辑器连续程序集重载；HTTP 服务在线，但官方 CLI 和 MCP 均没有注册的目标实例。主编辑器仍在运行，未重启、未绕过作者系统写图资产。

连接恢复后：确认 Edit 且编译完成；重新生成 CorinAttackGameplayAbilityAuthoringCode，核对自然完成边进入 Attack5End、End 动画长度为 121 帧；PublishSelected 仅发布 Attack Fixed/Float32；通过正式 ACL 发布和 Animation Domain 编译更新运行资源。

没有运行 replay，没有运行表现验收。

## 2026-09-27 23:42 后续发布结果

- 本轮开始时确认：技能资产收尾 AnimationClip 的 `m_EndTimeRaw=6227702784`，即 1.450000048 秒；动画运行资源也仍记录 1.45 秒。因此上次补长本机动画没有进入用户实际运行链。
- 本轮正式生成已保存：收尾片段 `m_EndTimeRaw=8661518336`，即 2.016666889 秒；完成边 `8e50ef17-01e6-4707-9d83-4834a41f79ef` 的目标为 `Attack5End`。
- `PublishSelected(..., "Attack")` 返回成功，Fixed/Float32 产物已发布；提交 `25fbb8cd3`。
- ACL 发布完成 32 段采样后被质量门槛拒绝，未安装新运行资源。首次调用只保留反射外层异常；第二次调用保留了内部原始错误，不能把两次失败说成发布通过。
- 确定的失败对象为 `Corin_Pipeline_Attack5_End_Inplace.anim`：Clip-to-sampling 旋转误差 140.516068 度，标量误差 0.0139160156。根骨、两肘、裙摆、武器和三个表情通道超出原门槛，没有放宽配置。
- 只读检查根骨四元数：0.5500002 秒为 `(0.5750341,0.250111282,0.331719846,-0.7047994)`，0.566666842 秒为 `(-0.5750775,-0.250025034,-0.3316636,0.704821)`；逐分量曲线在重切接缝穿过相反符号。区间细分检查得到约 179.33 度插值偏差。在临时内存副本调用 Unity `EnsureQuaternionContinuity` 未修正它，副本已销毁，未修改资产。
- 原始 dump 的同名完整收尾仍可用，但其绑定集合与衍生资产不同，不能直接替换全部内容：衍生资产还包含表情与正式注册曲线。
- 后续需要修复动作源的重切接缝再发布动画包。正式编译同时要求目标动画对应的足部分析缓存身份匹配；用户已要求本窗口不管 IK，因此已询问该段缓存由 IK 窗口更新，或是否仅授权本窗口更新这一段。未更新任何 IK 算法、参数、足部曲线或缓存。

上述阶段技能逻辑已经发布，动画运行资源尚未发布成功。

## 2026-09-28 发布完成

用户授权只更新这一段的足部缓存后完成：

- 通过 `CharacterAnimationQuaternionCurveAuthoring.NormalizeKeySigns` 同时修正关键帧四元数及其入、出切线符号；修改 Pipeline 收尾、WithWeaponInplace 收尾、WithWeaponRootmotion 收尾，每个资产修正 5 条骨骼旋转曲线。关键帧实际朝向保持不变；没有替换表情曲线或拉长动画速度。
- 正式单段采样检查：旋转误差 0 度。表情原曲线在基础采样密度下仍不满足门槛，因此编辑器采样入口按既有精度要求最多细化三次，给原生 ACL 标量编码预留原有误差预算。若细化后仍不达标，在压缩前失败。未放宽质量门槛，未改变 runtime 算法。
- 仅重建并应用 Attack5 收尾的足部缓存。122 个采样点，Apply 回读 Same；Artifact Identity 为 `9534eec4f8cb0368d80a87d12623f9abdbde132a167ef94c48d8ef01da76bb7c`。没有修改 IK 求解、参数或其他动作足部数据。
- 正式 ACL 发布成功，组身份 `19f07e26789371374144d0cf327c66b222de4d556ce17c1f36f92b90992ff3a3`。收尾片段最终采样率约 11520、23233 个采样点；Clip-to-sampling 和 Clip-to-ACL 旋转误差均为 0，标量误差分别为 `0.0004138947`、`0.0004901886`，低于 `0.001`。最终报告 publishable=true、errors 为空。
- 正式 Animation Domain 编译成功：7 个 Pose source、32 个 Action source。资源资产已保存，回读 dirty=false。
- 已保存的 Action source `4cfdcca827720544e9b01db0507ad2bf/7400000` 的 `m_DurationSeconds=2.016667`、group clip index=8。技能 Timeline 长度为 `2.016666889` 秒；两端均为 121 帧，退出完成边已在 `25fbb8cd3` 发布为进入 Attack5End。

代码提交 `626859e63`。美术动画、ACL 二进制和 Library 缓存仍按仓库规则在本机忽略目录内，本次提交不强行纳入这些文件。运行资源引用另行提交。

没有运行 replay 或新增测试。正式编译和发布已完成，可以由用户手测第五段无输入时进入完整收尾后回 Idle，以及有新输入时继续连招的行为；尚未宣称画面验收通过。
