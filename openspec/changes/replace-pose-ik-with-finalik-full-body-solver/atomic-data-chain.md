# Foot Placement重做基线

## 已删除结论

旧实现不再作为重做基础。它在平地闪烁、实际脚偏离可视化Animation Foot Route，并在转向时把旧Path、Surface和Ground Envelope直接刚体旋转。最新失败run `66435f5348cb4125b4106af6bff7e70a`还出现Foot位于`X≈49.58m`而支撑样本位于`X≈52.62m`的情况，局部斜面被跨约3.1m外推并产生约1.216m物理穿透。

## 保留的诊断纪律

后续每次只验证一个边界：先平地Animation Foot Route与Native Sole，再Body Trajectory，再Query/Hull，再Landing/Lock/Pelvis，最后FBBIK。编译、Build、Console和CSV等宽只证明工具链，不证明运动效果。

## 当前状态

Predictive、Stance/Anchor/Pelvis、Gizmo、CSV与自动入口已删除；共享Animation/Editor引用尚待清理。当前工作区是明确的重做准备态，不是可运行Foot IK版本。
