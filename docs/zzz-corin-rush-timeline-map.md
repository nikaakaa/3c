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

普通三段继续保留独立 Timeline 和既有作者源码。`Attack_Rush_Explode` 正式长度为63帧，接招窗口为 `[14,44)`，移动退出窗口为 `[42,63)`。2026-09-27后续核对将原先移动第44帧开放改为第42帧，并将接招上限的 `<=44` 改为 `<44`；旧比较节点由正式生成清理。第42、43帧两个窗口同时有效，Control优先处理有效普攻接招。该修改已编译、生成Timeline并发布RushAttack的Fixed/Float32，尚无普通分支专项运行证据；当前角色配置的Badge_S01会选择强化分支，不能用强化回放证明普通分支窗口。

## 生成链

强化五段入口调用 `CorinRushTimelineAuthoringBuilder`，输入为状态身份、分支身份、AnimationClip、状态时长和 ActionWindow 区间。

- 动画轨道输出 `FullBodyAction`，slot 为 `corin.full-body-action`。
- 每个窗口有真实 TimelineBody 图、Frame Blackboard 投影和结束条件。
- `TimelineTime` 只在所属 TreeClip 内消费。
- 无窗口的状态不生成空逻辑轨道；重复生成清理旧空 Boundary 图。
- Timeline / Section / AnimationClip 播放身份沿用原 stable seed，窗口使用各自状态与窗口名生成身份。

2026-09-27 后续根位移修正：强化五段已增加正式 MotionCurveTrack，引用 `CorinActionMotion` 中从对应 MotionReference 的 Bip001 X/Z 提取的平面位移。Y 与旋转保留在原地动画表现中。强化循环动画登记 `FootPlacementWeight=0`，保留悬空腿姿；这次实际动画标量曲线变更已发布 ACL 并编译 Domain Resource Set，足部分析结果复用。运行证据见 `diagnostics/corin-action-runtime-20260927-180403.json`；不能以该回放成功代替悬空姿态和位移视觉验收。
