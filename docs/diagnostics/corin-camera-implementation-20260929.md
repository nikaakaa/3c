# 可琳相机复刻实施记录

## 范围与状态

最新分工：用户已将命中链交给其他窗口，本窗口只负责相机配置。此前已进行的命中数据修改与真实执行缺口见 [命中链交接](corin-hit-chain-handoff-20260929.md)；下文早先的整链授权保留为过程记录，不再代表当前工作分配。

2026-09-29，用户已创建实现 goal，并明确不运行 replay。本文记录第一项可独立交付的震动核心修正；整个相机 goal 尚未完成。

依据：[震动消费者调研](corin-camera-shake-replication-20260928.md)。本次保留既有技能触发和 OnExit 选择性取消入口，没有重建角色动画、IK 或技能资产。

## 本批调用链

1. `CameraShakeAsset.CustomCurveKey` 仍由作者资源提供。`CameraShakeProjectionCompiler` 在正式 Profile 曲线集合中解析它，发布 `CameraShakePayload.CustomCurve`，运行时不再只持有未使用的字符串。缺少对应曲线在编译边界报错。
2. `CameraShakeEffectEvaluator` 对每个存活实例独立求值；位移采用余弦半幅信号，NoiseRatio 控制附加 Perlin 振幅，NoiseAngle 单独控制方向变化，首个采样不加方向噪声。已移除身份散列相位及 1.07/1.13 频率差。
3. 震源初始方向与距离保存在已分配的效果实例中；RealtimeVibration 只决定距离是否逐帧更新。mode 5 按真实距离读取空间曲线，不经过 DissipationDistance=0 的普通距离分支。
4. 基础震动按资源 PlayPriority、当前位移模长选择；附加震动累加。每个实例继续推进时间，未选中的基础实例不会重新起播。相等位移不替换已有候选。
5. 旋转按 Pitch/Yaw/Roll 的 0.25/0.5/0 相位求值，欧拉合成后右乘已有朝向。位置输出与旋转分开应用，避免修改轨道朝向时额外绕 Pivot 移动相机。
6. raw ShakeType 0 使用触发时震源方向，1 使用当前相机方向，2 在当前透视相机的屏幕像素空间转换。`ICameraRigAdapter.PixelHeight` 提供实际输出尺寸，经 `CameraFrameInput` 传给求值器；没有把窗口分辨率写成常量。当前可琳 13 项资源均为 raw 0，raw 2 尚未画面对照。
7. 正时长震动收到明确取消后不再输出，也不人为添加固定 0.016 秒尾巴。负时长效果按资源淡入／淡出曲线处理；取消时保留上一帧包络权重。负时长不强制要求仅供正时长使用的时间曲线。
8. 效果生命周期改为当前采样输出后判断是否已经结束，再推进时间；因此终点采样不会因先加下一帧 delta 而被提前删除。此顺序统一作用于现有相机效果 owner；与旧实现相比，有限时长效果最多保留到下一次终点采样。

所有求值新增状态复用既有池对象，循环内没有新增托管对象、LINQ 或动态容器。未进行运行时 GC 采集，因此这里只声明源码检查结果。

## 时钟与未完成业务

当前 `GameplayTickSystem.PresentFrame` 提供共享 Tick 时钟，OwnerTimeScale、LocalAvatarTimeScale 均为 1，没有原游戏 `DHDAMGEBBHN` 的独立倍率聚合器。当前震动消费 `ScaledDeltaSeconds`，不再把 RealtimeVibration／IngoreTimeScale 解释成无缩放时钟。

这只完成默认时钟入口修正。原游戏额外倍率、该倍率的 IngoreTimeScale 绕过、基础选择保持计数和整体静默尚未接到项目业务输入；不能把这些功能描述成已复刻。后续必须先确认实际输入来源，不能将 OwnerTimeScale 或 HitStop 猜作原始聚合倍率。

## 检查

- 第一轮 Unity 脚本编译完成；目标项目为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`，Edit 模式，Console 错误数为 0。
- 正式 `CharacterCameraProjectionBuilder.Build` 和产物检查成功，schema 为 `character-camera-projection/v3`；13 项 Shake 全部绑定 `Camera_ShakeSpatial_Curve_01`。Normal_01 命中资源的 IgnoreTimeScale=true，其余 12 项为 false。
- 取消与负时长边界整理后的第二轮 Unity 编译及正式投影构建也已完成，Console 错误数仍为 0。
- 未运行 Play、replay，未新增或修改测试代码。没有手感已经一致的结论。

## Goal 后续工作

- 补齐独立倍率／保持与静默的业务证据及必要输入。
- 修复 E 的 Start → Loop／Walk 镜头保持及明确退出，沿正式连续 Clip／节点生命周期实现。
- 接入正式命中结果对应的震动资源与 ShakeStrength，保留 ShakeOnNotHit 规则。
- 核对基础轨道、FOV 和持续推镜的原消费者，完成当前可琳技能的相机交付。

## 持续区间播放合同

当前 E 的 Start／Loop／Walk 各自拥有时间轴，Start 中的 CameraEffectClip 在状态切换时自然退场。核对后，原时间轴只支持 Once／Loop，并行节点也只支持等待全部子流程结束，无法直接表达“镜头持续、随主流程完成而停止”。本批在正式合同中扩展：

- `TimelinePlaybackMode.HoldLastFrame`：时间推进到内容末尾前最后一个 FixedScalar 采样点后保持，Cycle 不递增、播放状态保持 Running，活跃 Clip 直到 owner 停止才退出。表现采样使用提交的定点区间插值，保持时区间两端相同，不经过浮点时长重新定位。
- Fixed／Float32 的技能 Timeline 请求传递完整播放模式，不再压缩成 bool Loop。快照捕获、恢复、编码和解码同步支持该模式。
- `BtsmtlSkillParallelMode.FirstChild`：第一个子流程决定完成结果；它结束后，通过已有 ParentStop／ContinueStop 机制停止其他子流程，等待退出完成后返回主流程结果。退出期间仍保存第一个子流程的成功／失败结果。

目的：让 E 持续区间成为父状态，Start／Loop／Walk 作为内部状态机执行；CameraEffectClip 在伴随时间轴中保持。进入 Explode 或整个技能被取消时，由正式父子生命周期停止伴随时间轴。资产重组、Start 时间轴清理和 BranchAttack 双数值产物已正式生成并发布；画面效果尚未经手测。

运行态快照删除了与 PlaybackMode 重复的 Loop 字段，Fixed／Float32 编码版本由 11 升至 12。旧版本运行态快照不再直接恢复；不保留另一套兼容读取路径。尚未据此判断固定输入录制文件的兼容性。

编译状态：HoldLastFrame、FirstChild、作者源和局部发布入口均完成 Unity 编译及重载，Console 错误数为 0。未运行 Play、replay 或新增测试。

## E 持续镜头发布结果

- 外层 FSM 为 Hold → Explode → End；Hold 内层保留 Start → Loop / Walk 的输入与窗口条件。
- Hold 主体使用 FirstChild 并行：第一支执行内层 FSM；第二支播放私有 `Branch Hold Camera`，模式 HoldLastFrame。
- 私有时间轴引用 dump 的 Branch_02 Zoom_01 / Stretch_01；区间长 0.25 秒，末帧保持，资源自身的 Hold=-1 与退出时间仍由原有相机效果求值器消费。Start 原来的两条重复镜头 Clip 已删除；其动画、位移、Release 窗口和震动触发保留。
- Loop / Walk 切换不结束父状态；释放或中止通过 ParentStop 停止镜头时间轴。Walk 的释放条件具有独立 owned graph，不复用 Loop 条件的 placement。
- `GameplayAbilityExecutionDataAssetPublisher.PublishSelected` 原来只更新执行二进制，遗漏新私有 Timeline 的运行目录登记；现在与全量发布共用 MergeTimelines，保存正式 Definition 的目录。
- `btsmtl.generate_assets` 分别保存 BranchAttack Ability 与 BranchStart Timeline；`PublishSelected(..., "BranchAttack")` 发布 Fixed / Float32 成功。
- 回读 Definition 的 Timeline 目录为 24 项；其中 Branch Hold Camera 为 0.25 秒、2 个 Clip、dirty=false。

首次生成因作者创建顺序错误而失败，工具正式回滚：新父主体图尚未挂接时创建了嵌套 FSM，原生 FSM 事务对整个资产文件执行未引用清理，删除了尚未进入闭包的对象。现将状态主体、条件图在创建后立即绑定正式 owner，再继续创建其他对象，重新生成成功；未放宽清理或闭包约束。

## 命中震动资源与后续消费者

已通过正式资源作者入口补齐 Branch_02、Rush 的 A_01 命中震动，Profile 现有 15 项 Shake，投影构建通过。两项资源各 24 个标量／字符串字段及曲线引用已与 dump 对齐。真实命中结果与触发链尚未完成；用户已授权本轮一并补正式命中链。

新增基础轨道消费者证据、当前命中链缺口及授权范围见 [基础镜头与命中链补缺](corin-camera-basis-and-hit-20260929.md)。该文档明确区分已发布资源、已取得证据和未实施的链路，不把资料齐全等同于运行效果已经一致。

## 分工调整后的相机资源补齐

用户将命中链分给其他窗口后，本窗口继续核对三个现有技能的相机引用。Attack 作者目录的 20 项攻击配置需要 6 个 A 类震动键，RushAttack 的 3 项需要 1 个，BranchAttack 的 6 项需要 2 个。

原 Profile 的 15 个资源缺少下列 6 项，现已由 `CorinCameraResourcesAuthoring.Publish()` 正式保存并登记：

| 资源后缀 | AngleVertical | RadiusLength | Frequency | StandardConfigKey |
| --- | --- | --- | --- | --- |
| Normal_02_CamShake_A_01 | 260 | 0.025 | 12 | CamShake_A_01 |
| Normal_03_CamShake_A_01 | 90 | 0.025 | 12 | CamShake_A_01 |
| Normal_04_CamShake_A_01 | 0 | 0.025 | 12 | CamShake_A_01 |
| Normal_04_CamShake_A_02 | 80 | 0.025 | 12 | CamShake_A_01 |
| Normal_05_CamShake_A_01 | 110 | 0.025 | 12 | CamShake_A_01 |
| Branch_02_CamShake_A_02 | 180 | 0.05 | 20 | CamShake_A_03 |

所有键均以前缀 `Corin_Attack_` 开始。全部采用 dump 的时长 0.6000000238418579、NoiseAngle=10、NoiseRatio=0、IngoreTimeScale=true、空间曲线 01 与衰减曲线 02；数值以 float32 对账，不以表格显示精度判断。

新增 6 项共 162 个字段／曲线引用检查全部一致，Profile GUID 引用全部存在。证据见 [新增资源对账](camera-basis-runtime-20260929/additional-hit-shake-assets.json)。Unity 编译重载后 Edit/非编译、Console 0 错误；正式 Profile 投影 Build/RequireValid 成功，Shake 数量 21，dirty=false。

本批没有启动 Play/replay，没有新增测试，没有继续修改命中运行链。资源齐全不等于命中消费者已经接通；相机基础轨道和额外时钟输入的早先未完成项也没有因此视为完成。

## Zoom／Stretch 播放叠加映射修正

本批确认了一个真实配置错误：现有 18 项 Zoom、18 项 Stretch 的原始 `PlayStackingType` 均为 0，但作者资产保存为项目 `Add=2`。

证据不是按枚举声明顺序猜数值：829 元数据中 ConfigCameraZoom、ConfigCameraStretch 与 ConfigCameraShake 的该字段均引用同一 `ConfigDataPlayStacking` 类型描述符 `0x7ff97608ed20`；已取得的原 Shake 管理器消费者明确将 raw 0 纳入 Base 候选、raw 1 加入 Additive。对应项目值为 Replace=1、Add=2。类型与字段证据见 [Zoom/Stretch 配置类型](camera-basis-runtime-20260929/zoom-stretch-config-types.json)、[枚举类型](camera-basis-runtime-20260929/camera-config-enums.json)，分支证据沿用震动复刻文档第七节。枚举导出器没有展开 literal 常量表，因此没有把导出的字段 ordinal 当成枚举值。

修改链路：

1. `CorinCameraResourcesAuthoring.PublishPlaybackStacking` 从原始 zoom-0 / stretch-0 JSON 读取当前 Profile 对应资源的 PlayStackingType。
2. 通过 `CameraZoomAsset.ConfigurePlaybackStacking`、`CameraStretchAsset.ConfigurePlaybackStacking` 正式作者入口写入映射值，并逐资产保存。
3. 原有 Projection 编译器把该值交给相机运行数据；当前 Stretch 求值器据此区分 Base 候选与 Additive，修正后不会再因这批错误的 Add 配置将所有存活的 Stretch 逐项叠加。

Zoom 当前求值器尚只使用另一字段 `StackingType` 筛选，未消费 `PlayStackingType`。本批修正 Zoom 正式资源的映射，不将其描述为已经实现原作完整的播放仲裁。两种 Stacking 字段的职责不同，不能将它们合并。

正式 Publish 后 Profile 为 21 Shake、18 Zoom、18 Stretch，dirty=false；投影 Build/RequireValid 成功。36 项磁盘资产与提交前比较，唯一业务字段变化是 PlayStackingType；其他小数变化均验证为同一 float32 数值。见 [36 项对账](camera-basis-runtime-20260929/zoom-stretch-stacking-audit.json)。源码编译重载完成，发布前 Console 0 错误；未运行 Play/replay，未新增测试。

## Zoom 输出选择消费者与修正

进一步沿 `CameraDataAccessor.Zoom` 的实例列表找到原作实际消费者 RVA `0x11FE63B0`。直接调用关系和完整指令分别见 [调用者](camera-basis-runtime-20260929/zoom-stretch-callers.json)、[Zoom 组选择](camera-basis-runtime-20260929/consumer-0x11fe63b0.json)。

- `0x11FE6494–0x11FE64A0` 对实例调用 `HGGGKJKDEEI.EMPJHIKLNHB`（`0x12BB3F80`）更新当前值。
- 实例 `+0x3C` 为 PlayStackingType，`+0x58` 为当前有符号 FOV 改变量。raw 0 与 raw 1 分别进入两个候选组。
- `0x11FE64D3–0x11FE64F7` 和 `0x11FE64FC–0x11FE6520` 分别比较当前改变量的绝对值；只有严格更大才替换，绝对值相等保留先遇到者。两组都取一项，Additive 组也不是全部累加。
- `0x11FE6587–0x11FE65AB` 把两组有符号改变量相加，再加到 `TargetCalcData.cameraFov`。本层没有按请求身份、代次或 DataPriority 排序。
- 对应 IFix 分支在 829 快照均为 0；初始化字节与 IFix 开关分别解释，见 [分支状态](camera-basis-runtime-20260929/zoom-branch-flags.json)。

当前 `CameraZoomEffectEvaluator.Apply` 已按这一输出层规则修改：各项求得当前改变量，依 PlayStackingType 在 Base/Additive 组内取绝对值最大项，最终一次性加回基础 FOV。删除了该 owner 对通用 Select 和旧逐项改写 FOV 的依赖。字段资源查询直接依赖请求接纳时已有的 HasResource 约束，清理了该 owner 在推进、到期、退出中的重复资源校验与返回零的兜底；循环内没有新增分配。

实例生命周期尚有独立差异：`0x12BB4F70` 按 CameraConfigDataStacking 和 ValueVariationType 设置起始改变量、目标改变量与初始 FOV。其调用者 `0x11FE6090` 已定位：Base 传入 TargetCalcData.cameraFov（可由全局 FOV 栈正值覆盖）及上一帧 Base 改变量；Additive 传入 0 作为参考 FOV 及上一帧 Additive 改变量。当前求值已保留 Additive 的零参考，见 [初始化调用者](camera-basis-runtime-20260929/zoom-initialize-caller-0x11fe6090.json)。原作能继承组内当前改变量开始过渡；本项目仍使用既有包络／实例启动逻辑，尚未实现这项继承和全局 FOV 栈输入，不宣称 Zoom 的完整衔接已复刻。

Unity 实际重载了新 `EvaluateOffset` 方法（3 个参数），编译后 Console 0 错误，18 项 Zoom 的正式 Profile 投影 Build/RequireValid 成功。只做源码、编译及投影检查；未运行 Play/replay，未新增测试。

## Zoom 跨段起始值继承

本批继续实现前节已定位的初始化合同。当前可琳 Zoom 的原始 CameraConfigDataStacking 均为 0，对应继承本组当前改变量：

- `CameraZoomEffectEvaluator` 保存上一帧已选出的 Base／Additive 改变量。新实例初始化时读取所属组的值，保存起点及目标；本帧多个新实例使用同一份上一帧结果，不依赖本帧遍历到哪一项。
- 绝对 FOV 的目标是配置值减去初始化时的参考 FOV；相对 FOV 的目标为继承起点加配置增量。Base 参考当前阶段输入 FOV，Additive 参考为 0。实例建立后不再随每帧基础 FOV 重算目标改变量。
- 起始曲线在继承起点和目标改变量之间插值。保持期输出目标，正常退出向零插值。明确取消时按正式 RetireStartElapsed 采出取消点的改变量，再按 EndCurve／EndTime 退出。
- `CameraEffectRuntimeState` 的三个 Zoom 字段随现有池槽 Reset 清空；`CameraEffectEvaluator.Reset` 同时清除 Zoom owner 的两组历史。没有新增每帧对象、容器或额外状态源。

因此旧 Zoom 已造成 +2° 偏移时，新段可从 +2° 起步，避免新实例从零幅度起播造成的中间回落。这是源码行为说明，不是画面对照结果。

Unity 编译与域重载完成，Console 0 错误；编辑器回读确认新 SampleOffset 方法、ZoomStartOffset 字段和 owner Reset 已加载，正式 Profile 投影 Build/RequireValid 通过。未运行 Play/replay，未新增测试。

独立全局 FOV 覆盖栈尚未接入；本批使用现有相机阶段输入作为 Base 参考。原作其它实例接纳、优先级与资格规则仍需继续映射，不能把起始值继承完成等同于全部 Zoom 生命周期还原。新增资格函数 `0x12BB4AA0` 的完整指令保存为 [zoom-eligibility.json](camera-basis-runtime-20260929/zoom-eligibility.json)，本批未据未完成的字段映射改变资格行为。
