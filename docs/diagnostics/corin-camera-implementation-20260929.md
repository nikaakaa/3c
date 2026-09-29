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

## 基础轨道曲线与坐标顺序

用户已将命中链分给其他窗口，本批仅修改相机轨道投影及其求值器。

原消费者 `track-prepare`（0x109EBE40）把 Top/Middle/Bottom 原始轨道数组反向构成归一化位置 0/0.5/1，即 Bottom/Middle/Top。ScreenY 同样按 Bottom/Middle/Top 建曲线。项目资产保存的 CameraOrbits 原来沿用原始 Top/Middle/Bottom 顺序，而 ScreenOffsets 为 Bottom/Middle/Top；旧求值器直接按相同索引线性插值，两个通道实际方向相反。

此次继续取得向量和标量的完整曲线构建、控制点求解及求值函数：

- 向量构建 0x10A1B620、控制点求解 0x10A1C9E0；标量构建 0x1E891F30、控制点求解 0x1E8932C0。两者使用相同的开曲线三对角方程：首行 (0,2,1)、内行 (1,4,1)、末行 (2,7,0)，右端为 p0+2p1、4pi+2pi+1、8pn-1+pn；消元与回代求第一控制点，第二控制点为 2pi+1-control1i+1，最后一个为 (pn+control1n-1)/2。
- 该算法与本地 Cinemachine 2.10.7 `SplineHelpers.ComputeSmoothControlPoints` 一致。曲线运行时采用三次插值；原消费者把控制点转换为按区间时间归一化的切线后求值。
- 新证据 JSON 已登记在 `camera-basis-runtime-20260929/manifest.json`，保留函数字节哈希。此次未再次扫描或重算此前震动资源对账。

修改链路：作者资产继续保留 dump 的轨道数组顺序，Inspector 明确标记两组数组各自顺序。`CameraSequenceProjectionCompiler` 在投影构建时把轨道转换到 Bottom/Middle/Top，并将 Height、Radius、ScreenX、ScreenY 四个独立标量放入 Vector4，使用 Cinemachine 工具一次性计算各轴控制点。`CharacterCameraFramePlanner` 每帧按归一化位置直接求三次曲线，输出轨道高度、半径和屏幕位置；删除旧的两套线性插值及空轨道返回零分支。新增分配只发生在投影构建阶段。

投影格式更新为 v4，携带控制点；既有 v3 投影需要由正式构建入口重新生成。当前基础轨道 ElevationRatio 仍为 0.5，输入仍走既有角度链；本批不将默认配置切换到 0.6，也不擅自推断输入到归一化轨道的映射。中点采样本身不会因此改变，不能用本批修改声称日常转视角手感已经复刻。TopOrbit 延伸、输入归一化及 Delay 消费仍未闭合。

Unity 已完成编译重载，同一目标实例回读为 Edit／非编译状态；正式 CharacterCameraProjectionBuilder.Build 和产物 RequireValid 成功，返回 schema=v4、Profile dirty=false，Console 错误数为 0。运行装配 CharacterCameraRuntimeBindingBuilder.Prepare 会经同一入口重建投影，无需动画域烘焙。未运行 Play/replay，未新增测试；未进行运行时 GC 测量。

## Stretch 分通道输出选择

原 Stretch 输出消费者已定位为 `0x101B6330`，见 `stretch-output.json`。它直接遍历 CameraDataAccessor+0x3F8 的实例列表并更新实例，然后对 PlayStackingType 的 Base／Additive 两组分别处理：

- 半径：比较实例 +0xA0 的绝对值，严格更大才替换（0x101B64EB～0x101B6519；Additive 对应 0x101B655D～0x101B658B）。
- 位置：先经 0x101B7470 转换当前实例的坐标，再加当前跟随 Y 偏移；比较世界偏移向量长度，严格更大才替换。选择的是整个向量，不是各轴分别取最大（0x101B65F0～0x101B6835，Additive 对应 0x101B670C～0x101B68C9）。
- 滚转：比较实例 +0x9C 的绝对值，严格更大才替换（0x101B6839～0x101B6859、0x101B68CD～0x101B68ED）。
- 两组各自选择后再相加；本层不按 DataPriority 或事件身份选中一整条实例。对应 IFix 开关在 829 快照均为 0，初始化字节单独保留在 `stretch-branch-flags.json` 中。

当前 `CameraStretchEffectEvaluator.Apply` 已将原来的“选一整条 Base，再把 Add 逐条改写到 plan”替换为上述三通道选择。每项从同一个输入 plan 求贡献，避免前项修改后的镜头方向又改变后项偏移的坐标系；两组选择结束后一次性合成输出。局部贡献是值类型，无每帧容器或对象分配。资源缺失仍由请求接纳的 HasResource 边界处理，删除本 owner 求值、推进、到期和退出时的重复资源保护／返回零分支。

此批只替换已确认的三通道输出选择，不代表整个 Stretch 已还原。归一化仰角仍经过原来的单独规则；当前技能使用的 Branch_02、Normal_03、Normal_05 资源没有开启仰角修改。现有半径参考比例、跟随偏移变换与实例进入／退出计算仍需继续对齐，不能将其描述为已通过原作动态画面对照。

进一步定位到初始化函数 `0x17887240` 及调用者 `0x101B6210`：原始 StackingType=0 时继承所属组上一帧的半径、位置和滚转；当前 Stretch 作者资产及投影缺少这一字段。进入求值 0x17885330、退出求值 0x17886E40 及相关 phase 函数均已保留。本轮未强行以 Zoom 初始化规则代替这条链；接下来应补齐正式 StackingType 配置、原始半径参考比例及跨段继承。

Unity 编译重载完成，目标实例为 Edit／非编译状态；回读确认 StretchContribution 已加载，正式 Profile 的 18 项 Stretch 投影构建与 RequireValid 成功，Profile dirty=false，Console 错误数为 0。未运行 Play/replay，未新增测试；未做运行时 GC 或原作画面对照。

## Stretch 正式 StackingType 与跨段起点

依据前节 `0x101B6210` → `0x17887240` 的初始化链，Stretch 的两个叠加字段分开保留：PlayStackingType 决定 Base／Additive 组，原始 StackingType=0 决定从该组当前输出继承起点；初始化函数对非零值使用零起点。本批未根据枚举声明 ordinal 推断另两个非零值的接纳或多实例规则。

- `CameraStretchAsset` 增加原始整型 StackingType，`ConfigureStacking` 同时接收播放分组及原始实例叠加值。`CorinCameraResourcesAuthoring.PublishStacking` 从同一 stretch-0.json 正式写入；18 个可琳键的原始 StackingType 全为 0。旧的 Stretch ConfigurePlaybackStacking 入口已替换，Zoom 原有入口继续承担 Zoom 自身配置。
- `CameraStretchProjectionCompiler` 将值传入唯一正式 payload，投影版本更新为 v5。原资产的其它时间、曲线与偏移参数不手工改写。
- `CameraStretchEffectEvaluator` 保留上一帧各组已经选出的半径改变量、原坐标偏移和滚转。新实例首次消费时依据原始 StackingType 捕获这些起点；同一帧创建的实例读同一份历史，不读取本帧遍历中的中间结果。
- 进入期分别从起点到资源目标插值；保持期输出目标；退出期向零插值。明确取消从 RetireStartElapsed 的实际值开始衰减，不把继承段重新当成从零开始的单一包络。
- 位移先在配置坐标内插值，再经既有坐标转换参与通道选择；历史保留被选中的原坐标值。池槽 Reset 清除初始化标记和三项起点；owner Reset 或没有活跃 Stretch 时清除组历史。每帧只增加值类型运算，无新增容器或对象。

半径参考已确认：原函数 `0x178854B0` 读取 TargetCalcData+0x2DC，对应 CameraFollowCalcData.cameraLocateRatio；它不是世界单位的相机距离。原实例把目标／起点换算为 `(ratio+1)/cameraLocateRatio-1`，最终管理器还用 anchorRadius 构造 anchorRadiusAppend，并带两组包络的交叉项。本项目目前没有这些完整中间量，本批继承的是现有 RadiusRatio 增量，尚未把动态比例、交叉项接入正式链。归一化仰角及跟随 Y 额外通道的继承也没有因此视为完成。后续应统一基础轨道到效果阶段的数据流，而不能把 plan.Radius 直接代作无量纲 cameraLocateRatio。

Unity 编译重载完成，Console 错误数为 0；正式 PublishStacking 已保存 18 项 Stretch，回读资源及投影的 StackingType 均为原始值 0，dirtyResources=0，投影 Build/RequireValid 成功（v5）。磁盘与提交前逐项比较，18 项唯一内容变化均为新增 m_StackingType: 0，见 stretch-stacking-audit.json。未运行 Play/replay，未新增测试，未声称手感已通过画面对照。

## 基础半径比例与 Stretch 换算闭合

新增原消费者证据：`0x134DD0C0` 从基础相机状态给 TargetCalcData 写入 anchorRadius（+0x2E4）与 cameraLocateRatio（+0x2DC）；`0x1596C520` 以及其内联消费者 `0x1596B3E0` 明确使用 `(anchorRadiusAppend + anchorRadius) * cameraLocateRatio` 后减去基础半径，计算沿镜头方向的半径改变量。相关代码及分支状态见 radius-consumer 三份文件和 radius-branch-flags.json。与 CameraDataAccessor 的字段元数据和 CameraFollowCalcData 值类型偏移对照后才使用这些偏移，不把搜索到的其它同偏移函数视为相机证据。

本批沿正式数据流增加 CameraLocateRatio：

1. CorinCameraResourcesAuthoring.PublishDefaultOrbit 从已有 Default_Normal 基准来源读取 CAMERA_LOCATE_RADIUSRATIO，通过 CameraFrameOnePointByTrackStage 的正式 setter 保存。当前值为 1；没有更换配置键或默认仰角。
2. 轨道投影携带该比例（v6），FramePlanner 用轨道几何半径乘比例得到帧计划的距离，并把比例显式写进 CameraFramePlan。直接以世界半径构图的阶段使用单位比例；只替换目标、不重新构图的 FrameOneEntity 保留原比例。Sequence 过渡随已有混合进度插值比例，后续 WorldBasicData 修改保留该输入。
3. Stretch 初始化将上帧组内相对改变量转回 `(previousOffset+1)*ratio-1` 保存；零起点为 ratio-1。逐帧将保存起点和资源目标按 `(value+1)/ratio-1` 换算，再执行原进入／退出曲线。
4. 半径选择同时保存该实例的 RadiusEnvelope。进入期为 max(继承包络, StartCurve)，保持期为 1，退出期为 1-EndCurve；它与其它旧通道使用的阶段包络分开表达。显式取消从取消点的半径开始退出，RadiusEnvelope 则按退出曲线更新。
5. 最终半径为 `plan.Radius * (1 + baseOffset + additiveOffset + (1-1/ratio)*baseEnvelope*additiveEnvelope)`，与原管理器附加距离和最终比例相乘的组合一致；没有用相机米制距离代替无量纲比例。

现有生成式相机采样增加 camera-locate-ratio、radius、pivot-location、screen-offset 四个字段，读取同一个 AppliedPlan，没有新增每帧字符串或对象。

Unity 编译重载通过，Console 0 错误；正式 PublishDefaultOrbit 后 Build/RequireValid 成功，v6、ratio=1、dirty=false。磁盘原有全部数值按 float32 比较一致，仅新增比例字段；小数显示及 YAML 换行由 Unity 正式保存产生。未运行 Play/replay，未新增测试。

当前 Default_Normal 比例恰为 1，因此交叉项为 0，基础距离不会因为补齐比例字段而改变；不能把这批数据链修正宣称为已经改变日常转视角手感。仍需对齐归一化仰角及其输入、TopOrbit 延伸、跟随 Delay、额外跟随 Y 通道，以及尚未完成的震动时钟／保持／静默输入。原作当前 Corin 实例是否选择 Default_Normal 的早先证据限制仍然成立。

## 输入轴与归一化仰角消费者证据

本批补齐了此前未定位的输入轴所有者，未修改输入缩放或运行代码。

调用链已确认：NapVirtual3DActionCamera_1.OverrideDragAxisConfig（0x134DE720）→ DFLBCIKIEPE+0x58 的 FEBAFIHIDGP → 该实例 +0x70／+0x98 的两份 VCameraAxisState。原模块从 CameraDataAccessor.Control 的 Vector2（实例 +0x3B0）取得输入，经过 0x11E7E3B0 写入本帧输入，再由 0x11E7CFC0 更新两根轴。写入函数在拖动阶段 raw 2 下另有平滑；后续已通过 OnCameraMoveEnd 写入确认它是 Exiting，不是设备枚举，见下方“拖动退出生命周期”。

Default_Normal 的 DragConfig 原始参数为：

| 轴 | MaxSpeed | AccelTime | DecelTime | InvertInput |
| --- | --- | --- | --- | --- |
| X | 1550 | 0.10000000149 | 0.10000000149 | false |
| Y | 25 | 0.20000000298 | 0.10000000149 | false |

这些数值直接来自既有 Pipeline_Camera_Avatar_Config__1021078955_DFB680A125EE4808.json，不是项目 Sensitivity=(0.12,0.0025) 的来源证明。

VCameraAxisState 的实际步进是 0x143583E0，速度边界是 0x14358770：

- 先按轴类型取得玩家灵敏度倍率，再乘 m_MaxSpeed；两个倍率函数分别为 0x14358050、0x143588B0，按输入模式读取不同配置字段。不能直接将 1550／25 当作鼠标每像素角度／轨道比例。
- 目标速度为 input×有效 MaxSpeed；按目标速度与当前速度的符号、绝对值决定加速或减速。加减速分别使用 AccelTime／DecelTime，之后积分 Value += CurrentSpeed×deltaTime。
- 非循环轴进入两端各 10% 区域时逐步降低速度上限；越界时夹到范围内并把速度设为 0。常量 0.0001 和范围除数 10 已从指令读取。
- 与本地 Cinemachine 2.10.7 AxisState.MaxSpeedUpdate 的核心加减速／边界算法一致，但原函数还加入玩家倍率，并且本地 Cinemachine 在限速后把绝对值低于 Epsilon 的速度清零，当前原函数没有该步骤。不能直接宣称调用本地 AxisState 就完整复刻。
- VCameraAxisState.Value 与 CameraFollowCalcData.anchorElevation 分开保存；原模块用速度增量更新目标数据。0x12A1A790 对仰角按全局上下界夹取；0x12A1A8F0 把 anchorElevation、anchorOverrunElevationRatioDelta、anchorElevationDeltaRatio 相加后再夹取。对应 Default_Normal 的 DRAG_ELEVATION_REGIOIN 为 0～1。

证据包含 main-camera-type、camera-axis-config-types、camera-module-config-type、camera-drag-runtime-types、camera-axis-runtime-type、drag-axis-*、camera-axis-step、camera-axis-speed-limit、camera-axis-horizontal-gain、camera-axis-vertical-gain、calc-elevation-*。camera-axis-branch-flags.json 保留 IFix 与初始化字节，相关 IFix 分支均为 0；非零 0x58... 字节为初始化状态。

当前项目输入由 UnityFixedCharacterInputAdapter.ReadValue 读取 InputAction Vector2，经 TryGetLatchedVector2 直接交给相机；相机读取的是表现输入，不是 ToSimulationValue 中另行归一化的向量。当前消费者使用 Sensitivity 后直接累加角度，没有原作的速度状态。两个现有 CameraInitialState 构造调用均传 pitch=0；它们只恢复录制朝向，不能据此把整个初始状态协议当成归一化仰角协议。

后续实施边界：先对照原 CameraDataAccessor.Control 输入的生产／缩放及玩家倍率，再统一轴参数和归一化轨道消费。不能把未知上游缩放设为 1，也不能套入新的最大速度后用额外系数抵消。ScreenY 已确认仍需检查：本项目 CameraWorldBasicData.Offset 按世界单位参与 CameraToPivot，原数据的 DELAY_ScreenY 是构图参数；当前尚未把原 Delay 到最终构图的消费者完整追通，因此本批未猜测屏幕坐标换算公式。

此批只有证据和文档变化，不需要重新编译或运行。未运行 Play/replay，未新增测试；输入、归一化仰角与 Delay 仍未交付，Goal 保持 active。

## 默认轨道配置正式导入

按用户最新分工，命中链由其他窗口负责，本窗口只处理相机。此次把 PublishDefaultOrbit 从只导入半径比例扩展为导入现有轨道合同能表达的完整基础构图：DEFAULTSPHEREDATA.Orbits、ScreenYTrack、CAMERA_FOV、CAMERA_LOCATE_RADIUSRATIO，以及同一 Default_Normal 下的 ELEVATION_ANGLE。ConfigureOrbit 替换原单字段 setter，唯一调用方同步迁移；未增加备用入口。

原始轨道保持 Top／Middle／Bottom 顺序，ScreenY 保持 Bottom／Middle／Top 顺序，投影编译器继续承担已有顺序转换和控制点生成。资产发布后的唯一数值差异为 ElevationRatio 从 0.5 改成 dump 的 0.6；FOV=50、半径比例=1、三组轨道和屏幕配置未变。FramePlanner 因此改在 0.6 处取基础轨道；输入仍沿当前角度增量链，不把这一配置修正描述为输入轴已完整复刻。

目标 Unity 实例已完成编译与域重载，ConfigureOrbit 已加载，Console 0 错误。正式 PublishDefaultOrbit 后 Build/RequireValid 成功，schema=v6、elevation=0.6、fov=50、ratio=1、sequence dirty=false；磁盘 diff 确认仅一项数值变化。未运行 Play/replay，未新增测试，画面由用户手测。Default_Normal 是否为原作当前可琳实例实际选中的配置，仍保留此前证据限制。

输入倍率调查新增 camera-axis-player-gain-snapshot.json：沿原函数的 RIP 全局地址、泛型 singleton 静态字段及实例 klass，确认倍率来自 JODKHBJPDKE（type 52447），而非 CameraAxis 参数本身。829 快照中 mode==1 的横／纵倍率为 3／2.1，其它分支为 1／0.75。它们只是该快照的玩家状态，不能当作默认配置写进项目；输入模式名称和上游单位仍待确认。

## IgnoreLocalAvatar 与效果计时职责修正

本批追通配置到实例的赋值，确认 IgnoreLocalAvatar 不属于计时参数：

- Zoom：0x14059CD0 的 0x1405A145～0x1405A170，将 ConfigCameraZoom 的 IgnoreOwnerTimeScale(+0x27)、IgnoreWorldTimeScale(+0x25)、IgnoreLocalAvatar(+0x26) 分别写入实例 +0x4B、+0x4E、+0x4D。0x12BB4280 仅取前两项传给 0x13A29830；+0x4D 由资格函数 0x12BB4AA0 读取，为 true 时直接允许，否则检查实体有效性及当前角色资格。
- Stretch：0x15DBE3E0 的 0x15DBE9E9～0x15DBEA24，将同三项配置 +0x4F、+0x3A、+0x2C 写入实例 +0xC8、+0x52、+0x4A。0x17886330 只把前两项传给同一计时倍率函数；0x17885980 使用 +0x4A 控制资格检查。
- 0x13A29830 组合 owner／world 倍率；它没有 LocalAvatarTimeScale 参数。Zoom 更新函数 0x12BB3F80 再把倍率乘本次传入 delta。原作 IgnoreWorldTimeScale 在这里是绕过独立倍率，不能据名称直接等同于读取 Unity unscaledDeltaTime。

完整指令及哈希已保存到 camera-basis-runtime-20260929 下的 zoom-config-copy、zoom-clock、stretch-config-copy、stretch-clock、stretch-eligibility、effect-owner-world-scale；effect-clock-branch-flags 保存快照字节。主要函数 IFix 分支为 0。资格函数读取的 0x536AC6D 为 1，是走实体资格工具的业务分支，不能将所有 0x53 前缀地址都称为 IFix。

项目修正：CameraEffectEvaluationMath.ResolveDelta 删除 ignoreLocalAvatar 参数及其 LocalAvatarTimeScale 乘法；Zoom、Stretch、Override 三个现有调用方同步迁移，没有保留旧重载。配置中的 IgnoreLocalAvatar 字段仍保留其原始值。显式 CameraTimeDomain.LocalAvatarScaled 仍有独立业务用途，本批不删除该时钟域或 FrameInput 的对应输入。

因此计时不会再因 IgnoreLocalAvatar=false 而多乘一次本地角色倍率，也不会因这个资格字段要求存在 LocalAvatarTimeScale。当前 Tick 提供的该倍率为 1，所以不能宣称本批已经改变正常速度下的镜头手感。资格输入、原作独立世界倍率和暂停生产链尚未接通；现有 IgnoreWorldTimeScale 的时钟选择仍需调整，本次只删除有明确证据的错误职责，不能将完整计时复刻标记完成。

Unity 编译与域重载通过，反射回读 ResolveDelta 参数数为 3（两个标志和 FrameInput），Console 0 错误。git diff --check 通过；未运行 Play/replay，未新增或修改测试，没有修改命中链和 IK。

## 基础轨道 ScreenY 的单位和方向

本批继续追到 EJKKEBPOLME+0x58 的 IGOIAKCMOCO（type 90872），它保存 CameraDelayData 并执行构图。此前发现的 OEFNMPPHFFI+0x18 委托是 AACPAEKAACH 衍生效果模块注册的回调；0x171A7DA0 是注册入口，不能仅根据该委托称其为 Cinemachine FramingTransposer。

已经确认的构图链：

1. 0x1596B2B0 从 CameraDataAccessor.TargetCalcData 的仰角（+0x2EC）经轨道管理器取得 ScreenY。
2. 0x1596F9B0 用 DELAY_ScreenX、上述 ScreenY 和 DeadZoneWidth/Height 构造内框；0x159742B0 用相同中心、SoftZoneWidth/Height 和 Bias 构造外框。0x15971F90 在 0x15972281／0x15972291 读取两框交给后续构图。
3. 同一 solver 的 0x15974D90 将归一化矩形换算为相机平面距离：yMin=2*orthoSize*(0.5-screen.yMax)，yMax=2*orthoSize*(0.5-screen.yMin)；X 使用 aspect 和减去 0.5 的坐标。常量 1 与 -0.5 已从二进制读取。该换算与本地 CinemachineFramingTransposer.ScreenToOrtho 一致；透视相机的 orthoSize 为深度乘 tan(FOV/2)。完整方法、类型和分支字节已保存为 delay-screen-*、delay-composition-*、delay-dead-zone-rect；不把这项对应关系扩大为整个 Delay 算法一致。

项目原来将 SampleTrack 的 ScreenY 直接交给 CameraWorldBasicData.Offset.y。该 Offset 参与 CameraToPivot 的世界距离计算，0.5 因而被解释成半米，而不是屏幕中线。CharacterCameraFramePlanner 的轨道分支现改为先取得半径，再用 `(0.5-ScreenY)*2*radius*tan(FOV/2)` 求纵向距离，输出仍进入同一帧计划和相机求解链。没有增加运行分配或备用路径；X 仍使用已有作者合同，可琳当前 X 全为 0。

Unity 编译重载完成，Console 0 错误。编辑器内构建正式 Profile 投影、调用生产 BuildTargetPlan，在零输入／零初始角偏移下取得半径 3.7426796、FOV 50、offsetY=-0.0376972035；将 PivotLocation 按所得相机位置与旋转重新投影，ScreenY=0.5108，与默认仰角 0.6 处的轨道曲线值一致。记录见 track-screen-y-editor-check.json。这个检查只覆盖静态基础构图，不包含 Play/replay、动态阻尼、后续 Zoom/Stretch 或画面对照。

完整 Delay、死区／软区动态求值、输入归一化和效果修改 FOV／距离后的屏幕锚点连续性仍未完成。此次修复不等同于相机整体手感已经还原。

## 鼠标输入单位与符号生产链

本批定位到输入生产者，替代此前只知道 CameraDataAccessor.Control 消费端的证据缺口。新增原始指令、类型和快照存于 pointer-* 与 camera-pointer-input-types.json；不再次推断枚举声明顺序。

- UIInLevelPlayerCameraChildWindowController.OnInputAction（0x164E9210）通过 JODKHBJPDKE 的 0x172587B0 分别取得处理后和原始向量，保存到 +0x328／+0x330。OnBeforeWorldUpdate（0x164EB1F0）将两个向量引用、旋转状态和最后输入设备交给 0x13A4BE70；该函数读取后清空本帧向量。
- 0x158AC130 的跳转表数值 2 指向 +0x28 的 InputAction。829 快照回读该动作名为 InLevelCameraMousePositionDelta，绑定 <Mouse>/delta，动作处理器为 ScaleVector2(x=0.1,y=0.1)，绑定处理器及 override 为空。同对象 +0x18／+0x40 的动作分别为 GampadLeftStick／GamepadRightStick；因此这里的设备来源由实例内容确认。
- 开启处理的该鼠标分支先乘 JODKHBJPDKE+0x1BC，再除以 Time.deltaTime*60。0x158AC5F8 使用槽 0x0540AC98，与先前由 CinemachineBrain 确认的 Time.deltaTime 槽一致；60 位于 RVA 0x02818AA0。
- 世界更新前的 0x13A4BE70 处理 X／Y 反转设置（+0x1C4／+0x19B），取公共倍率 1/60（RVA 0x0282D980）；最后设备 raw 1 另乘 +0x1A8，不能将这个分支仅凭数值命名成某枚举。还会乘 ShootingGroundSubsystem 的瞄准辅助倍率；该子系统不存在时原代码用 1。随后分别乘玩家横向 +0x150 和纵向 +0x19C，交给 0x1229F130。
- JCCGIBAKPEE.0x1229F130 取得 +0xC8 的 CameraDataAccessor，将 X／Y 浮点符号位翻转后写入 +0x3B0 的 pointerDragDelta，并置旋转开始／持续标志。之前按类名过滤把 JCCGIBAKPEE 当作无关类会漏掉这条链；实际持有字段和写入指令才是依据。
- FEBAFIHIDGP 的 0x11E7CDF0 直接读取 pointerDragDelta；0x11E7CFC0 再以减号应用横向轴速度。两个负号不能被错误合并成项目鼠标应倒转。CameraControlFlag.get_pointerDragDeltaMultiPlatform（0x167EBD20）另有 +0x1B0 倍率，但基础拖动路径不经过该 getter，不能重复套用。

快照玩家参数为 +0x150=5、+0x19C=3.5、+0x1BC=0.6、+0x1A8=1.6；轴的非 raw-1 倍率为横向 1、纵向 0.75。它们作为 829 玩家状态保存，未宣称是出厂默认值。以无反转、非 raw-1 设备分支、无瞄准辅助为例，鼠标像素增量到轴前输入的幅值系数是 `0.1 * 0.6 / (deltaTime*60) * (1/60) * (5,3.5)`；之后仍须执行轴最大速度、加减速及轨道仰角规则，不能直接用该式替换最终角度。

项目当前实际资产也已回读：Corin Profile 的 Sensitivity 为 (0.12,0.12)，不是 CameraInputSettings 类初始化器里的 (0.12,0.0025)。Look 动作共用 <Gamepad>/rightStick、<Pointer>/delta 和 Joystick；Pointer 绑定带 InvertVector2(invertX=false,invertY=true)。现有相机只取得合并后的 Vector2，没有设备来源。下一步需沿正式表现输入携带设备类别，统一处理鼠标 delta 与摇杆速率，再接轴状态及归一化仰角；不能先对混合输入统一除以 deltaTime 或重复反转 Y。

本批只提交新的输入证据与复刻文档，未改运行代码或配置，未编译、Play 或 replay。输入轴交付仍未完成；已发布的 ScreenY 修正不受本批影响。

## 输入轴配置与归一化轨道交付

本次将已经取证的输入参数接入正式 Profile 和相机求值链，没有修改命中生产链、IK 或运行 replay。

1. UnityFixedCharacterInputAdapter 在读取 Look 的同时保存 PointerDelta／Stick 来源，经 ICharacterPresentationLookInput、CharacterCameraDomainRuntime、CameraFrameInput 交给 FramePlanner。删除 Pointer 绑定上的 Y 反转，由相机统一处理轴方向；没有保留旧 Sensitivity 路径。
2. CorinCameraResourcesAuthoring.PublishInput 从 Default_Normal 读取 MaxSpeed=(1550,25)、Accel=(0.1,0.2)、Decel=(0.1,0.1)、仰角范围=(0,1)；从已保存的829玩家快照导入鼠标／手柄倍率。玩家 +0x1c4／+0x19b 的实读字节均为00，已与配置反转合并。原始指令中来源flags4对应设备2、flags2对应设备1，分别由鼠标与GamepadRightStick生产；未按枚举声明顺序取值。
3. 鼠标增量按原作 Time.deltaTime 对应的 ScaledDeltaSeconds 归一化，手柄按速率处理；轴积分仍按正式 PresentationDeltaSeconds 推进，暂停或任一所需时间为0时不推进。实时模式两种时间相同，表现调速模式保持输入单位与推进时间分开。
4. CameraAxisRuntime 保存轴值和速度，执行已取证的加减速、范围末端10%限速及越界停止。轨道以归一化仰角采样，其他构图继续取得轨道几何角变化量。现有 Profile.RequireDefaultTrack 已要求默认 Sequence 恰有一个轨道阶段，本批未缩窄合法作者配置或新增兜底。
5. 投影版本更新到v7，全部输入字段参与Profile Revision。正式发布后资产新增上述配置，移除Sensitivity；调用Build/RequireValid成功，最终dirty=false。没有新增热路径对象分配；本批未做分配测量。

Unity编译、域重载和Console检查通过，0错误。编译中发现初始化Apply调用漏传输入类别，已补为None并重新编译。编辑器内调用生产ResolveLook检查鼠标、手柄、暂停、零时间、上下边界和松开后的减速，结果保存在input-axis-editor-check.json。1秒松开后的单帧角度增量约0.0000229°，保留原作没有微小速度清零的行为。此检查不是画面验收。

性能工具现有正式camera trace生产者只生成全零固定镜头输入，按PointerDelta交入仍为零；本批没有为历史手工非零trace证明设备来源或兼容性，也没有运行它们。玩家倍率是829快照状态，不宣称为出厂默认；Default_Normal是否为原作当前Corin实例实际选择的键仍未证明。

本批当时的未完成项：拖动退出生命周期（后续已补齐，见下文）、TopOrbit延伸、完整Delay及死区／软区跟随、Zoom/Stretch后屏幕锚点连续性、独立世界倍率和震动保持／静默输入。当前交付可供输入与轨道手测，不代表全相机手感已完全复刻，Goal保持active。

## 拖动退出生命周期

此前把0x11E7E3B0的raw 2分支称作“鼠标预平滑”不准确。FEBAFIHIDGP+0x100是APHELGAKDFI拖动阶段，与输入设备raw值无关；不能给全部鼠标输入常开一个平滑器。

本次追通的正式链：

- 0x13A4BE70读取当前处理后输入。两轴精确为0时结束先前控制；开始控制要求至少一轴绝对值大于0.025，逐轴不超过阈值的输入清零。阈值来自0x0283943C。
- 结束通知0x101D1BE0调用具名NapVirtual3DActionCamera_1.OnCameraMoveEnd（0x134E0A90），再经DFLBCIKIEPE.OJMDOHLGDMA（0x1A284EC0）把阶段写为2，并将CameraScreenDragConfig+0x30的DRAG_TO_EXIT_DURATION赋给BCFKAGODFJI+0x40。Default_Normal的值为0.1。
- 开始通知0x101D8D20调用OnCameraMoveStart（0x134E0C50）。模块仍活动时切回Controlling，保留速度；已经关闭时由0x11E7CB00重新打开并清两轴速度，然后使用当前角度／仰角作为轴值。
- 退出期间，先计算`deltaInput=currentInput-previousInput`，调用0x1F94B2B0／0x1F949C40，再保存`currentInput-Damp(deltaInput,duration,frameDelta)`。该Damp与本地Cinemachine.Utility.Damper一致：常量4.605170249938965、epsilon0.0001；原作稳定阻尼开关0x05368A80在829快照为0，本项目也未启用CINEMACHINE_EXPERIMENTAL_DAMPING。直接使用现有Cinemachine函数。
- 轴求值完成后，退出计时用Time.deltaTime槽0x0540AC98推进；0x14924E00在elapsed严格大于duration时完成。0x11E7D7EE经虚表+0x1A0调用0x11E7CA30关闭拖动模块。没有把该计时误写成无限渐近减速。

项目输入配置正式增加逐设备／逐轴的激活阈值及DragExitDuration，阈值按已经导入的玩家倍率换算到轴前输入单位。FramePlanner保持Inactive／Controlling／Exiting三个业务阶段；持续输入直接送轴，只有退出阶段消费Cinemachine阻尼，退出完成停止推进，完整停止后再次开始清速度。暂停保留阶段与计时。没有增加热路径对象分配或重复执行链。

正式PublishInput及Build/RequireValid成功，投影v8，最终dirty=false；Unity编译、域重载通过，Console 0错误。编辑器内生产ResolveLook检查显示：持续60帧10像素输入仍得到71.04179°，与v7相同；释放后第7次60Hz更新转为Inactive，额外60帧角度增量为0；重新开始首帧0.215278625°，对应从零速度起步；0.01像素小输入60帧不启动，退出中暂停不推进。记录见drag-exit-editor-check.json。没有Play/replay、没有新增测试代码，不把这些检查当作画面完全匹配。

原始证据包括drag-notify、drag-trigger、drag-manager-mode、drag-exit-progress、pointer-smoothing-mode、pointer-smoothing-scalar/vector及pointer-smoothing-snapshot。BCFKAGODFJI另有共享配置表的调用者，相关getter／producer证据只用于追溯调查，当前拖动参数依据是已证明的CameraScreenDragConfig字段，不是旁支配置表或玩家设置猜测。选定IFix字节均为0。

剩余工作仍包括TopOrbit延伸、完整Delay／死区／软区、效果改变FOV或距离后的构图连续性，以及震动的独立世界倍率和保持／静默输入。此次完成拖动退出，不代表完整相机复刻完成。

## Zoom／Stretch 改变镜头参数后的构图换算

当前基础轨道已经把归一化ScreenY换算为CameraWorldBasicData.Offset的米制值。旧Zoom只改FieldOfView、Stretch只改Radius，因此后续仍消费旧镜头下的米制偏移，原来的屏幕锚点随焦距／距离发生额外漂移。这与技能配置中主动给出的CamOffset平移是两件事。

新增证据确认：原作CameraState.<Lens>k__BackingField与LensSettings.FieldOfView的值类型payload偏移均为0；0x15970770在0x15970817读取传入CameraState的当前FOV，0x15970A70起乘0.5和Deg2Rad，再经0xE58590与目标深度组合。该数学函数的小角分支为x+x³/3；配合其透视构图调用，与本地CinemachineFramingTransposer的`tan(FOV/2)*depth`及原ScreenToOrtho换算一致。证据见composition-lens-fields、composition-perspective-math与composition-body。另行保存的composition-projection-after-lens是旋转构图路径，不把它误称为平移构图函数。本批确认当前镜头参数参与构图的单位关系，没有据此宣称完整Delay分支和执行顺序已复刻。

修改后的正式链：Zoom计算最终FOV、Stretch计算最终半径和配置位移 → CameraWorldBasicData.WithFraming将Offset按`newRadius*tan(newFov/2)/(oldRadius*tan(oldFov/2))`换算 → 原有震动、Shot和最终输出继续消费同一WorldBasicData。半径和FOV的修改由同一个入口保持屏幕锚点，没有新增屏幕位置缓存或第二套配置。特殊构造函数沿用已经归一化的旋转，对新FOV执行原有范围处理一次；运行路径没有新增托管分配。

删除已迁移且没有其他源码／反射字符串调用的WithRadius、WithFieldOfView方法及FramePlan无职责包装。CameraWorldBasicData为普通不可变结构体，不是序列化行为组件；作者资产没有调用这些方法的外部绑定。主动位置偏移仍由Stretch的原有PivotLocation计算提供。

独立数值检查以先前生产基础构图ScreenY=0.5108为输入：E Hold的FOV52、半径1.1倍下，旧算法约0.509387，修正后0.5108；E Explode的FOV60下，旧算法约0.508723，修正后0.5108。记录见zoom-stretch-framing-numeric-check.json。这是独立投影计算，不是新C#的运行证据。

Unity编译尝试被其他窗口的OperationStateMachineRuntime.cs:267两处CS1503（int参数传给ulong）阻塞；未修改该逻辑链。相机改动diff检查通过，当前不能宣称新代码已通过Unity编译或生产函数检查，也未运行Play/replay。待整体编译恢复后，需用正式Profile的Branch_02 Zoom／Stretch跑编辑器内生产求值检查，再交付这一批为可用版本。配置资源数值未改，不需要重新生成技能、动画或IK资产。

补充独立编译：使用Unity生成的csproj，`dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false`编译ThirdPersonCamera.Contracts成功，0警告0错误。相同方式编译ThirdPersonClient.Runtime时，由CharacterTimelineHost.cs:323引用的既有依赖输出不含14参数AbilityTimelineLogicMotion构造函数而失败；这属于独立编译采用现存依赖产物的版本不匹配，不能将其直接认定为该模块新增源码错误。两次构建结束均立即执行build-server shutdown。相机合同类型已通过C#编译，Zoom／Stretch完整消费链仍待Unity整体编译恢复后验证。

## Delay 模式、仰角与方向消费的进一步取证

本批没有修改相机运行源码或作者配置。当前统一 SmoothDamp(DefaultSmoothTime) 的实现仍未替换，完整 Delay 交付仍未完成。新增证据统一为 camera-basis-runtime-20260929/follow-delay-*，包括原始指令、字段布局、常量字节及配置摘录。

输入到输出已经确认以下层次：

1. 镜头控制标志、角色 tag 与模式队列进入 0x14A3BF30。配置 +0x58/+0x59 决定是否自动选模式及是否考虑动画状态；有队列项时出队，没有项时读取配置 +0x5C。0x14A3C400 使用主相机 +0x27C 的 CameraDelayMoveMode 在配置 +0x60 的 DELAYDATAS 字典查找。字典键不是 FOV 分档。枚举字段的 constant_value_status 仍为“默认值表尚未展开”，不能按声明顺序给 raw 0/2/3 等值取具名含义；角色 tag 的具体映射也尚未确认。
2. 状态混合之后，0x13C57C20 按归一化仰角选底→中或中→顶。半区坐标分别为 2*elevation、2*(elevation-0.5)。0x11919390 把 CameraScreenDragConfig.NapCamOrbitLerpTime(+0x34) 保存为分母，0x11919A70 以半区坐标除该分母后调用曲线，再交给 CameraDelayData.Lerp。0x1EA804F0 的工厂指令创建 (0,0)→(1,1) 且有效段切线为1的线性曲线。当前 Default_Normal 的 NapCamOrbitLerpTime=1，因此当前配置的仰角混合确实等同于分段线性；不能把该分母误称为每帧推进的时间平滑，也不能忽略它对其他配置的影响。
3. 0x116CB660 对 FollowOffset、XYZ damping、旋转 damping、屏幕中心、死区／软区与 bias 执行夹紧到[0,1]的插值，并通过 0x134D4390 混合方向倍率。动画状态／tag倍率对象在未替换分支直接采用目标端对象，不逐条插值；这一选择还需在状态消费者端进一步核实。本批保存了所读分支字节，初始化标志、IFix分支和业务开关不能混称。
4. 0x15971550 把 CameraDataAccessor+0x254 的向量送入 0x12CB0FE0；后者用循环槽保存向量、长度和传入时间，并维护向量总和。0x15974F90 以槽数取得平均向量后归一化，所以方向输入存在历史窗口，不能直接使用当前帧方向。窗口长度及 +0x254 上游生产仍待追通。
5. 0x15971550 读取前次 CameraState 的旋转，将其前向取反并投影到水平面，与上述历史方向求角。原始常量确认角度参数为 1-abs(angle)/180。0x13C56A40 用方向配置生成五个点：(0,1.35,0)、(0,1.35,forward)、(side,1.35,0)、(0,1.35,-backward)、(0,1.35,0)，调用 0x1F39A430 生成控制点。0x15970400 选择中间两段执行三次贝塞尔，0x15971550 最后取采样点 XZ 长度作为系数。不能将这些数值实现为方向枚举的三个硬切倍率，也不能仅凭函数形状声称 0x1F39A430 已与某版本 Cinemachine 函数逐项比对完成。
6. 0x15971D60 先把 Accessor+0x140 的当前FOV写入CameraState，再计算构图阻尼；它读取 CameraDelayData 的 HorizontalDamping(+0x48) 与 VerticalDamping(+0x4C)。其中垂直值还会受方向系数和 +0x224 的额外纵向系数影响，后者由 0x159717D0 的升降速度、计时及曲线分支产生。因此配置 mFOV 不能直接代替这里读取的当前FOV，构图阻尼也不能统一写成一个 SmoothDamp 时间。

配置数值保存为 follow-delay-config-summary.json，带原文件SHA256、键名、9个模式和27个仰角条目。纠正先前概括：并非各模式XYZ和构图阻尼均为0.5。中轨道 raw 2/11 的XYZ为(0,0.5,1)，raw 4 的水平／纵向构图阻尼为(0.5,3)；其他列出的中轨道XYZ为(0.5,0.5,0.5)。顶部／底部也保留独立条目，不能复制中轨道数值。当前 Default_Normal 是否是原作可琳实例实际选用键，仍未证明。

本批检查连接时8080无监听，使用已安装正式server CLI恢复服务，随后同一 e852139597e42532 实例重新注册；没有重启Editor、改变全局实例或运行Play。实例回读项目路径正确、compiling=false、playing=false。Console仍存在OperationStateMachineRuntime.cs:267两处int→ulong编译错误，已加载程序集列表没有相机程序集，WithFraming反射检查为false。因此上一批Zoom／Stretch修正仍未完成生产求值验证，不能当作新可测版本。没有因这些外部错误修改逻辑模块；本批只做证据／文档检查，不新增测试、不运行replay。

下一实施所需缺口已缩小为：模式枚举原始值与角色tag的对应、方向历史窗口长度与向量生产、状态过渡曲线选择与稳定时间、方向控制点算法逐项对应、跟随／构图阻尼最终消费。必须沿现有Profile→投影→SequenceEvaluator正式链整体接入，不能先把0.15机械替换为0.5或增加未被消费的备用配置。

## Delay 的正式作者数据链（待统一生成资产）

本批按“其他窗口编译受阻时先完成C# authoring，之后统一生成”的授权修改生产源码：CorinCameraResourcesAuthoring.Publish加入PublishDelay；它从同一Default_Normal读取9个模式的27组轨道参数、9条模式过渡及升降阻尼。CameraDelaySettings作为Profile的唯一Delay配置，CharacterCameraProjectionPayload携带同一配置，投影版本更新到v9。没有另写运行时JSON读取、备用配置或硬编码的模式名称。

数据覆盖：自动模式选择／动画相关开关、默认raw模式、速度平滑时间、仰角插值分母、每个模式的FOV与最小距离比例、上中下的跟随位置／旋转阻尼与方向倍率、构图阻尼／屏幕中心／死区／软区／bias、状态与tag倍率表、From／To／Style原始值、过渡时间／稳定时间／曲线，以及向上／向下的速度阈值、阻尼倍率、时间和曲线。当前所有动画状态／tag倍率表为空，仍按原始空表导入。曲线保留时间、值、入／出切线、权重、WeightedMode和wrap；m_TangentMode是编辑器切线编辑元信息，求值使用已导入的数值切线。未把模式编号按枚举字段顺序命名。

Profile.Revision加入Delay的Unity序列化正文，缺失Delay在原有Profile完整性边界报错；不在内部逐层增加校验。DefaultSmoothTime沿原Profile字段保存，导入使用同源DEFAULT_SMOOTH_TIME，没有复制第二份同义值。当前数据只进入作者与投影，SequenceEvaluator尚未消费它替换现有SmoothDamp；不得将此次配置链改动写成动态跟随已复刻。现存磁盘Profile尚未PublishDelay，正式投影仍待整体编译恢复后统一生成v9，源码保持在途，不把依赖未生成资产的中间状态提交为可用交付。

检查：定向diff --check通过。Unity刷新编译仍被OperationStateMachineRuntime.cs:267的两处int→ulong错误阻塞。普通dotnet独立构建先遇到缺少project.assets.json，恢复后仍缺失Temp/bin/Debug依赖产物；每次结束均立即关闭build-server。随后使用当前Unity生成的正式编译响应文件和同版本Roslyn，仅将输出重定向到Temp，完整相机合同程序集编译成功。完整Content.Editor编译仍缺少ThirdPersonCharacter.Animation.ref.dll；将检查范围明确缩到本次生产CorinCameraResourcesAuthoring.cs，并使用真实Unity/Editor/Newtonsoft引用与本次合同输出，编译成功。临时编译产物没有装入Editor，不能替代Unity整体编译、正式资产发布、序列化回读或画面验证。证据为follow-delay-authoring-compile-check.json。

方向历史窗口进一步确认：0x13C563A0在0x13C5673A传入3构造+0x58历史对象；0x12CB09E0在0x12CB0A32把该参数写入容量+0x30，覆盖初始化器中的20。结合已有更新函数的循环槽取模与平均方向读取，当前solver使用最近3次更新的向量窗口，不是默认20次，也不是按秒滑动窗口。Accessor+0x254的生产和模式／tag映射仍未闭合。

## Delay 输入生产边界修正：朝向、上一帧控制与状态 tag

本批继续追生产端，修正前文“历史移动方向”的过早概括。CameraDataAccessor.AvatarInfoData位于+0x230，其值类型NEGKDBJNJJL的HNPCBFOEDNP在payload+0x24，正好对应solver读取的Accessor+0x254。不能仅从direction参数名判断它是速度。

JCCGIBAKPEE.NFOMHELEAFH（0x101D4180）接收CameraAvatarPrepareData与NEGKDBJNJJL引用，在0x101D4324～0x101D432D写入这个向量。普通模型分支调用LMEACFFOIMO.EONJLJBFBOF（0x15023480）：从模型+0x1D0取得Transform，读取rotation，再旋转(0,0,1)。该向量常量在829快照RVA0x5363108已读取为00000000/00000000/0000803f；内部调用槽RVA0x540C048与具名UnityEngine.Transform.get_rotation_Injected的尾跳槽一致。因此普通模型分支的输入是模型世界前向，经最近3次更新的平均／归一化后进入方向曲线，不能用BodyPosition差分或输入MoveAxis替换。当前CameraFrameInput已有BodyRotation，但是否与原作模型Transform完全对应仍须沿装配核对。

模型+0xB8非空的另一分支从其+0x10对象取+0xC8向量，当前元数据字段声明为CLJINMONOIB；未证明当前Corin实例选择此分支或此向量的生产，所以不把普通模型结论扩大为全部角色／控制器。通过函数地址搜索元数据可能命中共享代码的另一方法所有者，不能以第一次地址匹配的类型名就给这一分支定性。选定生产函数与IFix分支字节保存为follow-delay-input-branch-bytes，829所读值均为0。

自动模式选择的两组tag已读出：0x14A3BF30通过RVA0x5360730根及+0x2F480静态存储读取+0xC0和+0xC8；它们是List<string>，分别为["Idle"]和["Move","Evade"]，不是枚举数组。0x13A0F700逐个查询，命中任一条即返回true；0x1777AF70从角色BaseData的字符串计数表读取计数并要求大于0。因此后续应对接角色正式tag事实，不能用动画片段显示名或Pose节点名代替。证据为follow-delay-mode-tag-snapshot、follow-delay-mode-0x13a0f700及follow-delay-tag-predicate。

另一个关键字段修正：Accessor+0x3A8是Control，+0x3BC是LastControl，两者都是CameraControlFlag；+0x3BC处的bool是上一帧isRotateStart，绝非当前isRecoveringFromLock（后者在Control+0x10，即+0x3B8）。开启动画相关自动选择时，raw 2入队分支检查当前isRotateStart为false且LastControl.isRotateStart为true，并结合Idle，或带移动控制的Move/Evade tag。raw 3分支包含当前转镜控制及移动开始条件。这里只按原始编号记录触发证据，未按枚举声明顺序推断正式枚举名。控制开关0xF128030还依赖0x101D1D40的全局相机可控条件；该条件含其他状态，需要继续核对，不能无证据常置true。

本批新证据决定了后续实现输入：朝向历史、前后帧转镜控制、角色tag事实三者分开；不能再用“是否正在位移”一个bool覆盖。上一批Delay C# authoring与v9投影仍为未生成资产的在途修改，本批没有更改其源码、运行Play/replay或重编动画／IK。尚未完成动态Delay消费，也没有新的可测版本。
