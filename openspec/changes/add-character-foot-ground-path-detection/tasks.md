## 1. Ground Path查询合同

- [x] 1.1 定义Current/Next Accepted Landing到Ground Path的输入和输出。
- [x] 1.2 记录Landing Event、运动权威、Surface、位置、法线、Component Up和Profile事实。
- [x] 1.3 定义不依赖Unity类型的Capsule请求、Raw Contact和typed rejection。
- [x] 1.4 为左右脚建立预分配Committed/Pending Ground Path页。
- [x] 1.5 在同一Foot Placement事务内复用已准备结果，输入变化时重新查询。

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
- [x] 3.6 同路径距离保留最高候选并强制保留两次Landing端点。
- [x] 3.7 使用CastAbove/CastBelow检查边缘和整条路径的竖直可达性。
- [x] 3.8 为不可达、退化、无合法接触和容量溢出发布typed rejection。
- [x] 3.9 计算二维上侧Convex Hull并输出连续Ground Envelope。
- [x] 3.10 预分配Builder workspace和Envelope顶点页。

## 4. Foot Placement事务

- [x] 4.1 在唯一Runtime中依次执行两次Landing、Ground Detection和Envelope Build。
- [x] 4.2 Raw Contacts与Envelope写入同一Pending Frame。
- [x] 4.3 Seal、Discard、Reset、Retarget和Dispose统一处理Ground Path状态。
- [x] 4.4 保持Pelvis与双脚Goal权重为零并保持FinalIK输入Pose不变。

## 5. Diagnostics与Gizmo

- [x] 5.1 Seal后只读摘要发布Raw Contacts与最终Envelope顶点。
- [x] 5.2 保留绿色Current Landing和黄色Next Landing标记。
- [x] 5.3 使用左右脚不同颜色绘制最终Envelope粗折线。
- [x] 5.4 删除Current到Next中心直线和其它遮挡图形。
- [x] 5.5 采样器增加最终Envelope顶点列。

## 6. 文档与校验

- [x] 6.1 更新proposal、design与spec，删除未由原始参考确定的额外规划层和版本层表述。
- [x] 6.2 执行Runtime与Editor项目编译。
- [x] 6.3 执行定向`git diff --check`。
- [x] 6.4 执行OpenSpec strict validate。
