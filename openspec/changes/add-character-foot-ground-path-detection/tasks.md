## 1. Ground Path合同

- [ ] 1.1 定义Ground Path Revision identity、输入、状态和typed rejection。
- [ ] 1.2 定义上一完成Sole与Accepted Landing组成的不可变Revision端点。
- [ ] 1.3 定义固定容量Raw Ground Contact与只读结果页。
- [ ] 1.4 为左右脚建立预分配Committed/Pending Ground Path页。

## 2. World Query模块

- [ ] 2.1 扩展Foot Placement查询请求以精确表达Capsule两个端点、半径、最大轴段长度、方向和距离。
- [ ] 2.2 定义不依赖Unity Physics类型的`ICharacterFootGroundPathWorldQuery`。
- [ ] 2.3 在唯一World Query Backend按最大轴段长度确定性切分完整Capsule轴。
- [ ] 2.4 对每个连续轴段执行实际Capsule Cast并写入同一固定容量结果页。
- [ ] 2.5 集中处理自身Collider、初始重叠、非法几何、重复命中与容量溢出。
- [ ] 2.6 对原始命中执行稳定canonical排序并保留分段索引、位置、法线、Surface与查询距离。

## 3. Foot Placement事务

- [ ] 3.1 从上一成功Seal的实际Sole建立左右脚Revision起点。
- [ ] 3.2 首次启动没有上一完成Frame时发布`OriginUnavailable`并在下一成功Frame后建立起点。
- [ ] 3.3 从同帧Accepted Landing与正式运动权威建立Revision终点和identity。
- [ ] 3.4 只在Revision变化或Rejected后的新authority tick执行Ground Detection。
- [ ] 3.5 将Ground Detection结果写入同一Foot Placement Pending Frame。
- [ ] 3.6 统一实现Seal、Discard、Reset、Retarget与Dispose的Ground Path状态处理。
- [ ] 3.7 保持Pelvis与双脚Goal权重为零并保持唯一FinalIK输入Pose不变。

## 4. 配置与发布

- [ ] 4.1 在Foot Placement Profile增加唯一Ground Detection配置并升级schema。
- [ ] 4.2 更新Profile revision、Projection依赖和发布校验。
- [ ] 4.3 更新Corin正式Profile并重新发布唯一Projection与Float32/Fixed角色产物。
- [ ] 4.4 删除Landing-only旧schema、旧字段名和任何默认补全路径。

## 5. 诊断

- [ ] 5.1 扩展Seal后只读摘要以发布Revision identity、Capsule请求、原始候选和typed rejection。
- [ ] 5.2 Scene Gizmo直接绘制完整Capsule起点、终点、半径、查询方向与距离，不重复绘制内部接缝。
- [ ] 5.3 Scene Gizmo绘制原始候选位置和法线，不显示文字、矩形鞋底、伪Path或Hull。
- [ ] 5.4 扩展现有Landing采样器以记录Ground Path Revision与原始候选事实。

## 6. 清理与统一

- [ ] 6.1 确认Runtime只通过Ground Path查询接口访问Unity适配器。
- [ ] 6.2 确认不存在第二Grounding、第二Pelvis、旧Plan、Anchor、Hull、fallback或FBBIK后处理引用。
- [ ] 6.3 确认Ground Path状态只随外层Presentation事务Seal或Discard。
