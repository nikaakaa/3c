# 可琳相机：震动时钟与升降分支核对

## 本轮结论

普通可琳不应启用原作的邦布变身升降阻尼曲线。原实施记录把这组曲线列为普通可琳跟随必补项，遗漏了消费者的玩法资格，现以本记录纠正。相机完整复刻仍未完成。

震动求值已消费正式投影中的 `MuteCameraShakeAdvancedProcess`：调用方表现时间为正时，配置为 true 使用调用方时间，为 false 使用 Unity 缩放时间；调用方时间为零时不推进累计时间。此前始终使用缩放时间，遗漏了该开关与零时间分支。

## 升降消费者的实际资格

`0x159717D0` 在 `0x15971828` 以空 owner 调用 `0x17268130`，检查返回对象 `+0x76`。解析方法槽 `0x55F4EE0` 的泛型上下文，类型参数是 `BangbooGameSubsystem`；该类型 `+0x76` 的正式字段为 `<isInHenshinBuddyStatus>k__BackingField`。

资格不满足时跳转 `0x15971BF4`，清状态编号并把当前额外阻尼倍率写为 1，返回 1。当前可琳工程没有该邦布变身玩法，因此现有普通跟随路径使用额外倍率 1 符合这一分支。本轮没有创建虚假的邦布状态、角色速度输入或无调用的升降计时器。

资格满足后，速度来源也已明确：`0x101D43BA` 调用槽 `0x540EC58`，与具名 `UnityEngine.Rigidbody.get_velocity`（`0x1EDA9820`）相同；结果写入 `AvatarInfoData.payload+0x48`，Y 对应 Accessor `+0x27C`。不能使用镜头自身速度替代。

`0x156231A0` 先推进 elapsed，再用 `clamp01(elapsed/duration)` 采样曲线，在状态切换时从当前倍率开始插值。下降使用 Drop 参数，上升使用 Up 参数；阈值内分支也取 Up 参数，仅计时器独立，不能猜成固定回到 1。时长非正时返回 1。配置数据保留原始数值，但普通可琳不执行这组特殊玩法公式。

证据：[资格、元数据及哈希](camera-basis-runtime-20260929/follow-delay-vertical-eligibility-evidence.json)。泛型 getter 与计时器的替换分支在 829 快照中均为 0。结论仅针对既定二进制与快照。

## 震动时钟改动

输入沿 `GameplayPresentationFrameContext → CharacterCameraDomainRuntime → CameraFrameInput` 进入 `CameraShakeEffectEvaluator.ResolveDelta`；正式 Profile 的开关已由 `PublishInput` 导入并复制进 Projection，本轮补其运行消费。

原作 `0x174DD3C0` 在 `0x174DD405` 比较调用方 delta；正时间路径在 `0x174DD43A` 读取配置 `+0xBE`。开关为 false 时改读槽 `0x0540AC98`（既有 Brain 消费证据对应 `Time.deltaTime`），为 true 时保留调用者 delta。零时间路径在 `0x174DD465` 将 delta 置零。源码继续使用现有输入，没有新的时钟数据源，也没有每帧分配。

这只完成宿主时间选择。原作实例独立倍率、非正时间下切离 Action3DCamera 的取消条件、全局震动资格、区域渐静默与 Base 保持仍未闭合，不把本次改动称为完整震动时钟复刻。当前 Default_Normal 的开关为 false，因此正常正时间播放的节奏不会因本次修正而改变。

## 检查

目标 `e852139597e42532` 的项目路径已核对，Unity 在 Edit 模式完成脚本编译和域重载。直接调用生产 `ResolveDelta`，六组输入覆盖两种开关、调用方零时间及 Unity 缩放时间为零的情况，结果均符合原作选择分支。

同时补做此前缺少的 `CameraWorldBasicData.WithFraming` 生产函数检查：四种半径比例乘五种 FOV，共二十组；屏幕偏移除以投影尺度的最大误差 `7.67988251e-9`，Pivot 与旋转保持。该检查仅证明这个函数在变更半径/FOV时保持既有屏幕构图，不证明完整动态构图顺序或实机画面。

证据：[生产函数检查](camera-basis-runtime-20260929/shake-clock-and-framing-editor-check.json)。检查使用 Profile 副本并销毁副本，没有保存原 Profile；原 Profile 当时为 dirty，保留其未保存状态。没有运行 Play/replay，没有新增测试源码，没有修改 IK、命中链或其他窗口源码。

## 震动实例倍率接入 owner 时钟

2026-09-29，继续补齐原作 `DHDAMGEBBHN` 时钟链的证据并接入当前正式输入。方法表确认 `OCKAOBKPAFD(0x138FB990)` 依次调用 `PFNMGOJBLBD(0x13900C70)`、`JIEIOFHFFKM(0x138FE170)`、`JOEKDCBNEAM(0x138FFE50)`，最终把聚合器输出乘 `+0x94` 写入 `+0x7C`。三个新函数正文与调用关系保存在 [clock-aggregator-native](camera-shake-runtime-20260928/clock-aggregator-native/native_evidence.json)。

当前实现仍不假设聚合器内部每一项都对应本项目某个具体系统。本批只把共享时钟已有的 `CameraFrameInput.OwnerTimeScale` 作为独立倍率正式输入：宿主先按 `MuteCameraShakeAdvancedProcess` 选择调用者或 scaled delta；实例 `IngoreTimeScale=true` 绕过倍率；为 false 时乘 owner 倍率，倍率恰好为 0 时替换为 `0.0001`，不实现 `Max(scale, 0.0001)`。

`GameplayTickSystem.PresentFrame` 当前把 `OwnerTimeScale` 固定传 1，因此本次接线不改变普通共享时钟下的震动节奏；它闭合的是输入通道和实例分支。聚合器内部各项业务来源、全局静默权重、区域渐静默、Base 保持计数触发和非正时间切离 `Action3DCamera` 仍未完成。

检查：目标实例 `e852139597e42532` Edit 模式刷新并完成域重载，Console 0 错误。`git diff --check` 通过。未运行 Play/replay，未新增测试；没有采集 GC 或实机手感。

## 静态震动资格消费

继续核对 `NHEFILHBNND.IDLJBLBBIOH(0x174DE6A0)`：先取得当前模块配置；配置缺失或 `CameraDataAccessor+0x188` 非零时允许执行；`MUTE_CAMERA_SHAKE(+0xBD)` 为 true 时返回 false。该结果是执行资格，不是区域渐静默权重。宿主权重函数 `LJMHOGFLLIC(0x174DDC50)` 另行调用主相机静默读取 `0x134E1E60`，两条输入不能合并。本轮新增宿主 `LJMHOGFLLIC`、`CIFJEEEEMGD`、`NOPJIAOCHID` 的完整反汇编和调用表，保存在 [host-manager-native](camera-shake-runtime-20260928/host-manager-native/native_evidence.json)。

当前项目在 `CameraEffectEvaluator.AddRequests` 的请求接纳边界消费 Projection 的 `MuteCameraShake`：配置为 true 时 Shake 请求不进入效果状态池。没有在最终输出上清零，也没有把静态配置与运行期区域静默混用。当前可琳 Profile 的原值为 false，因此本批不改变现有画面节奏。

检查：目标实例 `e852139597e42532` Edit 模式编译并完成域重载，Console 0 错误；`git diff --check` 通过。未运行 Play/replay，未新增测试。区域静默权重、Base 保持计数、聚合器内部业务项仍待后续补齐。

## Action3DCamera 取消条件溯源

2026-09-29，继续追 `host-MEIBNMFIBPN` 的非正时间分支。`JCCGIBAKPEE.PHMMFHJKDBP(+0x120)` 是字符串状态；取消判断先读取单例 `0x55B3CE8` 指向的模块对象，再比较该字段与缓存 `0x57EEA30` 的 `Action3DCamera`。不相等时调用 `0x174DDAB0(-1)` 取消全部实例；相等或对象缺失时不进入取消。因此这不是“暂停或 delta 非正就清理”，而是离开特定相机状态的正式条件。

状态生产者已核对三处：构造函数 `0x101D0330` 和重置函数 `0x101DB760` 都把 `+0x120` 初始化为同一 `Action3DCamera` 字面量；相机序列接纳函数 `MPMCEDBCNOJ(0x122A0AC0)` 在通过目标、优先级和接纳模式检查后，经 `LNGEJKLBKCD(0x122A0CE0)` 也写回 `Action3DCamera`。完整字段表与函数正文保存在 `camera-module-state-type.json`、`camera-state-.ctor-101d0330.json`、`camera-state-HBMLDILJCKM-101db760.json`、`camera-state-LNGEJKLBKCD-122a0ce0.json` 和 `camera-state-reset-caller-0x122a0ac0.json`。

当前 `3C_Client` 相机运行时没有独立的相机状态生产者，也没有可证明等价于 `PHMMFHJKDBP` 的“离开 Action3DCamera”输入；`CameraMode.ActionFocus` 只是名字相近，不能映射。同时当前 Profile 的 `MuteCameraShakeAdvancedProcess=false`，即使接入状态通道也不会进入该分支。本轮不伪造状态字段或把默认相机临时当作 false。

## 时钟聚合器与 Base 保持来源

2026-09-29，继续补齐 `DHDAMGEBBHN` 的上游业务边界。`OCKAOBKPAFD` 的三项前置不是同一类输入：`PFNMGOJBLBD` 更新 `+0x18` 的 `BMKABHIKIFC(OGHJFLEGIGM<float>)` 共享时间并维护 `+0x70`；`JIEIOFHFFKM` 消费 `+0x50` 的 `BCFKAGODFJI`，`+0x39` 为 true 时直接返回，`+0x31` 为 true 时把 delta 乘 `+0xE8` 交给保持计时；`JOEKDCBNEAM` 消费 `+0x48` 的 `ConfigEntityTimeSlowBase` 并在 BattlePhoto 慢速生命周期内推进实体时间。最终输出仍取共享时间对象并乘 `+0x94` 写入 `+0x7C`。完整布局保存在 [聚合器类型](camera-shake-runtime-20260928/clock-aggregator-owner-type.json)、[共享时间类型](camera-shake-runtime-20260928/clock-shared-time-value-type.json) 和 [依赖类型](camera-shake-runtime-20260928/clock-aggregator-dependency-types.json)。

`JIEIOFHFFKM` 的生产者进一步确认为关卡结算演出。`COJGPJAPOBA.LNBPCKHKNKK` 在进入和退出两个分支分别调用保持入口 true／false；该类型方法消费 `ConfigLevelEndCameraEffect` 和 `LevelEndPerformType`，完整签名保存在 [所有者类型](camera-shake-runtime-20260928/base-keeper-owner-type.json)。当前 `3C_Client` 没有关卡结算演出或等价的 `BCFKAGODFJI +0x31/+0x39` 状态生产者，因此 Base 保持计数不能接到普通攻击、E Hold 或相机事件。

BattlePhoto 分支同样不属于当前可琳战斗链：`BattlePhotoSubsystem.StartHoldTimeSlow` 和 `EndHoldTimeSlow` 调用聚合器，前者的原生调用关系保存于 [photo 证据](camera-shake-runtime-20260928/clock-photo-hold-native/native_evidence.json)；当前工程搜索未发现 TimeSlow 或 HoldTimeSlow 正式系统。因此本轮只闭合证据边界，不为缺失系统增加共享时钟的第二条配置路径。当前 `OwnerTimeScale` 仍是唯一已接入的正式倍率输入，`GameplayTickSystem.PresentFrame` 固定传 1。
