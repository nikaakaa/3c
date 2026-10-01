# 脚部诊断

当前未闭环问题与正式 owner 见[脚部进展](../../status/foot-placement.md)，修改、撤回与后续样本见[脚部阶段记录](../../archive/records/foot-placement/)。本目录只保存对应样本的证据，不合并不同版本的测量结果。

| 证据 | 内容与边界 |
| --- | --- |
| [台阶连续性解释器](ik-stair-continuity-explainer-20260930.html) | 采样、源码与失败实验的交互解释；候选回放启动失败不算 A/B 通过 |
| [接触起点候选离线筛选](contact-handoff-offline-20261001.json) | 两包 9,414 条脚记录中的 173 次交接代数比较，以及原录制释放／水平冻结的逐帧核对；候选已拒绝进入 Replay，完整业务运行仍为 0 帧 |
| [下坡响应](corin-downhill-response-20260927.json) | 下坡高度与响应采样 |
| [E 行走接触修正](corin-e-walk-contact-fix-20260927.json) | 当次接触修正证据 |
| [E 行走手动采样](corin-e-walk-manual-sampling-20260927.json) | 手动运行观察数据 |
| [查询边缘图](corin-foot-query-edge-20260927.svg) | 脚部地形查询几何解释 |
| [旧数据抖动定位](corin-jitter-existing-data-20260927.json) | 原姿势、求解输出与身体位移差异 |
| [抖动修正记录](corin-jitter-fix-20260927.json) | 抬脚修复有录制核对；混合旋转修复只有当时编译记录 |
| [脚部连续场景测试](ik-tests/README.md) | 接触交接与 Releasing 历史对照、来源哈希、正式运行结果和逐帧 HTML |

正式采样 CSV、Proof 和分析报告仍位于项目 `Diagnostics/`，通过报告内链接读取；没有改变采样包内部结构或身份。涉及混合旋转的证据由本目录保存唯一原件，并由[动画分类](../animation/README.md)引用。

## 后脚换面核对（2026-10-01）

本轮只读已有录制与正式源码，没有新建候选、修改运行代码、操作 Unity 或运行 Replay。用户描述的“平地走上台阶，后脚明显跳一下”尚未与某个精确录制帧绑定；下面区分已有的因果证据，不把相似表现当成同一根因。

| 历史片段 | 已确认的高度来源 | 当前处理状态 |
| --- | --- | --- |
| c75d 4452 左脚 | 当前支撑约 0.18 m，却被下一步包络额外抬高 12.6026 cm | `f37f8b4df` 已让末端包络服从选中的 SwingGround；当前源码保留，不能原样再修 |
| c75d 3175→3176 左脚 | Releasing 的 Toe 从 2.16 m 踏面跨入 2.34 m 踏面；OutputFootprint 补高 12.79 cm，真实踝升高 14.49 cm | 实际跨面后迟到补高的旧证据；不是下一步包络越权 |
| c28 3857→3858 左脚 | 身体不动，Toe 跨边后末端补高 12.9496 cm，真实踝升高 13.2697 cm | 与原动画局部踝仅升高 0.056 cm、微小求解残差对照，确认跳变先发生在目标层 |
| 65ec 4104→4105 | 前端标定脚掌与最终真实 Toe 的跨面时刻不同，随后真实踝升高约 9.83 cm | 必须同时查腿长可达性；前端探针通过不能代表最终骨骼或鞋网格通过 |

旧数据和方法分别保存在 [c75d](../../archive/records/foot-placement/corin-foot-c75d-walk-attack-20260928.md)、[c28](../../archive/records/foot-placement/corin-foot-c28-review-20260928.md)、[65ec](../../archive/records/foot-placement/corin-foot-sample-65ec-20260928.md)；当前接触控制权修复见[原修改记录](../../archive/records/foot-placement/corin-foot-floor-owner-fix-20260928.md)，已有插值前目标脚掌查询见[Landing 修复](../../archive/records/foot-placement/corin-foot-landing-target-20260928.md)。

### 新录制：后脚上移不等于末端补高

来源为 `20261001-064852-4bebfa0f077a433f8855ab35a62fc26a` 的 [full.csv](../../../3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20261001-064852-4bebfa0f077a433f8855ab35a62fc26a/character-foot-ik%252Ffull.csv)，SHA-256 为 `c57d09ded29f1ca93c017ef6b15ae55b24abe06a9efad90ec02cf02d7c43e8cd`。只做录制值差分，没有重新运行 C#、Physics、骨盆或 FBBIK。

右脚表现帧 `3789→3790` 对应 Completion `3783→3784`、采样行身份 `2094→2096`。Body 上移，脚相对身体沿记录前进速度方向位于后方约 44.97 cm；状态一直为 Releasing，作者及输出位置权重均为 1，Action 脚贡献为 0。

| 同一世界坐标阶段 | 前帧 Y（m） | 本帧 Y（m） | 增量（cm） |
| --- | --- | --- | --- |
| 已提交 Body | 0.692263544 | 0.698070600 | +0.580706 |
| FootPlacement 输入原动画踝 | 0.939805746 | 0.976121700 | +3.631595 |
| FootPlacement 输入原脚底 | 0.889779800 | 0.931129456 | +4.134966 |
| 最终有效竖直修正 | 0.161829233 | 0.167903185 | +0.607395 |
| 最终物理踝 | 1.101634740 | 1.144024730 | +4.238999 |

该帧 `SafetyFloorOwner=OutputFootprint`，但 clamp 为 0，插值输出与最终修正一致。状态目标修正反而从 0.194466114 降至 0.189683855 m；Releasing 仍在追赶较高目标，响应修正增加约 0.6074 cm。当前脚旋转权重为 0 时，原踝增量加修正增量重建物理踝增量，差小于 0.0001 mm。这里的 4.134966 cm 是原脚底增量，不能写成原踝增量。以上只排除该帧由末端额外补高引起，不证明原动画异常，也不证明用户所指现象已经修复。

### 新录制：两次重新接入已经清空旧历史

同包右脚表现帧 `3194`（Completion 3188）和 `3470`（Completion 3464）发生输出位置权重 0→1。两段作者权重始终为 1；前帧 Grounded=false、OwnershipLossReason=Ungrounded，当前帧恢复 Grounded=true。两次接入的前态均为 `HasOutput=false`、`HasPreviousResponseOutputPoint=false`，前帧最终有效修正为 0。

| 恢复表现帧 | 新状态目标竖直修正 | 本帧末端补高 | 有效脚底单帧上移 | 物理踝单帧上移 |
| --- | --- | --- | --- | --- |
| 3194 右脚 | 20.468760 cm | 0 | 20.958624 cm | 20.733800 cm |
| 3470 右脚 | 20.674658 cm | 0 | 20.681286 cm | 20.612773 cm |

源码链为 `Ungrounded 退出/清历史 → 新 Grounded 帧选 SwingGround → Interpolation 无输出历史时采用 SwingCorrection → 响应初始化 → BuildRequest 按作者权重 1 输出`。录制的响应初始化标记为 true，初始化原因保留 PolicyExited。首次大修正已在 StateTarget／Interpolation 存在，未由 OutputFootprint 新增。这与 [2dad 的异常旧历史重入](../../archive/records/foot-placement/corin-foot-2dad-review-20260928.md)有不同前态，不能据相似的 0→1 外观直接套用旧结论。新目标正确性及重新接入的连续规则仍未修复。

### 下一笔代码的具体入口与限制

当前 `Lifecycle.Resolve` 已执行 `StateTarget → QueryFootSupport/ConstrainStateTarget → Interpolation → ResolveContactClearance → 输出脚掌查询/约束 → ApplyOutputCorrection`；没有缺失整个目标查询或历史回写阶段。真正缺口是前后脚姿态的跨边推进：当前以目标位置所需高度决定是否冻结水平，末端又按实际响应后姿态补高，冻结位置再成为下一帧及新接触的历史。

下一笔应在这条唯一链中完成脚掌平移、旋转、净空和接触参考的连续交接，再与同一帧双脚骨盆后的腿长余量一起收口。旧接触仍支撑低面时不能被下一步高面抬走；真实脚端跨边前须具备净空；受限后的位移不能在解冻帧一次追回。具体联合推进实现尚未完成，不以这段要求冒充已验证算法。

原 `12571～12585` 连续反例仍必须保留：旧版硬抬 6.456 cm；当前冻结后腿长需求达 115.625%；提前放行候选的实际恢复步长增至 26.435 cm。另保留 Landing 旋转跨边、当前新接触承接滞后和上述重新着地接入。完整对照应按实际事件及整段极值判断，先离线筛选，再进入已有完整业务入口，合格后交用户 Replay；不新增测试系统。

本轮文档 diff、UTF-8／CRLF、HTML 唯一 ID、新增引用及全部内嵌 JavaScript／JSON 静态检查通过；原有交互脚本内容保持一致。浏览器启动命令被自动审批以 `blocked by policy` 拒绝，未返回更具体原因，因此新增区的浏览器视觉检查未执行；先前页面的检查不作为本次新增区的视觉验证。运行代码未改，未执行编译或新的业务运行。
