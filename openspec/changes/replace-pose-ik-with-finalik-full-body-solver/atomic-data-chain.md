# Foot Placement重做基线

## 已删除结论

旧实现不再作为重做基础。它在平地闪烁、实际脚偏离可视化Animation Foot Route，并在转向时把旧Path、Surface和Ground Envelope直接刚体旋转。最新失败run `66435f5348cb4125b4106af6bff7e70a`还出现Foot位于`X≈49.58m`而支撑样本位于`X≈52.62m`的情况，局部斜面被跨约3.1m外推并产生约1.216m物理穿透。

## 保留的诊断纪律

后续每次只验证一个边界：先平地Animation Foot Route与Native Sole，再Body Trajectory，再Query/Hull，再Landing/Lock/Pelvis，最后FBBIK。编译、Build、Console和CSV等宽只证明工具链，不证明运动效果。

## 当前状态

旧Predictive、Stance/Anchor/Pelvis、CSV与自动入口已删除；共享Animation/Editor旧引用已经清理。当前只接回可运行的Landing Prediction与无文字Gizmo，三个FBBIK Goal权重为零，因此它是落点验证版本，不是完整Foot IK效果版本。

## Managed发布边界

`AnimationFootFeatureSample`内嵌Current与Incoming完整Step固定页，单值约10KB。它可以留在预分配Native/数组页中，但不得继续内嵌进按值传递的managed Source Sample或作为`Dictionary`值类型；Mono会在调用点以`InvalidProgramException: Passing an argument of size '10000'`拒绝该IL。正式managed边界使用引用对象保存不可变Sample，并通过`ref readonly`读取左右Foot Feature，容器和调用链只传对象引用。
