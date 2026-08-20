# Design

## 1. 参考边界

本轮只采用原始参考和GDC 33–36页的顺序：

```text
Ground Detection
-> Near/Far、Bottom/Top排序
-> Normal验证与Edge Plane
-> Reachability
-> Convex Hull
-> Continuous Ground Envelope
```

Capsule是收集世界位置和法线的查询体，不是鞋底，也不是最终包络。查询轴只表示上一已提交Landing到下一Accepted Landing的连线，不能作为算法结果。

## 2. 唯一数据链

```text
Current Step + Incoming Step
-> 新Landing Event的Future Body Translation
-> Raw Landing
-> 每个有效表现帧一次且仅一次Landing SphereCast
-> LastLanding + NextSwingLanding
-> 分段Capsule Cast
-> Raw Ground Contacts
-> Ground Envelope Builder
-> Reachable Ground Path或Invalid Segment
-> Committed Ground Envelope
-> Seal后只读Diagnostics
```

每只脚只保存LastLanding与NextSwingLanding，并为当前NextSwingLanding保留事件首次Accepted点作为误差基准。PreSwing或Swing阶段每个有效表现帧继续执行一次且仅一次正式Landing SphereCast；新的权威Accepted预测超过更新死区时替换NextSwingLanding并重建Ground Path，小于死区时只复用当前路径，不能停止上游预测。实际落地后最新NextSwingLanding晋升为LastLanding。Ground Path只消费这对落点，逐帧Animated Sole不进入Ground Path查询输入。

## 3. Ground Envelope算法

### 3.1 二维路径平面

Builder以`Component Up`和Path Start与Target Landing之间的水平纵向组成二维平面。所有有效接触位置投影为`路径距离 + 高度`，法线只在可用时投影为对应二维法线；法线不能使有效碰撞位置从候选集合消失。

路径水平长度退化、没有合法接触、固定容量溢出时发布不同typed rejection；不得输出Path Start到Target Landing直线作为替代。

### 3.2 排序与Edge Plane

接触按路径距离从近到远、同距离按高度从低到高、最后按稳定candidate identity排序。相邻接触分别以`位置 + 法线`定义二维地面平面；两平面交点只有位于两接触之间且高度也位于两者范围内时才成为边缘候选，否则保留原接触点。

同一路径距离只保留最高地面候选，但Path Start和Target Landing必须作为首尾端点保留。这样台阶竖边不会被误画成多个无意义并列点，同时包络不会脱离LastLanding与真实NextSwingLanding。

### 3.3 Reachability与Invalid Segment

`CastAbove/CastBelow`只定义Capsule从路径上方向下的真实查询范围，不能兼任Reachability限值。Builder在同路径距离折叠为最高候选之前保留Edge的Bottom与Top事实，并计算：

```text
verticalDistance = abs(dot(edge.Top - edge.Bottom, ComponentUp))
```

正式Ground Path Profile增加`MaximumReachableVerticalEdge`。它表示当前角色步行动画允许脚跨过的最大离散垂直Edge，按世界米制配置并进入Profile revision与Query Identity。该值不复用KCC Step高度，因为Gameplay碰撞面与脚部表现面可以分离；也不直接使用腿长，因为腿的理论长度不等于当前动画能够自然跨过的台阶高度，Pelvis与Constraint在本阶段也尚未参与。

Builder必须检查全部有序Edge。任一Edge超过限值时，Ground Path发布`UnreachableEdge`，记录首个Invalid Segment索引、Bottom、Top、Vertical Distance与正式限值。Raw Contacts与Edge事实仍随当前Frame提交用于诊断，但Accepted Ground Envelope保持为空；不得删除障碍点后继续构造Hull，也不得沿用上一条Envelope。

GDC只明确“检查全部Edge垂直距离并标记大变化”，没有规定整条路径的失败语义。本项目选择让包含Invalid Segment的整条单脚Ground Path不可用于Foot Motion，因为LastLanding到NextSwingLanding之间必须是一条完整连续路线；发布部分可用路径会让脚在同一次Swing中途失去正式目标。

### 3.4 上侧Convex Hull

只有全部Edge通过Reachability后，才对候选按距离递增构造二维上侧单调链。新点加入时，所有会形成平直或向上凹陷的中间点被移除，只保留不会穿过任何候选地面的上侧转折点。结果是从Path Start到Target Landing的连续折线；它是feet-only地面下界，不是动画脚轨迹。

## 4. 事务与内存

每段Capsule Cast的Physics命中缓冲使用`SegmentHitCapacity`，整条路径的Raw Contact页、Envelope顶点页和Builder workspace使用独立的`ContactCapacity`；两者都按Profile预分配。

每只脚继续只有一个Committed Page和一个Pending Page。构建不创建List、数组或运行时诊断资产。Pending只能随外层Foot Placement Frame执行`Seal`或`Discard`，Reset、Retarget和Dispose统一清空双页与workspace。

查询结果只在当前Foot Placement事务和成功Seal后的诊断页中使用，不把旧结果变换成新地形事实。

## 5. 模块边界

```text
ICharacterFootGroundPathWorldQuery
    输入: Capsule请求
    输出: Raw Ground Contacts

CharacterFootGroundEnvelopeBuilder
    输入: Accepted Landings + Raw Contacts + Reachability限值
    输出: Invalid Segment或预分配Ground Envelope页

CharacterFootPlacementRuntime
    负责: 左右脚事务、Ground Path结果、唯一GoalSet

CharacterFootGroundPathGizmo
    只读: Seal后的LastLanding、NextSwingLanding与最终Envelope
```

纯Builder不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型。Unity Backend不选择Step、不构造Hull、不写Goal。Gizmo不重新查询或重算算法。

## 6. Debug显示

- 绿色方框：LastLanding。
- 黄色方框：NextSwingLanding。
- 青色或粉色粗折线：当前Swing脚成功Seal的Ground Envelope。
- 红色细线：Rejected Ground Path的首个Invalid Segment Bottom到Top。

不再绘制Current到Next中心直线、Capsule外框、扫掠边线、原始法线、文字、矩形鞋底或伪Path。Invalid Segment只来自成功Seal的只读诊断页，Gizmo不重新计算Reachability。

## 7. 用户可观察验收口径

- 平地和普通楼梯：全部Edge垂直距离不超过正式限值，Ground Envelope与现有结果一致，不增加拒绝抖动。
- 高墙或超过角色跨步能力的垂直面：Ground Path稳定发布`UnreachableEdge`，Scene只显示首个红色Invalid Segment，不显示Accepted Envelope。
- 同一Landing Event持续期间：下一预测超过死区时Scene线和Ground Path identity随新落点更新；预测位移处于死区内且其它输入不变时复用同一Committed结果，不因毫米级噪声逐帧切换Accepted与Rejected。
- CSV：记录全部Edge数量、首个Invalid Edge索引、Bottom/Top、Vertical Distance、正式限值与最终Ground Path状态，数值必须与Scene诊断来自同一Seal页。

## 8. 非目标

- 不消费Animation Clearance，不生成最终Swing Foot Motion。
- 不做Foot Lock、Constraint、Anchor或Pelvis。
- 不写Foot/Pelvis Goal权重，不修改FinalIK。
- 不用Reachability决定Gameplay/KCC能否移动。
- 不做Pelvis参与后的完整腿部可达域、主动水平踩点或落点重规划。
- 不增加第二Grounding、第二查询算法、默认地面、fallback或兼容reader。
