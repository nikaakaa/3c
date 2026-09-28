# 可琳摄像机震动复刻依据

日期：2026-09-28。状态：已取得战斗创建、实例更新、位移信号与距离衰减的原生函数证据；尚未完成管理器仲裁和最终写回还原。本轮只编写复刻依据，不修改游戏运行代码或作者资产。

## 一、结论

当前项目的震动不能称为按原逻辑复刻。差异已经不只是“可能波形不同”：原战斗实例使用余弦半幅值、固定通道相位、独立方向噪声，以及真实距离驱动的自定义空间曲线；当前实现使用满幅正弦、请求身份散列相位，把方向噪声与 NoiseRatio 绑定，并没有消费空间曲线。

用户指出 Cinemachine 自带功能是有效线索。dump 同时存在 Cinemachine 类型和游戏自有 VNoise 类型。实际已追通的战斗链是 `ConfigCameraShake → AONHPMMKECA → CameraDataAccessor.Shake`，其中使用 `VNoiseParams`，不是仅凭库里存在 `CinemachineImpulseSource` 就认定直接播放原生 Impulse。

## 二、来源与可重复核对入口

- 游戏二进制：`D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/GameAssembly.dll`。
- 本轮已重新核对 SHA-256：`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`，与现有 829 元数据证据一致。
- 元数据入口：`D:/ZZZ_Dump/PIK分析包/元数据/控制器与战斗/catalog_829.json`；通过既有 `GameplaySession("829")`、`exported_type` 读取快照，而不是从混淆名称猜类型。
- dump 资料：`D:/ZZZ_Dump/output/corin_replication/replication-guide/` 下的 `镜头参数.md`、`analysis/公共曲线.md`、`data/variants/shake-0.json` 和 `battle-0.json`。
- 本轮原始证据目录：[camera-shake-runtime-20260928](camera-shake-runtime-20260928/)。完整函数按 PE 异常目录边界反汇编，文件内保存边界、指令和函数字节哈希。
- 少数无异常目录条目的叶函数只保存 256 字节前缀，文件明确标记 `256-byte-prefix-not-complete-function`；不能把该前缀当完整函数。
- 函数普遍带 IFix 分流。本文件公式对应已读取二进制的普通原生路径，不声称已还原运行期间可能加载的热更替代实现。

### 关键函数表

| 职责 | 类型／方法 | RVA | 证据 |
| --- | --- | --- | --- |
| 攻击侧震动入口 | `KCFCEMEAKEF.LFNHBNKPBDO` | `0x15DC5EF0` | [创建函数](camera-shake-runtime-20260928/combat-shake-create.json) |
| 命中震动入口 | `KCFCEMEAKEF.LBBNNCEBLGA` | `0x14056300` | [命中入口](camera-shake-runtime-20260928/combat-hit-shake.json) |
| 获取震动实例 | `CameraDataAccessor.PBPNPCPGJMC` | `0x0F125A50` | [方法元数据](camera-shake-runtime-20260928/combat-runtime-types.json) |
| 提交震动实例 | `CameraDataAccessor.HADEHOJELJO` | `0x0F121370` | [提交函数](camera-shake-runtime-20260928/shake-submit.json) |
| 实例逐次更新 | `AONHPMMKECA.EMPJHIKLNHB` | `0x12A7ED00` | [更新函数](camera-shake-runtime-20260928/shake-update.json) |
| 选择方向空间 | `AONHPMMKECA.MHEPEFNHNNI` | `0x12A7E530` | [空间分支](camera-shake-runtime-20260928/shake-sample.json) |
| 构造位移信号 | `AONHPMMKECA.BGLBBBJMJPA` | `0x12A7DEE0` | [位移信号](camera-shake-runtime-20260928/shake-spatial.json) |
| 距离衰减 | `AONHPMMKECA.KMFCACDAEEN` | `0x12A7DC30` | [距离衰减，文件沿用初查名称](camera-shake-runtime-20260928/shake-envelope.json) |
| 周期信号加噪声 | `VNoiseParams.BPLIMBOKJGD` | `0x007BE130` | [噪声函数](camera-shake-runtime-20260928/52818-BPLIMBOKJGD.json) |

以上地址均为该二进制的 RVA，不能用到其他版本后仍假定有效。

## 三、真实数据链与字段映射

创建函数直接接收 `ConfigCameraShake`。`rsi` 为配置，`rbx` 为申请出的 `AONHPMMKECA`。原字段名来自元数据，运行实例的混淆字段通过写入位置对应：

| 配置输入 | 实例位置 | 创建时处理 |
| --- | --- | --- |
| `StandardConfigKey` | `+0x28` | 保存字符串；后续是否参与模板覆盖尚未确认 |
| `AngleVertical / RadiusLength / DistanceToPlane` | `+0xA8/+0xAC/+0xB0` | 三者分别乘入口的 float 参数；该参数业务含义及各调用点取值待确认 |
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

提交函数给新实例写入递增序号 `+0xE0`，再放入 `CameraDataAccessor.Shake` 管理的列表。原实现保留创建次序，但本轮尚未完成该序号在仲裁中的消费证明。

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
- `0x007BE171/176`分别乘振幅和 0.5；阈值与常量直接读自二进制：[常量](camera-shake-runtime-20260928/noise-constants.json)。
- 三角函数 `0x00E4C370` 的小区间多项式为 `1-x²/2+x⁴/24-x⁶/720+…`，系数、分支与指令见[余弦证据](camera-shake-runtime-20260928/cosine-polynomial-constants.json)及[完整函数](camera-shake-runtime-20260928/trigonometric-function.json)。
- 噪声间接调用和 `UnityEngine.Mathf.PerlinNoise` 包装入口引用同一槽 RVA `0x0540A598`，见[Unity 数学包装](camera-shake-runtime-20260928/unity-math-wrappers.json)。

战斗创建时 Amplitude=1，NoiseRatio 写入 RandomAmplitude。位移三个分量均使用 `timeOffset=0`；旋转 Pitch/Yaw/Roll 分别使用 `0.25/0.5/0`，频率相同。[通道常量](camera-shake-runtime-20260928/shake-runtime-constants.json)

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

IgnoreTimeScale 对更新所取全局倍率有独立分支；具体倍率生产者、零倍率处理及调用者传入 delta 的时间域还需继续核实，文档不据此声称时钟完全对齐。

### ShakeType 的已知行为

- 数值 0：使用创建时保存在 `+0xC0` 的方向，投影到水平面；该方向来自实体侧 `LMEACFFOIMO.EONJLJBFBOF`。
- 数值 1：读取 `CameraDataAccessor.Target.Forward`，随后进入同一个平面向量构造函数。
- 数值 2：另有屏幕投影／回投及相机旋转组合。

已确认不能把三个模式统一当成 `Camera.rotation * XYOffset`。完整轴正负、叉乘方向、数值 2 的投影边界，以及枚举成员名与数值的最终对应仍待补证；声明列表的排列顺序不能代替枚举常量。

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

## 七、项目修正边界

以下只记录实现应遵循的输入、处理和输出，不代表本轮已实施：

1. 作者资源仍是唯一输入；相机运行模块消费 Frequency、NoiseRatio、NoiseAngle 等字段，替换现有自造公式，移除身份散列相位及没有原作证据的频率差。
2. 在相机域取得震源位置／方向与相机基准数据，按 ShakeType 和 RealtimeVibration 决定快照或持续更新；不从技能直接修改 Unity Camera。
3. CustomCurveKey 应解析为正式空间曲线依赖，距离权重和时间权重按原调用顺序组合。
4. 命中震动从正式命中结果触发，保留 ShakeOnNotHit 条件；动作帧震动与命中震动各自依照 dump 来源。
5. 节点负责瞬时触发；持续效果由已有连续 Clip 合同表达。选择性取消仍由 OnExit/OnDisable 的正式条件回调表达，不擅自把所有宿主片段结束都当震动取消。
6. E 跨 Start/Loop/Walk 的持续镜头范围与本文件震动公式问题分别修复，不能通过每段重新触发来掩盖效果身份丢失。

## 八、尚不足以承诺完全复刻的项目

- 管理器怎样消费 PlayStackingType、PlayPriority、DataPriority 和递增序号；提交入队已确认，重叠选择／替换／相加尚未追完。
- 时间倍率与最终相机输出的完整调用者链，尤其是停顿、受击时间缩放与迟到触发。
- CameraDir/CameraScreenDir/EpicenterDir 的枚举常量与完整坐标转换。
- StandardConfigKey 的后续使用，以及 CameraShakePropConfigEnum 是否在更上游改变装配。本轮创建函数确实保存了模板键，不能再当成“肯定仅作者使用”。
- IFix 当前实际替换状态，以及游戏实际加载的资源变体。本轮只证明指定版本原生路径与已整理资源。

因此本文件已能指导修正波形、有效幅度、相位、方向噪声及 mode 5 空间曲线；还不能把它描述为全链路、所有场景的完全复刻。

## 九、验证状态

已核实二进制版本、元数据类型身份、关键函数边界与指令常量，保存了原始证据。未运行游戏画面对照、Unity Play、回放或代码编译，因为本轮没有修改运行代码。本文件中的“确认”指静态消费者证据，不代表复刻效果已经交付。
