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

选用已保存输入 `11fa0cf23ef84252b4aee9ee86905411`，共 2691 帧、1965 帧移动输入、16 次相邻移动输入方向反转。后续确认这份旧录制的全部帧均缺少 `AttackHeld` 字段；此前只统计为真的帧数，不能证明字段存在且为 false。通过 `CharacterFixedInputTraceWorkflow.ReplayTraceWithDiagnostics` 启动真实角色回放与现有脚部、表现诊断采样；不是新建测试文件或替换运行入口。

本次请求被正式工作流接受，但在启动 Fixed 会话阶段超时：`Canonical Fixed input diagnostic-replay timed out while starting the Fixed session.` 没有生成新的脚部／表现采样目录，也没有取得回放完成证明。此失败不能计为 TurnBack 效果通过或失败；截至此记录，相位代码、正式作者配置和运行资源采用已完成，真实切换检查尚未完成。没有重复启动同一次未确定状态的回放，没有修改启动超时阈值或绕过正式场景启动链。

## Package Manager 启动错误定位

2026-09-27 用户报告每次启动出现 `[Package Manager Window] The "path" argument must be of type string. Received undefined`。Editor 日志确认错误来自 `GetRegistriesRequest`，UPM 的 `project:list-packages` 与 `config:project:get-registries` 返回 500。日志中的 `params: {}` 不是项目路径丢失的证据：项目路径经请求头传递。

只读检查 Unity 2022.3.62f2c1 内置 UPM 的 `server/app.js`，在独立 Node 进程内调用原模块 `getRegistries`，使用当前项目 Packages 路径，复现相同异常。堆栈定位到 `getDeprecatedGlobalConfigRoot`：Windows 分支执行 `path.join(process.env.ALLUSERSPROFILE, "Unity/config")`。只读检查 Unity PID 40472 与其原 UPM PID 151872 的进程环境，二者均缺少 `ALLUSERSPROFILE`，但 `ProgramData=C:\ProgramData` 正常。

同一原模块、同一项目配置，只将诊断进程的 `ALLUSERSPROFILE` 补为 `C:\ProgramData`，调用立即成功，返回默认 Unity 仓库与项目 OpenUPM 仓库。没有修改 manifest、包锁、Unity 安装文件或项目代码。这一对照确认本条错误来自启动进程继承的环境变量缺失，不能据此认定此前所有回放超时都由它造成。

已确认用户级 `ALLUSERSPROFILE=C:\ProgramData` 并广播环境更新。修复前日志保留在 `tmp/turnback-mcp-recovery/upm-path-20260927/`。以正确环境、原可执行文件和同一 IPC 参数重启了本项目 UPM 子进程（新 PID 88600）；Unity Editor 保持打开。新 UPM 已监听，但截至记录时未收到编辑器请求，编辑器状态调用仍超时。现有进程不会自动继承用户环境更新；仍需保存工作并从正确环境重新启动 Unity，确认真实包列表／仓库请求成功后再继续 TurnBack 回放。当前不能声明 Package Manager 窗口恢复或相位同步视觉验收通过。

后续恢复：11:35 在实例恢复响应后，先退出暂停的 Play，确认 Edit/idle，再通过正式 `SaveOpenScenes` 与 `SaveAssets` 保存，正常执行 File/Exit。确认旧 PID 123480 退出后，在启动进程内显式设置 `ALLUSERSPROFILE` 为 Windows `CommonApplicationData`，启动相同项目和相同 Unity 版本，新 PID 45756。11:36:14 的 `config:project:get-registries` 返回 200，11:36:16 的 `project:list-packages` 返回 200；这次是实际编辑器请求成功，不只是独立模块验证。新编辑器的环境回读为 `C:\ProgramData`。只修改用户环境变量后从旧父进程启动仍可能继承旧环境，因此本次启动显式传入该标准变量；没有增加 UPM 专用配置或修改安装代码。

11:38 编辑器完成加载并确认 Edit/idle 后，通过原 `ReplayTraceWithDiagnostics` 重新启动同一份 2691 帧输入。请求已接受，后续在 Play 期间只读取磁盘日志与报告，不发送执行代码查询。回放完成与腿部表现仍需以新产物确认。

## 真实转身窗口采样结果

本次旧录制实际运行 1207/2691 帧后，遇到 `Tick input does not contain required value 'AttackHeld'`。退出 Play 后，正式诊断流程保存了已采到的数据。前 1207 帧内有 15 次 Run→TurnBack 和 15 次 TurnBack→Run，30 个混合窗口均已完整结束；不把整份录制算作成功。

- 表现采样：`3cDemo/Client/3C_Client/Diagnostics/GeneratedPresentationSampling/20260927-034009-acfa5ce2cf864a33b1eefef071405783/`。
- 脚部采样：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260927-034009-b33a806852324f46beb9e7f83bf12354/`。
- 汇总数据：[corin-turnback-runtime-20260927.json](corin-turnback-runtime-20260927.json)。按原生 lineage 对齐脚部与表现记录，不能用脚部表的左右脚行号直接匹配表现帧。
- 曲线图：[corin-turnback-runtime-20260927.png](corin-turnback-runtime-20260927.png)。展示普通入口、双脚支撑入口、双脚支撑出口三个完整窗口；脚踝曲线来自实际角色最终物理采样。

30 个窗口中，切出播放器相邻采样时间增量均为 `0.01666666753590107` 秒。同步没有改写切出时间。切入播放器在同一来源身份内没有时间倒退。普通入口实际发生在不同动画时间；双脚支撑入口首个非零权重采样时间为约 `0.18333335` 秒，这是从合法入口 `0.166666687` 秒推进一帧后的结果，不能把非零权重第一帧误当成初始入口。

两个播放器同时有权重的 360 帧中，35 帧处于有限切入的自然双脚支撑分支；其余 325 帧符合相位映射规则，最大周期相位误差约 `1.13e-7`，没有超过 `1e-5` 的不符帧。有限切出在双脚支撑内起混合时，切入 Run 使用接下来的半周期支撑锚点；因此这段时间不要求两个原始相位数值相等。直接比较原始相位会在 11 帧得到最大约 0.0391 的差异，不能混称为普通单脚相位精确相等。

脚踝世界位移的单帧 95 分位数：正常 Run 约 0.2149 米，转身混合约 0.2007 米。扣除根平移后分别约 0.1321／0.1284 米。该比较来自同一次运行的不同时间段，不是同输入修改前后的 A/B；不能单独证明所有腿部摇摆、拉扯已消失。曲线与时间映射证明同步进入真实运行链路，最终画面质量仍不能用这些统计替代。

随后选用现有新录制 `757f243033414fc7b123c97e2fcb0d70`：2716 帧均存在 `AttackHeld`，其中 251 帧按下，1418 帧有移动、8 次相邻方向反转。同一正式入口运行至 776/2716 帧时遇到 `Camera presentation requires an active TreeClip invocation`，已退出 Play，pending=false。没有给旧录制补默认输入，没有修改攻击或相机逻辑，也没有将这次完整战斗回放宣称通过。

Unity Mono 分配补充：在 Edit 模式直接加载正式运行资源中的 Run、Walk、TurnBack 相位计划，预热后调用实际 `AnimationPhaseSynchronization.Map` 共 324400 次，覆盖两种步态的整圈入口、混合持续帧及有限动画出口，当前线程新增托管分配为 0 字节。没有复制算法或写测试文件。这是 Unity Mono 下同步计算的分配证据，不是整场景、整帧或诊断采样器的零分配结论。

最终回读运行资源仍为 `Locomotion.Gait` 三个成员，TurnBack 4 个相位键和 1 个双脚支撑区间，Run／Walk 各 3 个相位键，三份计划 `RequireValid` 均成功。针对当前实例重新执行正式包列表查询，任务 `3a5a3464463b41afaae80f92487e7eca` 成功返回 65 个包，结果保存于 `tmp/turnback-mcp-recovery/upm-path-20260927/packages-after.json`；默认 UPM 日志有其它编辑器并发写入，不能把其中其它请求的 500 当成本项目这次请求失败。
