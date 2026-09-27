# ZZZ Corin RushAttack Timeline 对照

原始证据：`D:/ZZZ_Dump/output/corin_replication/20260903_controller_structured_v6/Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`。状态启用 `m_UseFrameCount`，有退出时间的边使用 `m_FrameCount`；不能用旧 `m_ExitTime × 总帧数` 反推代替。例如强化起手的原始值为 38 帧，不是 35 帧。

## 强化状态的正式配置

下表时间为 Timeline 轴帧数，窗口为左闭右开区间。动画都引用正式生成的 FootMotionTarget，素材完整长度与状态提前退出时间分开。

| 状态 | 实际 Motion | Timeline 时长 | 接招窗口 | 移动退出窗口 |
|---|---|---:|---|---|
| `Attack_Rush_Enhance` | `Attack_Rush_Enhance_Start` | 38 | 无 | 无 |
| `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Loop` | 80，循环 | 无 | 无 |
| `Attack_Rush_Enhance_End` | `Attack_Rush_Explode` | 70 | `[0,40)` | `[12,70)` |
| `Attack_Rush_Enhance_Explode` | `Attack_Rush_Enhance_Explode` | 63 | `[16,44)` | `[24,63)` |
| `Attack_Rush_Enhance_Explode_End` | `Attack_Rush_Enhance_End` | 118 | 无 | `[16,118)` |

强化 End 的按键接招条件只有 `FrameCount < 40`，没有最早第 14 帧的 FrameCount 条件。强化 Explode 的按键接招条件为 `FrameCount >= 16 && FrameCount < 44`。移动退出分别取实际条件中的 12 / 24 帧；无显式 FrameCount 条件的恢复段按原始转移字段保留 16 帧。

`Badge_S03 + HoldAttackA` 的自动续接条件尚需独立输入/Control 证据；当前窗口消费的是 Attack 请求，不把按住自动当成重复按键。

## 普通分支

普通三段继续保留独立 Timeline 和既有作者源码。当前 `Attack_Rush_Explode` 正式长度为 63 帧，接招从第 14 帧开放，移动退出仍按当前作者配置的 44 帧。原始 JSON 的移动条件是 42 帧；这是尚待处理的参数差异，不混同为本轮强化修正已经覆盖。

## 生成链

强化五段入口调用 `CorinRushTimelineAuthoringBuilder`，输入为状态身份、分支身份、AnimationClip、状态时长和 ActionWindow 区间。

- 动画轨道输出 `FullBodyAction`，slot 为 `corin.full-body-action`。
- 每个窗口有真实 TimelineBody 图、Frame Blackboard 投影和结束条件。
- `TimelineTime` 只在所属 TreeClip 内消费。
- 无窗口的状态不生成空逻辑轨道；重复生成清理旧空 Boundary 图。
- Timeline / Section / AnimationClip 播放身份沿用原 stable seed，窗口使用各自状态与窗口名生成身份。

本轮不改动画曲线、不重做足部分析、不重新压缩 ACL；已有动画资源由当前正式资源目录复用。