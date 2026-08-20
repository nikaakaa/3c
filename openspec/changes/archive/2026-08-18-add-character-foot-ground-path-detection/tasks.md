## 1. Ground Path查询合同

- [x] 1.1 定义LastLanding到NextSwingLanding的Ground Path输入和输出。
- [x] 1.2 记录Landing Event、运动权威、Surface、位置、法线、Component Up和Profile事实。
- [x] 1.3 定义不依赖Unity类型的Capsule请求、Raw Contact和typed rejection。
- [x] 1.4 为左右脚建立预分配Committed/Pending Ground Path页。
- [x] 1.5 每脚维护稳定LastLanding与可实时更新的NextSwingLanding，实际落地时把最新Next晋升为Last。
- [x] 1.6 同一Landing Event每个有效表现帧只执行一次正式Landing SphereCast，更新死区内复用路径，超过死区时更新落点并重建路径。

## 2. Unity World Query Adapter

- [x] 2.1 按最大轴段长度确定性切分完整Capsule轴。
- [x] 2.2 每个连续轴段执行真实Capsule Cast。
- [x] 2.3 过滤自身Collider、初始重叠和非法几何。
- [x] 2.4 保留分段索引、Surface、位置、法线、查询距离和稳定identity。
- [x] 2.5 固定容量溢出与无接触发布不同rejection。

## 3. Ground Envelope模块

- [x] 3.1 新增不引用Unity Physics或Editor类型的独立Builder。
- [x] 3.2 把接触投影到脚步纵向与Component Up二维平面。
- [x] 3.3 按Near/Far、Bottom/Top和candidate identity稳定排序。
- [x] 3.4 验证二维法线并用相邻位置与法线定义Edge Plane。
- [x] 3.5 只保留位于相邻接触范围内的合法平面交点。
- [x] 3.6 同路径距离保留最高候选并强制保留Path Start与Target Landing端点。
- [x] 3.7 保持CastAbove/CastBelow只属于Capsule查询范围，不把查询高度当作Reachability限值。
- [x] 3.8 为退化、无合法接触和容量溢出发布既有typed rejection。
- [x] 3.9 计算二维上侧Convex Hull并输出连续Ground Envelope。
- [x] 3.10 预分配Builder workspace和Envelope顶点页。

## 4. Reachability与Invalid Segment

- [x] 4.1 在正式Ground Path Profile增加米制`MaximumReachableVerticalEdge`并删除高差永不拒绝的旧合同。
- [x] 4.2 在同路径距离折叠前建立保留Bottom、Top、位置和稳定identity的预分配Edge页。
- [x] 4.3 对全部Edge计算沿Component Up的垂直距离并与同一Profile限值比较。
- [x] 4.4 对首个超限Edge发布`UnreachableEdge`、Invalid Segment索引、Bottom、Top、Vertical Distance与限值。
- [x] 4.5 让Reachability限值进入Profile revision和Ground Path Query Identity，Identity不变时复用Committed结果。
- [x] 4.6 Invalid Segment存在时保留Raw Contacts与Edge诊断、清空Accepted Envelope且不执行Hull。
- [x] 4.7 删除绕过Invalid Segment继续构Hull、沿用旧Envelope或使用KCC Step高度补Reachability的路径。

## 5. Foot Placement事务

- [x] 5.1 在唯一Runtime中依次执行事件级Landing缓存、Last/NextSwing晋级、Ground Detection和Envelope Build。
- [x] 5.2 Raw Contacts与Envelope写入同一Pending Frame。
- [x] 5.3 Seal、Discard、Reset、Retarget和Dispose统一处理Ground Path状态。
- [x] 5.4 保持Pelvis与双脚Goal权重为零并保持FinalIK输入Pose不变。
- [x] 5.5 把Edge页、Invalid Segment和Reachability结果纳入现有左右脚Pending/Committed页与统一Seal/Discard。

## 6. Diagnostics与Gizmo

- [x] 6.1 Seal后只读摘要发布Raw Contacts与最终Envelope顶点。
- [x] 6.2 保留LastLanding与NextSwingLanding标记。
- [x] 6.3 使用左右脚不同颜色绘制最终Envelope粗折线。
- [x] 6.4 删除Current到Next中心直线和其它遮挡图形。
- [x] 6.5 采样器增加最终Envelope顶点列。
- [x] 6.6 只读摘要发布Edge数量、首个Invalid Segment和正式Reachability限值。
- [x] 6.7 Gizmo在Rejected时绘制首个红色Invalid Segment且不绘制伪Envelope。
- [x] 6.8 CSV记录Edge、Vertical Distance、限值和最终Ground Path状态，保持采样器只读。

## 7. 文档与校验

- [x] 7.1 更新proposal、design与spec，删除未由原始参考确定的额外规划层和版本层表述。
- [x] 7.2 执行Runtime与Editor项目编译。
- [x] 7.3 执行定向`git diff --check`。
- [x] 7.4 执行OpenSpec strict validate。
- [x] 7.5 刷新Unity脚本并检查Console，不运行外部Runtime、Editor编译或Unity batchmode。
- [x] 7.6 Reachability实现完成后执行定向`git diff --check`。
- [x] 7.7 Reachability实现完成后执行OpenSpec strict validate。
