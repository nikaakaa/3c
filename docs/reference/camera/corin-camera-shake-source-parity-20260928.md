# 可琳摄像机震动复刻依据

日期：2026-09-28。状态：已追通指定版本的配置读取、战斗创建、实例求值、管理器仲裁、取消及 Cinemachine 写回；补充了 829 快照中实际加载的资源和核心 IFix 开关。时钟入口有原生分支与本地源码交叉证据。尚存的上游业务和证据边界见第十节，不能将本文称为全游戏摄像机的完全还原。本轮只编写复刻依据，不修改游戏运行代码或作者资产。

## 一、结论

当前项目的震动不能称为按原逻辑复刻。差异已经不只是“可能波形不同”：原战斗实例使用余弦半幅值、固定通道相位、独立方向噪声，以及真实距离驱动的自定义空间曲线；当前实现使用满幅正弦、请求身份散列相位，把方向噪声与 NoiseRatio 绑定，并没有消费空间曲线。

用户指出 Cinemachine 自带功能是有效线索。dump 同时存在 Cinemachine 类型和游戏自有 VNoise 类型。实际已追通的战斗链是 `ConfigCameraShake → AONHPMMKECA → CameraDataAccessor.Shake`，其中使用 `VNoiseParams`，不是仅凭库里存在 `CinemachineImpulseSource` 就认定直接播放原生 Impulse。

## 二、来源与可重复核对入口

- 游戏二进制：`D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/GameAssembly.dll`。
- 本轮已重新核对 SHA-256：`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`，与现有 829 元数据证据一致。
- 元数据入口：`D:/ZZZ_Dump/PIK分析包/元数据/控制器与战斗/catalog_829.json`；通过既有 `GameplaySession("829")`、`exported_type` 读取快照，而不是从混淆名称猜类型。
- dump 资料：`D:/ZZZ_Dump/output/corin_replication/replication-guide/` 下的 `镜头参数.md`、`analysis/公共曲线.md`、`data/variants/shake-0.json` 和 `battle-0.json`。
- 本轮原始证据目录：[camera-shake-runtime-20260928](../../diagnostics/camera/camera-shake-runtime-20260928)。完整函数按 PE 异常目录边界反汇编，文件内保存边界、指令和函数字节哈希。
- 少数无异常目录条目的叶函数只保存 256 字节前缀，文件明确标记 `256-byte-prefix-not-complete-function`；不能把该前缀当完整函数。
- 函数普遍带 IFix 分流。[829 开关快照](../../diagnostics/camera/camera-shake-runtime-20260928/native-gates-829.json)中，创建、实例更新、位移、距离、仲裁及宿主更新的已检查 IFix 开关为关闭。初始化标志不是 IFix 开关，不能混用。此结果只适用于该快照，不外推其他游戏版本或随后加载的补丁。

### 关键函数表

| 职责 | 类型／方法 | RVA | 证据 |
| --- | --- | --- | --- |
| 攻击侧震动入口 | `KCFCEMEAKEF.LFNHBNKPBDO` | `0x15DC5EF0` | [创建函数](../../diagnostics/camera/camera-shake-runtime-20260928/combat-shake-create.json) |
| 命中震动入口 | `KCFCEMEAKEF.LBBNNCEBLGA` | `0x14056300` | [命中入口](../../diagnostics/camera/camera-shake-runtime-20260928/combat-hit-shake.json) |
| 获取震动实例 | `CameraDataAccessor.PBPNPCPGJMC` | `0x0F125A50` | [方法元数据](../../diagnostics/camera/camera-shake-runtime-20260928/combat-runtime-types.json) |
| 提交震动实例 | `CameraDataAccessor.HADEHOJELJO` | `0x0F121370` | [提交函数](../../diagnostics/camera/camera-shake-runtime-20260928/shake-submit.json) |
| 实例逐次更新 | `AONHPMMKECA.EMPJHIKLNHB` | `0x12A7ED00` | [更新函数](../../diagnostics/camera/camera-shake-runtime-20260928/shake-update.json) |
| 选择方向空间 | `AONHPMMKECA.MHEPEFNHNNI` | `0x12A7E530` | [空间分支](../../diagnostics/camera/camera-shake-runtime-20260928/shake-sample.json) |
| 构造位移信号 | `AONHPMMKECA.BGLBBBJMJPA` | `0x12A7DEE0` | [位移信号](../../diagnostics/camera/camera-shake-runtime-20260928/shake-spatial.json) |
| 距离衰减 | `AONHPMMKECA.KMFCACDAEEN` | `0x12A7DC30` | [距离衰减，文件沿用初查名称](../../diagnostics/camera/camera-shake-runtime-20260928/shake-envelope.json) |
| 周期信号加噪声 | `VNoiseParams.BPLIMBOKJGD` | `0x007BE130` | [噪声函数](../../diagnostics/camera/camera-shake-runtime-20260928/52818-BPLIMBOKJGD.json) |
| 配置按 key 解析 | 配置字典查询 | `0x15B75910` | [配置解析](../../diagnostics/camera/camera-shake-runtime-20260928/shake-config-resolve.json) |
| 宿主逐帧处理 | `NHEFILHBNND.MEIBNMFIBPN` | `0x174DD3C0` | [宿主更新](../../diagnostics/camera/camera-shake-runtime-20260928/host-MEIBNMFIBPN.json) |
| 基础选择及附加叠加 | `NHEFILHBNND.NOPJIAOCHID` | `0x174DE220` | [管理器求值](../../diagnostics/camera/camera-shake-runtime-20260928/host-NOPJIAOCHID.json) |
| 按身份取消 | `NHEFILHBNND.EBMNKBFGCDN` | `0x174DDAB0` | [取消处理](../../diagnostics/camera/camera-shake-runtime-20260928/host-EBMNKBFGCDN.json) |
| 主战斗相机写回 | `NapVirtual3DActionCamera_1.OHPGHKDFBKL` | `0x134DB450` | [相机输出](../../diagnostics/camera/camera-shake-runtime-20260928/writeback-0x134db450.json) |
| 另一相机更新路径写回 | `NapVirtual3DActionCamera_1.JNGDLMCHIOO` | `0x134E41A0` | [相机输出](../../diagnostics/camera/camera-shake-runtime-20260928/writeback-0x134e41a0.json) |

以上地址均为该二进制的 RVA，不能用到其他版本后仍假定有效。

### 实际加载的版本，而非仅比较导出文件

配置解析函数读取的字典在 829 快照中有 6350 项，从中读取了 81 条可琳震动的完整对象；28 个字段逐条与 `shake-0.json` 比较，共 2268 项，按 float32 精度比较数值，结果为零差异。见[实际对象](../../diagnostics/camera/camera-shake-runtime-20260928/loaded-corin-shakes-829.json)、[逐字段比较](../../diagnostics/camera/camera-shake-runtime-20260928/loaded-source-comparison.json)。这证明所查快照实际加载了这些值，不等于当前项目已经正确消费了它们，也不等于录屏观感已一致。

## 三、真实数据链与字段映射

创建函数直接接收 `ConfigCameraShake`。`rsi` 为配置，`rbx` 为申请出的 `AONHPMMKECA`。原字段名来自元数据，运行实例的混淆字段通过写入位置对应：

| 配置输入 | 实例位置 | 创建时处理 |
| --- | --- | --- |
| `StandardConfigKey` | `+0x28` | 保存字符串；已追通的求值、仲裁和写回链未消费该字段，配置解析直接返回字典对象 |
| `AngleVertical / RadiusLength / DistanceToPlane` | `+0xA8/+0xAC/+0xB0` | 三者分别乘创建入口的 scale；不能只缩放最终位置而漏掉角度 |
| `PitchAmplitude/YawAmplitude/RollAmplitude` | `+0x94/+0x98/+0x9C` | 乘同一入口 float 参数 |
| `Frequency` | `+0xCC` | 写入 `VNoiseParams.Frequency` |
| 固定值 1 | `+0xD0` | `VNoiseParams.Amplitude=1` |
| `NoiseRatio` | `+0xD4` | 写入 `VNoiseParams.RandomAmplitude` |
| `NoiseAngle` | `+0x88` | 单独保存，与 NoiseRatio 分离 |
| `CustomCurveKey` | `+0x30` | 用曲线解析函数 `0x137FB5D0`取得 AnimationCurve |
| `CurveKey` | `+0x18` | 取得时间衰减 AnimationCurve |
| `DissipationMode/ImpactRadius/DissipationDistance` | `+0x50/+0x84/+0x60` | 分别保存 |
| `ShakeTotalTime` | `+0xFC` | 保存时长 |
| `ShakeType` | `+0xE8` | 保存空间模式 |
| `RealtimeVibration/IngoreTimeScale` | `+0x68/+0x6B` | 分别保存，不能合并为一个忽略时间缩放开关 |
| `PlayStackingType/PlayPriority/DataPriority` | `+0xD8/+0x90/+0x6C` | 分别保存，不能只按一个优先级替代 |

提交函数给新实例写入递增序号 `+0xE0`，再放入 `CameraDataAccessor.Shake` 管理的列表。序号用于取消和移除身份；普通仲裁没有按“序号最新者获胜”排序。

已找到的普通动作直接创建调用点传 scale=1。命中入口也从 1 开始，但允许实体浮点变量 **`ShakeStrength`** 查询 `0x194F6A80` 成功时覆盖它。该字符串缓存槽 `0x5811100` 在 829 快照中尚为 null；继续沿 `0x2793E0` 初始化器的 `0x3233F` 使用表和 `0x27B370` 解密函数，解出了 literal 53812 的正文。不能因缓存未初始化便把所有命中 scale 硬编码成 1。见[直接调用者](../../diagnostics/camera/camera-shake-runtime-20260928/direct-callers.json)、[命中入口](../../diagnostics/camera/camera-shake-runtime-20260928/combat-hit-shake.json)、[字符串解码](../../diagnostics/camera/camera-shake-runtime-20260928/hit-scale-literals.json)、[解密函数](../../diagnostics/camera/camera-shake-runtime-20260928/metadata-literal-0x27b370.json)。

`StandardConfigKey` 尚不能断言在所有上游只用于作者工具；但实际加载对象与原配置全字段相同，且当前消费者无模板覆写步骤，不能再假设存在一份未找到的模板会自动修正当前波形。

## 四、波形、振幅、频率与相位

`VNoiseParams.BPLIMBOKJGD` 的普通路径可写为：

```text
u = Frequency * time + timeOffset
signal = 0.5 * Amplitude * cos(2π * u)
if RandomAmplitude >= 0.0001:
    signal += 2 * (PerlinNoise(u, 0) - 0.5) * RandomAmplitude
```

证据：

- `0x007BE151`乘频率，`0x007BE156`加偏移；再乘 2 和 π。
- `0x007BE171/176`分别乘振幅和 0.5；阈值与常量直接读自二进制：[常量](../../diagnostics/camera/camera-shake-runtime-20260928/noise-constants.json)。
- 三角函数 `0x00E4C370` 的小区间多项式为 `1-x²/2+x⁴/24-x⁶/720+…`，系数、分支与指令见[余弦证据](../../diagnostics/camera/camera-shake-runtime-20260928/cosine-polynomial-constants.json)及[完整函数](../../diagnostics/camera/camera-shake-runtime-20260928/trigonometric-function.json)。
- 噪声间接调用和 `UnityEngine.Mathf.PerlinNoise` 包装入口引用同一槽 RVA `0x0540A598`，见[Unity 数学包装](../../diagnostics/camera/camera-shake-runtime-20260928/unity-math-wrappers.json)。

战斗创建时 Amplitude=1，NoiseRatio 写入 RandomAmplitude。位移三个分量均使用 `timeOffset=0`；旋转 Pitch/Yaw/Roll 分别使用 `0.25/0.5/0`，频率相同。[通道常量](../../diagnostics/camera/camera-shake-runtime-20260928/shake-runtime-constants.json)

因此当前可琳 NoiseRatio=0 时，基础信号是 **`0.5*cos(2π*Frequency*time)`**，不含请求身份散列。这里的 time 是实例累计时间，受其时钟消费规则影响，不能不加限定地当墙钟。

项目当前 `sin(...+identityPhase)*RadiusLength` 的波形、起始值和基础幅度都不同。在排除方向与衰减后，单看周期信号峰值，当前满幅实现是原半幅信号的两倍；不能把这个比值直接当最终屏幕位移比例。

### 方向噪声是另一条独立链

`0x12A7E012`开始的代码采 `PerlinNoise(time,0)`。首次采样计数为 0 时用 AngleVertical；后续使用：

```text
angle = AngleVertical + 2 * (PerlinNoise(time, 0) - 0.5) * NoiseAngle
```

随后围绕所选水平方向构造旋转，结合 RadiusLength 与 DistanceToPlane 形成位移向量，再乘周期信号。这里不依赖 NoiseRatio，所以 **NoiseRatio=0 不能禁用 NoiseAngle**。

更新函数在输出后递增采样计数，并累加实例时间；复刻时应保留首帧与后续帧的区别。

## 五、空间、时间衰减与 RealtimeVibration

### 可琳使用的 mode 5 已确认

`0x12A7DC65`比较 DissipationMode 与 5；该分支直接把传入距离交给 `CustomCurveKey` 解析出的 AnimationCurve 求值，发生在 ImpactRadius/DissipationDistance 的普通分支之前。

所以 **DissipationDistance=0 不会关掉 mode 5 的自定义空间曲线**。项目目前跳过了它。

`Camera_ShakeSpatial_Curve_01` 的键为 `(0,0.5)、(2,0.5)、(4,1)、(10,1)`，不是归一化时间曲线。必须用距离采样，保留原切线与边界；不能先 Clamp01(distance)，也不能仅因名称有“衰减”就改为单调减小。

### 真实距离何时更新

- 创建时，RealtimeVibration=false 的路径计算相机侧位置与震源 Transform 的距离，保存到实例 `+0xA4`。
- 更新时，RealtimeVibration=true 的路径重新计算距离。
- `0x12A7F42E`读取这项距离并调用距离衰减函数。

这已经证明 RealtimeVibration 有“持续更新空间距离”的职责。项目把它与 IgnoreTimeScale 做 OR 后选择 UnscaledDeltaSeconds，不符合已取得的消费者证据；不能仅凭字段名字解释为实时钟。

### 时间衰减

正时长分支在 `0x12A7EFDE`读取累计时间、除以 ShakeTotalTime、夹到 0～1，再采 CurveKey；空间权重与时间权重相乘。`Camera_ShakeDecay_Curve_04`为常量 1，Rush 的该资源不会凭空产生时间衰减。

创建函数只在负时长路径配置 FadeIn/FadeOut；正时长路径会清零对应淡入淡出字段。当前可琳所查 13 项资源本来就为零，但通用系统不能不分正负时长套用同一套包络。

时钟不是 `IgnoreTimeScale || RealtimeVibration` 二选一，具体顺序见第八节。

### ShakeType 的已知行为

- 数值 0：使用创建时保存在 `+0xC0` 的方向，投影到水平面；该方向来自实体侧 `LMEACFFOIMO.EONJLJBFBOF`。
- 数值 1：读取 `CameraDataAccessor.Target.Forward`，随后进入同一个平面向量构造函数。
- 数值 2：先用 `(0,0,1)` 构造信号向量，将 Target.LookAtPosition 投影至实际相机屏幕，把深度约束在相机近／远裁剪面之间；在屏幕 XY 加偏移后回投，减去原 LookAtPosition，再叠加沿修正后相机前方的 Z 偏移。不能把这条路径简化成世界空间 XY 平移。

共同的平面向量构造已经核对：`forward = Normalize(水平输入方向)`，`right = Cross(up, forward)`；将 right 绕 forward 按上述 angle 旋转、归一化，得到 `right * RadiusLength + forward * DistanceToPlane`，三个分量乘同一 `signal(time, 0)`。零向量与基础轴常量见[坐标常量](../../diagnostics/camera/camera-shake-runtime-20260928/coordinate-constants.json)。模式 2 的屏幕坐标调用可从[空间分支](../../diagnostics/camera/camera-shake-runtime-20260928/shake-sample.json)复查。

本文按实际 raw 数值表达空间行为；枚举成员名与数值的对应未从常量表独立解出，不用声明排列猜测。可琳当前资源的 raw 值可以直接匹配对应算法。

## 六、Cinemachine 哪些可复用，哪些不能直接替代

本地对照版本为项目已有的 `com.unity.cinemachine@2.10.7`，**不是据此认定游戏使用相同版本**。

| 项目 | dump 消费者事实 | 本地源码对照／实现边界 |
| --- | --- | --- |
| 常量周期信号 | cos、Amplitude×0.5 | 与 `NoiseSettings.NoiseParams.GetValueAt` 的 Constant 分支一致 |
| 随机信号 | 有周期信号与 Perlin 的加法组合 | 原生 Constant/非 Constant 二选一不等于游戏 BPLIMBOKJGD 的叠加公式 |
| VNoiseSettings | 有 PositionNoise/OrientationNoise 数组并汇总通道 | 结构与 NoiseSettings 相近，但本次战斗实例直接嵌入 VNoiseParams，不经已找到的 UI 噪声对象 |
| 距离衰减 | 自定义 mode 5 直接采距离曲线 | 原生 Impulse 的距离分支可作对照，不能替代这项游戏扩展 |
| 多种震动系统 | dump 中还存在 DefaultShakeSequence、ImpulseManager、Galgame/UI 噪声 | 类型存在不等于可琳战斗调用它；不要混合它们的字段和公式 |

原则：已有 Unity/Cinemachine 数学原语可以复用；游戏的参数装配、方向空间、生命周期和仲裁必须按实际消费者实现。没有证据支持直接换成任意 Noise Profile 或 Impulse Preset 就等价。

## 七、管理器、取消与最终输出

### Base 与 Additive

宿主 `NHEFILHBNND` 从 `CameraDataAccessor.Shake` 取得 `CBPLFNAINAH`，先更新存活实例，再选择／叠加结果：

1. `PlayStackingType=1`：位移和旋转分别加入附加总和。
2. `PlayStackingType=0`：基础震动候选，比较 `PlayPriority`，数值较大者优先。
3. 相同优先级比较本帧已经求值的**位移向量模长**，严格更大才替换；相等时保留先遇到者。比较的不是配置振幅，也不是旋转模长。
4. 首个候选同样经过与初始零位移的比较；不能另加“首个必选”的分支。纯旋转、零位移候选因此不能擅自按旋转幅度入选。
5. 基础候选未被选中仍继续推进自己的时间，不是等待获选才开始播放。
6. 管理器 `+0x36` 的保持计数大于零时，保留实例 `+0x69` 标记的基础选择，不走普通重选。`BICAAKLLGEC(bool)` 负责进入／退出计数，[计数入口](../../diagnostics/camera/camera-shake-runtime-20260928/output-0x187098f0.json)已定位。向上已追到战斗工具 `KCFCEMEAKEF.BICAAKLLGEC` 以及两个调用者；其中 `COJGPJAPOBA.LNBPCKHKNKK` 明确传 true/false 成对进入退出，`NNNFCKFOIPG.DEELNEDKMFP` 转发业务值。[调用者身份](../../diagnostics/camera/camera-shake-runtime-20260928/hold-triggers.json)保留混淆原名；尚不能凭它们猜测具体招式触发条件。
7. 输出为 `(选中的 Base + 所有 Additive) * 管理器整体权重`。该权重属于相机整体抑制状态，不是单个效果的 FadeOut。

完整证据：[管理器求值](../../diagnostics/camera/camera-shake-runtime-20260928/host-NOPJIAOCHID.json)。`PlayPriority`、`DataPriority`、事件序号各有职责，不能合并为一个排序值。

### 取消、自然到期和 OnExit

- 按字符串／实体取消的入口筛选**效果 key `+0x10`**及实体 ID，再将对应序号入取消队列，不是用 StandardConfigKey 匹配。[筛选函数](../../diagnostics/camera/camera-shake-runtime-20260928/accessor-GENBHBKFANB-0xf125f60.json)
- 另一取消入口按 `ConfigDataClearPriority` 的 raw 值工作：0 筛选 DataPriority 相等；3 筛选 DataPriority 小于等于传入值；1、2 在该函数内都直接排队传入的实例 ID。不能据此断言 1、2 的所有上游业务含义相同。[优先级筛选](../../diagnostics/camera/camera-shake-runtime-20260928/accessor-GENBHBKFANB-0xf120ec0.json)
- 取消单个实例时，只有**负时长、正 FadeOutDuration 且有曲线**的实例进入退场，保存当前累计时间为退场起点；其余情况调用停止并清空该实例的活动状态、时间、输出相关状态。[退场判定](../../diagnostics/camera/camera-shake-runtime-20260928/shake-stop-status.json)、[停止](../../diagnostics/camera/camera-shake-runtime-20260928/shake-stop.json)
- 正时长自然到期由实例更新负责。先求本次输出，再判断当前累计时间是否到期；未到期才递增采样计数和累计时间。因此复制公式时也要保持更新先后顺序。
- 项目继续由 OnExit／OnDisable 条件回调决定**是否请求取消**。请求取消后，效果内部才按上述正负时长规则处理；不能为所有正时长震动额外加固定 0.016 秒退场，不能因一个 Clip 自然结束就无条件取消仍需持续的效果。

### 相机整体静默

`AnimatorZoneMuteCameraShake.OnZoneEnter/Exit` 调用主相机 `0x134E14B0`，维护实体 ID 列表；这条路径不是上面的 Base 保持计数。主相机 `get_MuteCameraShake` 向震动宿主提供静默状态。[进入](../../diagnostics/camera/camera-shake-runtime-20260928/mute-zone-0x11464710.json)、[退出](../../diagnostics/camera/camera-shake-runtime-20260928/mute-zone-0x11464b70.json)、[列表管理](../../diagnostics/camera/camera-shake-runtime-20260928/mute-camera.json)、[状态读取](../../diagnostics/camera/camera-shake-runtime-20260928/mute-camera-getter.json)

宿主整体权重有正常、渐静默、静默、渐恢复四种 raw 状态 0/1/2/3。渐变时长是约 **0.12 秒**，以切换时的当前权重为起点；中途反转也从当前值继续，不重新跳到 0 或 1。[整体权重处理](../../diagnostics/camera/camera-shake-runtime-20260928/host-LJMHOGFLLIC.json)、[常量记录](../../diagnostics/camera/camera-shake-runtime-20260928/resolved-boundaries.json)

### Cinemachine 写回

最终主战斗相机读取管理器合成值，执行：

```text
state.PositionCorrection += shakePosition
state.OrientationCorrection = state.OrientationCorrection * Quaternion.Euler(shakeRotationDegrees)
```

位置直接进入世界位置修正，不能在输出处再乘一次 Camera.rotation。旋转从度转弧度后进入 Unity 的四元数转换；右乘顺序由 SIMD 指令逐项展开确认，见[四元数代数](../../diagnostics/camera/camera-shake-runtime-20260928/writeback-quaternion-algebra.json)。Cinemachine 的 CameraState 提供最终合成容器，游戏自有模块提供信号、坐标、包络和仲裁；“使用 Cinemachine”不等于“换成原生 Impulse 就一致”。

## 八、时钟的实际消费顺序

宿主 `0x174DD3C0` 先选择 delta，再交给实例 `0x12A7ED00`：

1. 输入 delta 为正，且 `MUTE_CAMERA_SHAKE_ADVANCED_PROCESS=true` 时，保留调用者 delta。
2. 输入 delta 为正、该配置未启用时，调用槽 `0x0540AC98`。游戏内 `CinemachineBrain.GetEffectiveDeltaTime` 的 `m_IgnoreTimeScale=false && fixedDelta=false` 分支使用同一槽；对应本地 Cinemachine 2.10.7 源码是 **Time.deltaTime**。这是原生分支与源码交叉识别，不是已读到具名 Time 包装函数；没有依据把它替换成 unscaledDeltaTime。[Brain 原生函数](../../diagnostics/camera/camera-shake-runtime-20260928/brain-time.json)、[槽地址记录](../../diagnostics/camera/camera-shake-runtime-20260928/resolved-boundaries.json)
3. 非正输入 delta 的该路径取 0；高级处理启用时，比较全局对象 `+0x120` 的状态字符串：不等于 **`Action3DCamera`** 时请求取消全部实例。该字面量同样从初始化元数据独立解出，见[状态字符串](../../diagnostics/camera/camera-shake-runtime-20260928/host-state-literals.json)。不能把所有非正 delta 都一律清理，也不能当普通正时间更新处理。
4. 实例读取 `DHDAMGEBBHN` 的倍率 `+0x7C`；其 `+0x104` 状态开启则用 1。实例 IngoreTimeScale=true 时本级倍率用 1；false 时用该倍率，恰为 0 时替换成 **0.0001**。这不是 `Max(scale, 0.0001)`。
5. 用已有 elapsed 求值，在当前更新末尾执行 `elapsed += selectedDelta * effectiveScale`。IngoreTimeScale 只绕过这里的倍率，不能倒推上游 delta 一定是未缩放时间。

倍率生产者已追到 `DHDAMGEBBHN.OCKAOBKPAFD`：读取 `+0x18` 的浮点聚合器输出，乘该对象 `+0x94` 再写入 `+0x7C`；初始化写 1。见[生产者](../../diagnostics/camera/camera-shake-runtime-20260928/clock-OCKAOBKPAFD-0x138fb990.json)、[初始化](../../diagnostics/camera/camera-shake-runtime-20260928/clock-OnCreate-0x138faa50.json)。聚合器各项究竟对应哪些受击／暂停业务尚未逐一还原，不能把它直接等同于 Unity Time.timeScale 或项目任意一个 HitStop 值。

## 九、项目修正边界

以下只记录实现应遵循的输入、处理和输出，不代表本轮已实施：

1. 作者资源仍是唯一输入；相机运行模块消费 Frequency、NoiseRatio、NoiseAngle 等字段，替换现有自造公式，移除身份散列相位及没有原作证据的频率差。
2. 在相机域取得震源位置／方向与相机基准数据，按 ShakeType 和 RealtimeVibration 决定快照或持续更新；不从技能直接修改 Unity Camera。
3. CustomCurveKey 应解析为正式空间曲线依赖，距离权重和时间权重按原调用顺序组合。
4. 命中震动从正式命中结果触发，保留 ShakeOnNotHit 条件；动作帧震动与命中震动各自依照 dump 来源。
5. 节点负责瞬时触发；持续效果由已有连续 Clip 合同表达。选择性取消仍由 OnExit/OnDisable 的正式条件回调表达，不擅自把所有宿主片段结束都当震动取消。
6. E 跨 Start/Loop/Walk 的持续镜头范围与本文件震动公式问题分别修复，不能通过每段重新触发来掩盖效果身份丢失。
7. 相机域拥有实例时间、基础选择、附加累加和最终修正输出；技能及命中系统只提供触发、取消、身份和源数据。必须保留 delta 与额外倍率两个输入的区别。
8. 消费已有 Cinemachine／Unity 数学能力时按上述原链装配，不添加人为随机相位、轴频率差或未经证实的模板默认值。

## 十、研究覆盖表与剩余边界

| 问题 | 本轮状态 | 实施限制 |
| --- | --- | --- |
| 波形、振幅、相位、方向噪声、曲线 | 已取得消费者与常量 | 可据此写求值逻辑，不能再自造频率／相位 |
| Base/Additive、优先级、身份取消、最终写回 | 已追通 | 保持计数的业务触发尚未逐项还原，不能擅自创造触发条件 |
| 实际资源与 IFix | 已核实 829 的 81 项资源及列出的核心开关 | 只覆盖指定快照；不能宣称其他版本也相同 |
| 时间 delta | 已追通宿主选择，并通过 Brain 分支对照识别默认槽 | 具名 Time wrapper 在现有导出中无 RVA；保留交叉证据等级 |
| 额外时间倍率 | 已定位生产、消费、零值和绕过规则 | 聚合器上游受击／暂停因素尚未全部还原；不擅自与项目 HitStop 一对一映射 |
| 命中创建强度 | 已定位可选实体变量 `ShakeStrength`；字符串从初始化元数据解密 | 必须按实体变量实际值处理，不能把动态强度判成恒定 1；其玩法侧赋值不属于相机求值器 |
| 空间模式 | raw 0/1/2 算法与修正写回已定位 | 枚举具名数值未独立解出；屏幕模式还需场景尺寸、投影与视口一致才有可比画面 |
| StandardConfigKey | 保存位置、当前消费者、实际载入值已查 | 更上游模板生产过程未全部还原；不能断言全游戏只在编辑器使用 |
| 高级处理中的非正 delta | 已解出与 `Action3DCamera` 的比较及全体取消分支 | 需要由相机状态生产者提供真实状态，不能将任意暂停都映射为离开此状态 |
| 日常跟拍、阻尼、FOV、持续推镜、命中触发时机 | 原资源和项目缺口见先前消费审计 | 不属于“震动求值查完”的等价范围，尚不能由本文承诺全部镜头手感一致 |

这些条目区分了已经读完的消费者、由交叉证据识别的引擎入口，以及仍未解出的上游数据。**当前可实现已确认的震动核心，不能声称全场景 100% 复刻，也不能拿猜测补齐剩余项。** 若实施涉及未确认的业务输入，必须先补该项证据；本轮没有将这张表中的“未确认”偷偷实现成默认值。

## 十一、验证状态

已核实二进制版本、元数据类型身份、关键函数边界、指令常量、829 已加载配置、选定 IFix 开关以及四元数乘法顺序；原始证据与哈希保存在同目录 manifest。未运行游戏画面对照、Unity Play、回放或代码编译，因为本轮没有修改运行代码。本文件中的“确认”指静态消费者／快照证据，不代表复刻效果已经交付。
