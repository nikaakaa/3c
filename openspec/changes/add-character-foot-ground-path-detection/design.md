# Design

## 1. 参考边界

本轮只采用原始参考和GDC 33–36页的顺序：

```text
Ground Detection
-> Near/Far、Bottom/Top排序
-> Normal验证与Edge Plane
-> Convex Hull
-> Continuous Ground Envelope
```

Capsule是收集世界位置和法线的查询体，不是鞋底，也不是最终包络。中心线只表示两次落点的连线，不能作为算法结果。

## 2. 唯一数据链

```text
Current Step + Incoming Step
-> 各自Future Body Translation
-> Current/Next Raw Landing
-> 两次Landing SphereCast
-> Current/Next Accepted Landing
-> 分段Capsule Cast
-> Raw Ground Contacts
-> Ground Envelope Builder
-> Committed Ground Envelope
-> Seal后只读Diagnostics
```

Ground Path只消费当前两次Accepted Landing和本次查询输入。输入变化时必须从新输入重新执行Landing与Ground Detection；旧查询结果不得旋转、平移或补洞。

## 3. Ground Envelope算法

### 3.1 二维路径平面

Builder以`Component Up`和两次落点之间的水平纵向组成二维平面。所有有效接触位置投影为`路径距离 + 高度`，法线只在可用时投影为对应二维法线；法线不能使有效碰撞位置从候选集合消失。

路径水平长度退化、没有合法接触、固定容量溢出时发布不同typed rejection；不得输出Current到Next直线作为替代。

### 3.2 排序与Edge Plane

接触按路径距离从近到远、同距离按高度从低到高、最后按稳定candidate identity排序。相邻接触分别以`位置 + 法线`定义二维地面平面；两平面交点只有位于两接触之间且高度也位于两者范围内时才成为边缘候选，否则保留原接触点。

同一路径距离只保留最高地面候选，但Current Landing和Next Landing必须作为首尾端点保留。这样台阶竖边不会被误画成多个无意义并列点，同时包络不会脱离两次真实落点。

### 3.3 高差处理

`CastAbove/CastBelow`只定义Capsule从路径上方向下的真实查询范围。碰撞点已经是该查询返回的世界事实，不能再用同一组查询高度把点判成不可达并删除；GDC 36页的上侧凸包仍然经过障碍顶部。高差由上侧凸包保留为连续包络几何，不生成第二个拒绝分支。

### 3.4 上侧Convex Hull

对全部候选按距离递增构造二维上侧单调链。新点加入时，所有会形成平直或向上凹陷的中间点被移除，只保留不会穿过任何候选地面的上侧转折点。结果是从Current Landing到Next Landing的连续折线；它是feet-only地面下界，不是动画脚轨迹。

## 4. 事务与内存

每只脚继续只有一个Committed Page和一个Pending Page。Raw Contacts、Envelope顶点和Builder workspace都按Profile命中容量预分配；构建不创建List、数组或运行时诊断资产。Pending只能随外层Foot Placement Frame执行`Seal`或`Discard`，Reset、Retarget和Dispose统一清空双页与workspace。

查询结果只在当前Foot Placement事务和成功Seal后的诊断页中使用，不把旧结果变换成新地形事实。

## 5. 模块边界

```text
ICharacterFootGroundPathWorldQuery
    输入: Capsule请求
    输出: Raw Ground Contacts

CharacterFootGroundEnvelopeBuilder
    输入: Accepted Landings + Raw Contacts
    输出: typed rejection或预分配Ground Envelope页

CharacterFootPlacementRuntime
    负责: 左右脚事务、Ground Path结果、唯一GoalSet

CharacterFootGroundPathGizmo
    只读: Seal后的两次Landing与最终Envelope
```

纯Builder不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型。Unity Backend不选择Step、不构造Hull、不写Goal。Gizmo不重新查询或重算算法。

## 6. Debug显示

- 绿色方框：Current Accepted Landing。
- 黄色方框：Next Accepted Landing。
- 青色或粉色粗折线：对应脚成功Seal的Ground Envelope。

不再绘制Current到Next中心直线、Capsule外框、扫掠边线、原始法线、文字、矩形鞋底或伪Path。

## 7. 非目标

- 不消费Animation Clearance，不生成最终Swing Foot Motion。
- 不做Foot Lock、Constraint、Anchor或Pelvis。
- 不写Foot/Pelvis Goal权重，不修改FinalIK。
- 不增加第二Grounding、第二查询算法、默认地面、fallback或兼容reader。
