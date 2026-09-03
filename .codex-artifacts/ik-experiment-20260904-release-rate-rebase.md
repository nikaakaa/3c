# ReleaseCompleted 后按现有下降速率剥离一步旧响应

## 假设

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 中，ReleaseCompleted 后紧接新 Contact 时，旧 AnimationRelativeScalar 仍完整进入 PlantWorldResidual。整段剥离会把跳变推迟到脚底输出，因此本轮只剥离 Profile 的 `CorrectionResponseDecreaseSpeed * DeltaSeconds` 一步，保留其余世界位置连续性。

## 改动

- 仅在 `ReleaseCompleted → Plant` 的直接交接设置一次待处理标记。
- 下一次 Plant 捕获连续性残差前，沿旧响应方向扣除一个现有下降速率步长。
- 普通 Swing 先发生时清除标记；查询、目标、Residual 衰减、状态转换和 Profile 不变。

## 预期

右脚 1916 一类的旧 Release 残差应小幅下降，避免整段重基造成的新跳变；Landing/Release 事件集合和非交接帧保持不变。

## 回放

待固定 Trace 回放完成后补充实际样本路径、正式诊断计数和异常帧对比。若跳变、穿透或弯曲抖动回归，保留本样本并回退代码。
