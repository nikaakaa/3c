# 首次已验证 Contact 加快世界残差衰减

## 假设

半步交接已经把旧 Release 响应的首帧影响降低，但 `PlantWorldResidual` 首次仍按 `0.03s` 半衰期衰减。若“平滑跨过一级”主要由这条衰减曲线造成，首次已验证 Contact 将半衰期缩短一半，应该更快接近踏面；后续帧保持原曲线，避免全局改速率。

## 改动

- 仅在 `ReleaseCompleted` 后进入已验证、已进入状态的首个 Plant 事件中，将本次解析出的残差半衰期乘 `0.5`。
- 普通 Contact、同事件持续帧、Swing/Release 及其它换代仍使用现有 Profile 半衰期。
- 交接剥离量、目标、查询、Transition、Pelvis、Solver、穿透容差和其它 Profile 不变。

## 预期

右 1916 的 Plant output 与后续 residual 尾巴更快落向已验证踏面，目标 extension/bend 更快恢复；必须检查首帧和后续跳变、穿透、左右脚、Pelvis/Physical 全包覆盖。

## 回放

待固定 Trace 回放完成后补充实际 Samples 和完整对比。若首帧过伸、穿透或二次跳变增加，保留失败样本并精确回退本轮代码。
