# Foot Placement数据链

本文记录正式运行数据如何进入一次Plan构建、如何进入唯一执行状态，以及诊断如何证明每一项事实来自哪里。它不定义第二条执行链。

## 唯一执行链

```text
Original Component Pose
+ Authoritative Step / Clock
+ Committed Body Motion
+ Current World Support
-> CharacterFootPlacementRuntime单帧事务
-> Foot Placement Final Goal Set
-> FinalIK FBBIK
```

Current Support只提供当前世界事实，Predictive Query只提供未来地形事实，FinalIK只执行最终Goal。任何Rejected候选都不得让Current Grounding在Swing中伪装预测结果。

## Plan构建请求

每次Initial、Intent Revision、Event Successor和Current Event Replacement只允许向Builder提交一个不可变`CharacterFootPlanBuildRequest`：

```text
Attempt kind / Foot side / Frame
Authoritative Step and Clock
Root / Body / Motion / Timeline / Component Up / Leg facts
Sole radius
CharacterFootPlanBuildOrigin
```

禁止调用方分别传入Sole、Ground Path和Support。它们必须先组成一个不可拆分的Origin：

```text
Origin kind
Source Plan Sequence / Landing Event identity
Sole Pose
Ground Path point
Support Surface / point / normal
Sole height above support
```

Ground Path由同一个Sole投影到同一个Support得到。任一项无效时整笔Attempt拒绝，Builder不得从另一个owner补字段。

## Origin来源规则

| Attempt | Origin |
|---|---|
| Initial | 当前Step仍由Stance约束时使用Committed Stance Support；否则使用Current Query Support |
| Intent Revision | Active Plan上一完成输出；不存在时使用Current Frame |
| Event Successor | Committed Landing；否则Landing Handoff；再否则使用已验证Projected Landing预建geometry |
| Current Event Replacement | Committed Landing；否则Current Query Support |

Current Event Replacement禁止继承未提交的旧Projected Landing。Projected Landing只属于原Plan对未来落点的承诺，不能与新事件路线拼成一笔不存在的事实。

`Current Query Support`与`Committed Stance Support`必须是两份显式不可变事实。前者只来自本帧唯一Current Query；后者只来自上一完成帧Anchor及其Surface。调用方不得用一个来源不明的`groundProbeStart + support`在两者之间隐式择优，也不得把旧Anchor标记成`CurrentFrameSupport`。进入Unlocked Swing后的Current Event Replacement不能再使用待释放旧Anchor作为起点。

## PlanAttempt观察合同

`PlanAttempt`只观察一次构建，不拥有Plan或脚状态。每次构建都必须发布：

```text
Attempt identity / kind / frame / event
Origin完整来源
Build state and typed reject reason
Ground Probe Route
Animation Foot Route
Ground Envelope / Clearance Path
Query requests / accepted supports / rejected geometry
Landing candidate and plan identity
```

进入Physics前失败时，几何序列为空但Origin和typed reason必须存在；进入Query后Rejected必须保留完整查询快照。成功Attempt帧输出该Attempt自己的几何，不能混入当时仍在执行的Active Plan。非Attempt帧才输出Active Plan几何。执行期Current Path、Action Progress和Ground Path Progress只在所选几何确属Active Plan时发布。

## 当前证据

run `9a7cf93abf044e7eb15d8eaa7eca491d`的左脚frame 197把新事件路线与旧Projected Landing拼接：查询从`Y=1.8m`开始，首个合法楼梯踏面是`Y=1.08m`，人为制造`-0.72m`并触发`StepExceeded`；该帧有120次请求和152个原始命中，Physics没有漏检。

重构后run `a8ebac33e0794600bc328f002481bd3e`共1059行、1279列，Header唯一且每行等宽。84次Current Event Replacement中，Projected Landing来源为0；每个Origin Ground Path都位于其Support Plane上。该run也暴露旧CSV观察链在成功Attempt帧仍输出Active Plan几何，因此schema v97进一步改为Attempt帧选择Attempt geometry，禁止把两份Plan写进同一组序列列。

schema v97回归run `f9335d62431949a59cab0943c898eaaa`共749行、1279列，Header唯一且每行等宽。173次PlanAttempt的Origin Ground Path与本次Attempt Ground Probe起点错配为0，Current Event Replacement使用Projected Landing为0，Origin Ground Path偏离其Support Plane为0。数据链原子性已可直接验收。

该run同时把下一处问题从混合数据中分离出来：右脚frame 363的Current Event Replacement以`CurrentFrameSupport`创建Origin，Sole Y为`1.267m`、Origin Support Y为`1.800m`，鞋底位于该支撑面下方`53.3cm`；Predictive Query首个合法踏面却是`1.080m`。进一步对账证明`Y=1.800m`来自Planner执行时尚未验证的上一帧旧Anchor，而本帧真正Current Query是Surface `-316164`、`Y=1.080m`；Stance随后才因Anchor距离`0.844m`将旧Anchor释放。也就是同一帧发生了“先用旧Anchor建Plan，后验证并释放旧Anchor”，且诊断把旧Anchor错误标成Current Frame。该错序直接制造`72cm`假台阶并触发`StepExceeded`，不是FinalIK、阈值或Physics漏检。

同一Event Successor还会在连续authority tick从同一个Projected Landing重复构建并重复得到`StepExceeded`；例如右脚frame 475至478使用同一来源Plan 97和同一Ground Path，连续生成Plan 99至102。支撑一致性修复后还要检查重试资格，禁止重复查询同一份未变化事实。

## 下一步诊断顺序

1. 先验证Attempt Sequence、Origin、Ground Probe和Landing属于同一Plan。
2. 再验证查询后的有向支撑链和Step/Gap拒绝是否基于真实相邻踏面。
3. 再验证Active、Revision、Successor和Landing Commit的原子换代。
4. 最后检查Goal、Pelvis和FBBIK结果。

如果上游身份尚未一致，不得通过放宽台阶阈值、增加平滑或让响应式Grounding接管Swing来掩盖。
