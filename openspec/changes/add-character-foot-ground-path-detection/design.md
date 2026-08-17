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

Capsule是收集世界位置和法线的查询体，不是鞋底，也不是最终包络。查询轴只表示上一已提交Landing到下一Accepted Landing的连线，不能作为算法结果。

## 2. 唯一数据链

```text
Current Step + Incoming Step
-> 新Landing Event的Future Body Translation
-> Raw Landing
-> 每个Landing Event一次Landing SphereCast
-> Committed Current Landing + Cached Next Landing
-> 分段Capsule Cast
-> Raw Ground Contacts
-> Ground Envelope Builder
-> Committed Ground Envelope
-> Seal后只读Diagnostics
```

每只脚按Landing Event identity缓存Accepted Landing。同一事件在整个脚步周期内不重复SphereCast；当前事件完成时，原Next Landing晋级为Committed Current Landing，再为新的Incoming Event查询一次Next Landing。Ground Path只消费这对稳定落点，逐帧Animated Sole不进入查询输入。

## 3. Ground Envelope算法

### 3.1 二维路径平面

Builder以`Component Up`和Path Start与Target Landing之间的水平纵向组成二维平面。所有有效接触位置投影为`路径距离 + 高度`，法线只在可用时投影为对应二维法线；法线不能使有效碰撞位置从候选集合消失。

路径水平长度退化、没有合法接触、固定容量溢出时发布不同typed rejection；不得输出Path Start到Target Landing直线作为替代。

### 3.2 排序与Edge Plane

接触按路径距离从近到远、同距离按高度从低到高、最后按稳定candidate identity排序。相邻接触分别以`位置 + 法线`定义二维地面平面；两平面交点只有位于两接触之间且高度也位于两者范围内时才成为边缘候选，否则保留原接触点。

同一路径距离只保留最高地面候选，但Path Start和Target Landing必须作为首尾端点保留。这样台阶竖边不会被误画成多个无意义并列点，同时包络不会脱离上一已提交落点与真实Next Landing。

### 3.3 高差处理

`CastAbove/CastBelow`只定义Capsule从路径上方向下的真实查询范围。碰撞点已经是该查询返回的世界事实，不能再用同一组查询高度把点判成不可达并删除；GDC 36页的上侧凸包仍然经过障碍顶部。高差由上侧凸包保留为连续包络几何，不生成第二个拒绝分支。

### 3.4 上侧Convex Hull

对全部候选按距离递增构造二维上侧单调链。新点加入时，所有会形成平直或向上凹陷的中间点被移除，只保留不会穿过任何候选地面的上侧转折点。结果是从Path Start到Target Landing的连续折线；它是feet-only地面下界，不是动画脚轨迹。

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
    输入: Accepted Landings + Raw Contacts
    输出: typed rejection或预分配Ground Envelope页

CharacterFootPlacementRuntime
    负责: 左右脚事务、Ground Path结果、唯一GoalSet

CharacterFootGroundPathGizmo
    只读: Seal后的Committed Current Landing、Cached Next Landing与最终Envelope
```

纯Builder不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型。Unity Backend不选择Step、不构造Hull、不写Goal。Gizmo不重新查询或重算算法。

## 6. Debug显示

- 绿色方框：上一已提交Accepted Landing。
- 黄色方框：下一Landing Event的Cached Accepted Landing。`n- 青色或粉色粗折线：当前事件对成功Seal的Ground Envelope。

不再绘制Current到Next中心直线、Capsule外框、扫掠边线、原始法线、文字、矩形鞋底或伪Path。

## 7. 非目标

- 不消费Animation Clearance，不生成最终Swing Foot Motion。
- 不做Foot Lock、Constraint、Anchor或Pelvis。
- 不写Foot/Pelvis Goal权重，不修改FinalIK。
- 不增加第二Grounding、第二查询算法、默认地面、fallback或兼容reader。
