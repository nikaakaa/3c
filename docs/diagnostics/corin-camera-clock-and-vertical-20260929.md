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
