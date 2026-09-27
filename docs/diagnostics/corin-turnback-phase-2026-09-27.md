# Corin TurnBack 同步排查

## 已确认的要求

当前正在播放的动画保留原来的时间与速度。切入动画在自己允许的入口内寻找对应相位。混合期间只调整切入动画，结束后从调整后的时间继续。转身中间的双脚支撑是合法内容，不能按走跑循环的左右交替规则拒绝整段素材。

## 当前资源与运行链

`CorinAnimationPresentationProfile.asset` 的 `Locomotion.Gait` 当前包含战斗 Run、Walk、TurnBack 三个成员。TurnBack 的 `m_GaitPhase`、Clip Player 入口和运行 Phase plan 已通过正式作者与编译入口保存并回读。作者配置完成与运行画面确认分别记录。

原生 Clip Player 之前没有消费同步组；原生 StateMachine 直接混合两个各自推进的播放器。本次接入的链路为：Profile 同步组 → 动画领域资源中的 Clip Phase plan → StateMachine 的当前源和切入目标 → Player 有效时间 → 同一时间的 Pose、脚特征和属性采样。原始时钟不被重写。Reset、退出同步和帧丢弃不保留失效关联。

## 从实际数据读取的结果

2026-09-27 通过 Unity 实例 `e852139597e42532` 读取当前 Ready 分析缓存，并在隔离 Preview Scene 采样 dump 与烘焙动画。预览场景已关闭，没有保存到正式场景。

| 素材 | 精确 GUID | 观察 |
| --- | --- | --- |
| 战斗 Run FootMotionTarget | `4e613663635adb8428d84960c297d5d7` | 首段右脚支撑；约 0.267 秒左脚落地时，右脚校准足底高度约 0.441 米 |
| 战斗 TurnBack dump | `835befbd18dffcb9734b445f90fc0af9` | 首帧与战斗 Run 首帧腿姿相同；先右脚支撑，再进入双脚着地，之后继续转身并恢复跑步 |
| 战斗 TurnBack FootMotionTarget | `ac3b96331bba6754aa9bc13302762790` | 约 0.167 秒第一次左脚落地，两脚校准足底高度约 -0.002／0.002 米；之后长时间双脚接近地面；约 0.933 秒再次出现左脚 Landing |
| MainCity_Run_TurnBack dump | `74e2307a050733d4c904cce5abf2ef14` | 从左脚支撑起步；首帧左右脚骨高度约 0.091／0.423 米；素材长度约 1.35 秒，与战斗版本约 1.183 秒不同 |

表中校准足底高度与脚骨高度不是同一种量，不应直接混算。Run 与 TurnBack 的校准足底数据来自同一个分析源；MainCity 行只用于确认起步侧，尚未作为正式 Foot Motion Target 分析、发布。

dump 还存在 MainCity_Run_End_L／R 与 MainCity_Walk_End_L／R。能证明原素材具有按左右脚分开的部分动作，不能从这些动画文件推断原游戏状态机、同步组或切换算法。

## 尚未解决的边界

只把 Run 的左脚 Landing 与 TurnBack 的第一次左脚 Landing 都写成 0.5，不能证明两只脚的姿态相容：前者另一只脚抬起，后者两脚着地。有限素材整段相位可计算，也不等于允许从整段任意位置开始转身。不能放宽入口到转身结束之后来获得“匹配成功”。

MainCity 版本提供另一侧起步的实际素材，但它不是战斗版本的自动等价替换。需要核对持械姿势、转身时序、正式 Motion Reference 与控制层运动时间；不能仅因为起步脚相反就切入现有战斗状态。当前控制层 MovingTurn 的运动时间为 28/60 秒，出口混合约 0.3 秒，二者也不能被当作素材全长。

本次没有扩大 TurnBack 的允许入口、替换为 MainCity 动画或增加响应等待。TurnBack 相位曲线、正式入口、同步组和运行资源已经保存。通用代码编译通过不能作为腿部拉扯消失的证据；真实运行效果另行记录。

当前范围按作者要求只处理战斗素材，城市素材不作为修复方案。

## 本次落地与资源采用状态

通用同步代码通过 `ThirdPersonClient.Editor.csproj` 单工作进程编译，0 错误、34 警告，构建服务器已关闭。

战斗 Run 与 Walk 原注册相位在首帧分别为 -0.03333334、-0.02631579，而当前 Ready 分析缓存的右脚 Landing 在首帧。已通过正式相位 AuthoringService 修正这两条注册曲线；Run 的左右脚锚点为 0／0.266666681 秒，Walk 为 0／0.3 秒。曲线保存后两份脚部分析缓存仍为 Ready，没有重新执行脚部分析。

动画领域资源编译发现当前 28 Clip ACL 组缺失或过期。本次随后执行正式 `Publish ACL Resources` 菜单，完成了 28 段读取／采样，但整个组的质量门槛失败：

- Clip：`Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_End_Inplace.anim`。
- Clip-to-sampling 最大旋转误差：140.516068°；Clip-to-ACL：140.51326°。
- 诊断包括 Bip001、左右 Elbow、裙摆、武器骨骼及两个表情标量。

该次发布失败意味着不能据此宣称运行资源已采用新的相位计划。攻击资源由其它任务处理，不是本任务继续修改同步代码、保存战斗 TurnBack 正式作者配置或独立检查相位映射的前置条件。本任务不修复攻击素材、不放宽其质量门槛，也不反复发布整组 ACL。

作者配置、相位计划编译、运行资源采用和实际画面效果分别记录。旧发布失败不是当前资源采用状态：本次同步组保存后，正式 `Compile Animation Domain Resources` 已完成 7 个 Pose source 和 28 个 Action source 的编译；运行资源回读包含 TurnBack、Run、Walk 的三份 v3 相位计划。没有再次发布整组 ACL，也没有处理攻击资源错误。

## 双脚支撑处理进展

战斗 TurnBack 的 Ready 分析缓存包含连续双脚支撑区间 `0.166666687–0.5166667` 秒，共 22 个采样点。本次通用实现从正式分析缓存提取区间，并编入 Clip Phase plan；不为 Corin 在运行时写特例。计划入口限制为 `0–0.166666687` 秒，入口混合时长约 `0.12` 秒，不到转身尾部寻找起播位置。

有限切入在允许入口内比较普通相位对应点与可覆盖完整剩余混合时长的双脚支撑点。选中支撑区间后按切入动画自身速度推进，切出动画时间不变。双脚支撑成立只证明两脚支撑覆盖，不等于两段腿姿完全相同；仍需检查逐帧映射和混合效果。

最新双脚支撑实现及统一 `CompilePhasePlan` 入口的静态编译通过，0 错误、32 警告。本次确认 Unity 已加载 v3 相位计划程序集后，发现 TurnBack 分析缓存身份已过期，因而只重新分析该战斗 TurnBack，再通过 `CharacterLocomotionPhaseAuthoringService.Apply` 保存曲线。保存后缓存为 Ready，`CompilePhasePlan` 成功返回以下锚点：`(0,0)`、`(0.166666687,0.5)`、`(0.9333334,1.5)`、`(1.1833334,2)`。

随后 PoseGraph 只读导出一度未成功返回，编辑器状态读取连续超时，目标 Unity 进程报告无响应。连接恢复后正式只读导出成功；对比现有源码，保留当前有效配置，仅修改 TurnBack 构造参数及其子图内容版本。最终源码编译 0 错误、32 警告，构建服务器已关闭。Unity 完成程序集加载后，正式 `btsmtl.generate_assets` 成功保存资产，没有创建或删除资产。

保存后的 `corin.locomotion.turn.sequence` 入口为 `0–0.166666687` 秒，`corin.locomotion.turn.graph` 内容版本为 `fe3179c25b674435a555a5bf3adde634`。与执行前资产对比，仅入口值和该版本号变化。没有重启 Editor，也没有重发整组资源发布。后续同步组请求一度因连接中断失败；用户明确授权重启共享 MCP 服务后恢复连接，通过 `SetLocomotionSyncGroups` 保存并确认组内三个成员。

## 已执行的独立映射检查

检查直接加载本次 Unity 编译的 `ThirdPersonClient.Runtime.dll` 与对应 `UnityEngine.CoreModule.dll`，调用实际 `AnimationPhaseSynchronization.Map`；没有复制算法或新增运行入口。Run、Walk、TurnBack 的相位键从已保存的正式动画文件读取。双脚支撑区间使用本次 Unity 正式 `CompilePhasePlan` 返回的数据。诊断计划中的资源身份字符串只用于内存构造，没有保存为正式资源。

- Run、Walk 各覆盖一整圈 120 个入口，每个入口检查 0.12 秒混合内的 13 帧，步长 0.01 秒。共 3120 个映射点，无异常、无越出允许入口、无时间倒退；实际起播范围为 `0–0.166666687` 秒，相邻映射时间增量最大为 0.01 秒。
- TurnBack 在 0.3、0.466666667、0.633333354、0.9 秒分别退出到 Run，检查各自 0.3 秒混合的 31 帧，没有时间倒退。有限动画到结尾时按播放器语义钳制采样时间。
- 0.466666667 秒退出时，Run 映射时间在退出后 0、0.1、0.2、0.3 秒依次约为 `0、0.01159、0.08116、0.15072` 秒。这显示双脚支撑出口会先保持切入的支撑姿态，再继续推进；是否造成可见停顿仍需画面确认。
- 在 .NET 10.0.11 下直接加载 Unity 已编译的同一运行程序集，预热后执行 440000 次 `Map`，包括单脚相位入口、双脚支撑入口和有限动画出口，当前线程新增托管分配为 0 字节。测量循环在内存中编译，没有新增测试文件或产品运行入口；该结果不替代 Unity Mono／Player 的整帧分配检查。

这些独立检查仅确认指定数据的时间映射和算法分配，不覆盖实际 PoseGraph 调用、骨骼混合或脚部空间位置，不能据此宣称腿部拉扯已经消除。

## 正式运行检查

选用已保存输入 `11fa0cf23ef84252b4aee9ee86905411`，共 2691 帧、1965 帧移动输入、16 次相邻移动输入方向反转，没有 `AttackHeld` 为真的帧。通过 `CharacterFixedInputTraceWorkflow.ReplayTraceWithDiagnostics` 启动真实角色回放与现有脚部、表现诊断采样；不是新建测试文件或替换运行入口。

本次请求被正式工作流接受，但在启动 Fixed 会话阶段超时：`Canonical Fixed input diagnostic-replay timed out while starting the Fixed session.` 没有生成新的脚部／表现采样目录，也没有取得回放完成证明。此失败不能计为 TurnBack 效果通过或失败；截至此记录，相位代码、正式作者配置和运行资源采用已完成，真实切换检查尚未完成。没有重复启动同一次未确定状态的回放，没有修改启动超时阈值或绕过正式场景启动链。
