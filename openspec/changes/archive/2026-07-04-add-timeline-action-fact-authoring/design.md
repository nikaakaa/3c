# Design: Timeline 动作事实

## 轨道职责

Timeline 轨道负责“什么时候发生什么”：

- `AnimationTrack`：动画贡献。
- `ActionWindowTrack`：Hit、Cancel、IFrame、Guard 等窗口。
- `ActionCueTrack`：Gameplay、VFX、SFX、Camera 等 cue。
- `MotionWarpTrack`：允许 motion warp 的时间窗口。

轨道不负责 action activation，也不负责命中成立。

## Action Context

TimelineNode 可配置 Action Context：

- 空 Action Context：普通表现 Timeline，不创建 ActionInstance，不提交 action-scoped window/cue。
- 有效 Action Context：Timeline 输出携带 ActionInstanceId，交给 ActionProfile 策略解析。

Timeline asset 可以被多个 ActionProfile 复用，因此 Timeline clip 不保存完整网络策略。

## GameplayResult 边界

第一阶段不要求 Timeline 直接产出 GameplayResult。HitWindow 只是攻击窗口事实，命中、伤害、目标归属属于 gameplay solver 或服务端。Loopback demo 可以后续有正式 debug solver，但不能放在 RootTree 平铺测试节点里伪装成动作 body。

## Corin 清理目标

Corin 攻击闭环应从：

```text
RootTree:
  Activate Attack
  Play Attack Timeline
  Submit Attack Window
  Submit Attack Cue
  Submit Loopback Result
```

收敛为：

```text
Action State:
  OnEnter Activate Attack
  Root Play Attack Timeline(Action Context)
  OnExit Submit Lifecycle

Attack Timeline:
  AnimationTrack
  ActionWindowTrack
  ActionCueTrack
```

其中 Action State 的落点由后续 Corin 编排 proposal 处理，本 proposal 只约束 Timeline 动作事实来源。
