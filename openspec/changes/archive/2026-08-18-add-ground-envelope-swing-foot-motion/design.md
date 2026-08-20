# Design

## 1. 参考边界

本轮落实GDC第11页Predictive Foot Motion，并建立在第31与33-36页完整Ground Path结果之上。第11页的正式约束是：左右脚独立，脚的前进来自动画，动画高度是Foot Path之上的高度，最终脚不得低于Foot Path。

仓库参考总结把Foot Path明确为相对两次落点基线的增量路线，因此本轮保持以下组合：

```text
Raw Contacts
-> Edge
-> Reachability
-> Convex Hull
-> Accepted Ground Envelope
+ Animation Foot Motion
-> Swing Foot Goal
```

Ground Envelope是feet-only地形下界，不是最终脚轨迹。最终脚仍保留原生动画的水平运动、抬脚节奏和旋转；本轮只把包络相对落点基线的垂直增量叠加到原生动画脚。

## 2. 唯一数据链

```text
同帧Original Component Pose
-> Original Animated Ankle / Heel / Toe / Sole

LastLanding + NextSwingLanding
-> 全部Edge通过Reachability的Accepted Ground Envelope

Current authoritative Swing Step
+ animation.foot-placement-weight
+ Component Up
-> Swing Foot Motion Builder
-> Corrected Sole / Corrected Ankle
-> FootPlacement Final Goal Set
-> 唯一FinalIK FBBIK
-> Final Component Pose
```

Foot Placement仍是唯一Goal owner。Builder不查询世界、不读取Transform、不调用FinalIK；FinalIK不选择Step、不采样Ground Envelope、不保存Foot Motion状态。

## 3. Swing准入

每只脚独立判断是否拥有当前Foot Motion。只有同时满足以下事实才可输出非零位置权重：

- Current Step权威且`IsSwing`。
- Current Step的Landing Event identity与该脚`NextSwingLanding`一致。
- Ground Path为Accepted、全部Edge通过Reachability且首尾Landing Event与当前缓存一致。
- Ground Envelope至少包含匹配LastLanding与NextSwingLanding的两个有序端点。
- `animation.foot-placement-weight`在`[0,1]`内。

同一脚的`NextSwingLanding`在同一Landing Event identity下，新的成功预测超过正式更新死区时才更新落点并重建Ground Path。毫米级变化不换路径。当前阶段全部Edge按可走处理，预测点漂移不得降低Position Weight。不可走仍只由Ground Path的`UnreachableEdge`等typed rejection拒绝。

PreSwing只允许提前建立Landing与Ground Envelope，不提前移动脚。支撑脚、Landing完成帧、`UnreachableEdge`、其它Ground Path Rejected或身份不一致时，Goal保持原生Ankle与原始旋转，但位置和旋转权重都为零；不得沿用上一帧有效Goal。

Walk通常只有一只脚满足Swing准入。Runtime不增加全局“左右二选一”启发式；如果未来合法动画出现双脚同时Swing，两只脚仍按各自权威Step与Ground Path独立求值。

## 4. 路径进度与Envelope采样

以`Component Up`归一化为`up`，把`NextSwingLanding - LastLanding`投影到垂直于`up`的平面，得到纵向`forward`与水平长度`pathLength`。

当前原生动画Sole中心投影到该轴：

```text
distance = clamp(dot(projectOnPlane(originalSole - LastLanding, up), forward), 0, pathLength)
progress = distance / pathLength
```

Builder在Ground Envelope相邻顶点中寻找覆盖`distance`的线段并线性采样`envelopePoint`。同时在两个Landing端点间按同一`progress`采样`baselinePoint`。

Envelope端点、纵向距离或顶点顺序非法时发布typed Foot Motion rejection，不输出旧Goal或直线替代Envelope。

## 5. 保留动画并计算垂直增量

原生动画离基线的高度为：

```text
animationHeight = dot(originalSole - baselinePoint, up)
```

最终脚底高度为：

```text
finalSoleHeight = dot(envelopePoint, up) + animationHeight
```

因此实际地形修正为：

```text
verticalCorrection = dot(envelopePoint - baselinePoint, up)
correctedSole = originalSole + up * verticalCorrection
correctedAnkle = originalAnkle + up * verticalCorrection
```

Ground Envelope是两个端点之上的上侧Hull，因此`verticalCorrection`应为非负。负值超过数值容差表示Envelope合同失效并发布typed rejection；正值不超过同一几何容差时归零，避免接触采样浮动启动一次完整的FBBIK求解。

该公式不把NextSwingLanding直接当Ankle，不改变脚的水平位置，也不覆盖动画自身抬脚高度。Heel、Toe和Ankle使用同一个平移量，本轮不修改旋转。

## 6. Goal与FinalIK

Foot Goal保持现有`FootPlacementEffectorTarget` ABI。只有`verticalCorrection`严格大于几何容差且现有Foot Placement Weight大于零时才形成有效位置Goal：

```text
ComponentPosition = PoseRoot.InverseTransformPoint(correctedAnkle)
ComponentRotation = 原生Ankle Component Rotation
PositionWeight = verticalCorrection > geometryTolerance
    ? animation.foot-placement-weight
    : 0
RotationWeight = 0
```

`swingPhase`由当前Step的`EventPhase`在`LiftOffPhase`到`LandingPhase`之间归一化，不保存跨帧状态。未通过Swing准入时，Component Position与Rotation仍记录原生Ankle事实，但两个权重为零。通过准入但Ground Envelope与Landing基线重合时，Foot Motion保持Accepted、Vertical Correction为零、Goal位置保持原生Ankle且两个权重为零。Pelvis Goal始终为零权重。

FinalIK继续只做GoalSet到Pose的Adapter。它按现有lineage验证后执行一次FBBIK，不查询Ground Envelope、不补目标、不平滑目标，也不在求解后修脚。

## 7. 事务与状态

Swing Foot Motion使用当前Foot Placement Frame中已经通过Reachability的Accepted Ground Path页：新查询结果位于Pending页，身份相同的结果复用Committed页。Foot Motion结果、三个Goal与diagnostics在同一Frame中产生，并随外层Presentation事务一起`Seal`或`Discard`。

本轮计算是单帧纯函数，不新增跨帧Foot Motion缓存、Plan、Revision、Spring或Goal history；Landing事实只在同一Landing Event内保持首次Accepted点作为误差基准，当前NextSwingLanding可按上游死区规则实时更新。失败帧直接发布typed rejection和零权重Goal，不能用上一帧结果补洞。

## 8. 可观察结果

成功Seal后的每脚diagnostics固定记录：

- Foot Motion State与typed Reject Reason。
- Landing Event identity与Ground Path identity。
- 路径distance与progress。
- Original Animated Sole与Ankle。
- Baseline Sample与Envelope Sample。
- Vertical Correction。
- Corrected Sole与最终Component Ankle Goal。
- 实际Position Weight与Rotation Weight。

Foot Motion Accepted只表示输入合同和Ground Path有效，不等于必须执行FBBIK。平地零增量时摘要保留Accepted与零权重，使用户能够区分“有效但无需修正”和“合同失败”。

Scene Gizmo只读取该摘要：

- 白色小标记表示Original Animated Sole。
- 左脚青色、右脚粉色的小标记表示Corrected Sole。
- 两者之间使用细线表示实际垂直修正。
- Active Swing失败时在Original Animated Sole位置显示红色线框标记。

现有LastLanding、NextSwingLanding与Ground Envelope继续显示。上游`UnreachableEdge`的红色Invalid Segment继续来自Ground Path只读摘要，Foot Motion不得复制或重算它。Gizmo不显示文字，不读取Transform反推结果，不采样Envelope，也不执行world query。

CSV采样器记录同一摘要，使用户可以确认：

```text
CorrectedAnkle - OriginalAnkle
```

只沿`Component Up`，Ground Path失败或包含Invalid Segment时权重为零，Pelvis权重始终为零。

## 9. 实施顺序与用户可观察验收

### 9.1 先完成上游Reachability

- 普通楼梯的每个Edge低于正式限值，仍显示完整左右脚Ground Envelope，脚和Pelvis保持原动画。
- 超过正式限值的垂直面稳定显示红色Invalid Segment，不显示Accepted Envelope，不在同一Landing Event内反复横跳。
- CSV中的Invalid Edge、Vertical Distance和限值与Scene显示来自同一Seal页。

### 9.2 再实施本change的Foot Motion

- 平地：Original Sole与Corrected Sole重合，Vertical Correction与Position Weight为零，FullBodyIK验证Goal lineage后跳过FBBIK Update。
- 普通楼梯：当前Swing脚的Corrected Sole只沿Component Up高于Original Sole，细线长度逐值等于CSV的Vertical Correction。
- 连续按A转圈：同一Landing Event内Ground Path identity不变时，Envelope采样与Foot Goal不换代。
- Invalid Segment：该脚只显示Original Sole失败标记，Position Weight为零，不沿用上一帧Corrected Sole。
- 支撑脚与Pelvis：Goal权重始终为零，用户看到的变化只能来自当前Swing脚位置Goal。

### 9.3 用户验证唯一方法

1. 在平地记录Foot Landing CSV，确认Swing脚Foot Motion为Accepted、Vertical Correction和Final Goal Position Weight为零；FinalIK不得因本change产生额外求解。
2. 在普通楼梯记录同一Landing Event，确认Ground Path identity保持不变，Envelope Sample减Baseline Sample的Component Up分量大于零，Corrected Ankle减Original Ankle逐值等于该分量，最终Foot Goal Position Weight按当前Swing phase从零连续进入并最终受`animation.foot-placement-weight`上限约束。
3. 同时启用现有FootPlacement Goal Target Watch与FullBodyIK Pose Watch，确认二者Frame、Completion和Rig lineage一致；再检查CSV中的FinalPhysicalWriteCompletionIdentity、FinalPhysicalAnkleComponentPosition和FinalPhysicalAnkleGoalResidual，确认唯一final writer写入后的物理脚踝位置与Goal关系。Scene Gizmo仍只证明Goal事实，不能单独证明骨骼结果。
4. 对高墙确认Ground Path为`UnreachableEdge`、Foot Motion为Rejected、Goal权重为零，并确认同帧FullBodyIK脚部输出没有消费上一帧Goal。

## 10. 参考后续阶段

本change完成GDC第11页的Predictive Foot Motion后，参考顺序如下：

1. Foot Locking：按第13-16页建立数据定义的Locked、Sliding、Unlocked约束场景，位置锁定与旋转自由度分开表达。
2. Hips稳定：按第17页选择支撑腿，上坡与下坡采用不同高度策略，并由唯一Pelvis owner使用临界弹簧去除身体弹跳。
3. Foot Orientation：按第19页根据移动朝向处理上下坡pitch，并限制pitch/roll；跑步可由正式数据关闭，不写状态分支。
4. 转向支点：按第21-28页让身体转向围绕更接近接触脚的Pivot，避免锁脚时的腿扭曲、身体失衡和脚穿透。

这些阶段依赖当前Foot Motion和最终骨骼消费已被实机确认后再分别立项，不得提前把Lock、Spring、Pelvis或Pivot塞进本Builder。

## 11. 取舍

### 11.1 采用垂直增量，不直接追逐NextSwingLanding

直接把NextSwingLanding作为Foot Goal会在Swing开始时把脚拉向未来落点，覆盖动画步距和抬脚节奏。垂直增量只解决地形穿透，用户能单独判断Ground Envelope是否正确驱动Pose。

### 11.2 不替换完整三维动画路线

完整三维Foot Route可以主动调整水平踩点，但会同时引入路线时间映射、横向校正和事件换代问题。本轮保持动画水平位置，业务代价是暂不解决脚踩在台阶边缘或横向绕障。

### 11.3 不增加Spring或平滑

Spring可以让错误目标看起来不那么突兀，却会隐藏包络采样与事件身份问题，并引入新的跨帧状态。本轮先让输入与输出一一对应；连续性在后续Foot Constraint与Pelvis阶段单独设计。

### 11.4 不同时做Pelvis

Pelvis需要支撑脚、腿长、Constraint状态和双脚最终目标。当前同时移动Pelvis会让用户无法区分脚目标错误与身体支撑错误。代价是本轮上楼时可能看到腿被压缩或拉伸，这属于明确的阶段边界。

## 12. 非目标

- 不做Pelvis高度或旋转修正。
- 不做Foot Lock、Sliding、Constraint或Anchor。
- 不做脚底法线旋转、Heel/Toe Pivot或Turn Pivot。
- 不在本change重复实现Reachability；只消费上游唯一Reachability结果。
- 不做主动水平踩点或台阶中心校正。
- 不做Spring、滤波、惯性化或Goal历史平滑。
- 不增加第二Grounding、第二Goal owner、fallback、兼容路径或FBBIK后处理。
