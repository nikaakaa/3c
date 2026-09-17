# 效果组合顺序与贡献状态证据（4.5）

修订 v1，2026-09-17。固定顺序 Override → Zoom → Stretch → Shake → Shot → Environment 由 `CameraEffectEvaluator` 唯一执行；本页记录顺序依据与 Shake 后 Basis 的采样合同。

## 顺序依据

1. **Override 最先**：姿态接管改变的是"取景基准"本身，必须在 Zoom/Stretch 等参数修正之前确定基准帧。
2. **Zoom → Stretch 居中**：两者都是对基准帧的参数修正（FOV、画面形变），按固定先后应用，权重走各自 Clip 曲线。
3. **Shake 在 Shot 之前**：震屏是叠加扰动，作用在已合成的期望位姿上；Shot 切机位是整帧替换，替换后的新机位仍需接受扰动以保持命中反馈连续。
4. **Shot 不越过 Environment**：切机位后的最终位置仍必须通过环境约束（近裁剪/墙体），不得因 Shot 免检。
5. **Environment 最后**：任何效果的结果都先进入统一碰撞收缩/恢复，输出安全计划后才交给 RigAdapter。

## Shake 后 Basis 对动作采样的合同

- Shake 修正发生在 `CameraFramePlan` 层，最终 plan 经 `ICameraRigAdapter` 应用后回读 `CameraRigResult`/`CameraBasisSnapshot`。
- 因此 **`CameraBasisSnapshot`（含 LookDirection）反映的是包含 Shake 在内的实际输出**：动作采样读取 Basis 时感知的是震屏后的镜头方向，与玩家所见一致。
- 不存在第二个"无 Shake 的干净 Basis"数据源；动作系统不得绕过 BasisSnapshot 另取方向。

## 贡献状态

每个效果 owner 求值后向 plan 写入自己的贡献结果；诊断快照（CameraDebugSnapshot）按顺序记录各效果贡献与 Environment 修正前后差异，排查时按该顺序读。