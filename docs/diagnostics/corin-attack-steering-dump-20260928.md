# 可琳攻击转向 dump 对照

## 来源

先读 `D:/ZZZ_Dump/output/corin_replication/replication-guide/技能与输入.md`，再查 `data/zones.json`、`data/windows.json`。

原始解码文件：`D:/ZZZ_Dump/output/corin_replication/20260904_config_decoded_v6/1387972831.blk_export/CAB-747b46ca75fd1cf803f29ba087ecdb75/AnimatorZone_Avatar_Female_Size01_Corin.json`。实际 SHA256 与 source-index 一致：`3acf6cc38cb004edb1d11b8bb906645e40ef3b8189d25b87819e6cce4c5b7f15`。本次为只读核查，未修改业务配置。

## 普攻锁敌转向

五段共用 `LockTargetZone_Attack`：

- `NeedLockTargetOnZoneEnter=true`，`ForceReselectLockTarget=false`。
- `steerType=1`，`steerDirectionType=0`。
- `ApplyRotateSpeedZone=false`，`RotateSpeedRatio=60`。
- `curveRotateSpeed` 和 `CurveRotateSpeed` 的值均恒为 1。
- `AllowRootMotionRotation=false`，`UseJoystickSteerPreferred=false`。

| 状态 | StartFrame–EndFrame | StartNormalizedTime–EndNormalizedTime |
| --- | --- | --- |
| Attack_Normal_01 | 0–10 | 0–0.2 |
| Attack_Normal_02 | 0–9 | 0–0.2 |
| Attack_Normal_03 | 0–32 | 0–0.4 |
| Attack_Normal_04 | 0–18 | 0–0.2 |
| Attack_Normal_05 | 0–25 | 0–0.2 |

两种时间字段都保留。现有 `analysis/时间区域规则.md` 已确认 `ConfigMisc.IsAnimatorZoneUseFrame` 决定时间域，但未确认活动实例开关，不能武断只采用其中一组。

## 独立的转速区域

原始大写 `RotSpeedCurve` 的（时间，值）关键点如下，尚未由本轮独立确认该字段运行采样的时间单位：

| 区域 | rotSpeedFactor | RotSpeedCurve |
| --- | --- | --- |
| 普攻1/2共用 RotSpeedZone_Attack | 999 | (0,1)、(1,1)、(1.01,0)、(100.00001,0) |
| 普攻3 | 6 | (0,1)、(8,1)、(16,0)、(80,0) |
| 普攻4 | 6 | (0,1)、(9,1)、(18,0)、(88,0) |
| 普攻5 | 6 | (0,1)、(12,0)、(124.00001,0) |
| 普通Rush | 1 | (0,999)、(1,999)、(1.01,1.5)、(70.00001,1.5) |

这些区域也配置 `steerType=1`、`AllowRootMotionRotation=false`。锁敌区明确 `ApplyRotateSpeedZone=false`，因此不能直接将上述区域曲线与锁敌倍率相乘，也不能只抄一个 999 给所有攻击。

普通 Rush 的锁敌区另有 `RotateSpeedRatio=1`，大写 `CurveRotateSpeed` 为 (0,60)、(4,60)、(18,6)、(70,6)。Branch 的锁敌倍率为 12，大写曲线在 5、0、1、0 间分段；不能把普攻转向配置推广为所有技能共用。

## 与当前实现的差异和结论边界

当前五段 MotionWarp 从 0.116667/0.083333/0.1/0.15/0.166667 秒开始，按累计偏航进度在约 0.4–0.55 秒窗口内校正到动作目标快照，总校正上限为 90°。这不是上述 dump 锁敌转速窗口的直接抄录。

dump 支持“攻击开头立即开始、高倍率快速转向”的解释，符合用户观察。它尚不足以证明原生执行为一帧直接设置朝向：`steerType=1` 的枚举含义、倍率对应的基准转速及执行公式仍需原生函数证据。不能把 60 宣称为 60 度/秒，不能先将所有攻击改成无条件瞬转，也不能把速度倍率曲线直接塞进累计角度进度曲线。
