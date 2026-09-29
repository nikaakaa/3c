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

调用链已确认：NapVirtual3DActionCamera_1.OverrideDragAxisConfig（0x134DE720）→ DFLBCIKIEPE+0x58 的 FEBAFIHIDGP → 该实例 +0x70／+0x98 的两份 VCameraAxisState。原模块从 CameraDataAccessor.Control 的 Vector2（实例 +0x3B0）取得输入，经过 0x11E7E3B0 写入本帧输入，再由 0x11E7CFC0 更新两根轴。写入函数在输入模式 raw 2 下另有平滑，尚未把该值根据枚举声明顺序擅自命名为某设备模式。

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
