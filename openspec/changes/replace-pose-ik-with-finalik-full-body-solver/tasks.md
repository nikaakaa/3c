## 1. 删除失败实现

- [x] 1.1 删除旧Predictive Planner、Plan、Query、Targets、Ground Envelope与WorldProjection实现。
- [x] 1.2 删除旧Revision、Successor、Continuity、LandingHandoff与Stance/Anchor/Pelvis实现。
- [x] 1.3 删除旧Predictive Profile/Tuning、Gizmo、CSV Capture、自动控制入口和专用Variant。
- [x] 1.4 保留Rig/Calibration、Sole几何、Surface、WorldQuery、Lyra Current Grounding基础、Pose/Feature输入和唯一FBBIK。
- [x] 1.5 清理共享Animation/Editor中的旧类型引用，使源码只留下新的FootPlacement装配入口。

## 2. 平地动画路线

- [ ] 2.1 定义不可变Biomechanical Step读取页，只发布一个Current与一个Incoming Step。
- [ ] 2.2 对每个可达Locomotion Clip执行Flat Reconstruction Gate。
- [ ] 2.3 同相位比较完整Animation Foot Route、Native Sole、Ankle与Landing端点。
- [ ] 2.4 绘制完整Animation Foot Route与当前权威相位点，不绘制文字或代表点伪路线。
- [ ] 2.5 平地实际Foot Motion必须沿Animation Foot Route执行且无owner闪断。

## 3. Body Trajectory与初始Plan

- [x] 3.1 从Simulation committed事实构造Body position/facing/linear/angular trajectory输入。
- [x] 3.2 用Biomechanical Step的`RootLocalLanding`与Body trajectory生成唯一Raw Landing候选。
- [x] 3.3 Landing必须通过正式Physics SphereCast取得可踩Surface，不得使用旋转旧命中或默认地面。
- [x] 3.4 为Landing投影、查询请求和无命中拒绝建立纯计算单元测试。
- [x] 3.5 以零权重Goal接回唯一FootPlacement事务，使角色可运行但不修改原动画骨骼。
- [x] 3.6 绘制Native Sole、Raw Landing、实际查询和Accepted/Rejected Landing，不显示文字或伪Path。

## 4. 转向重规划

- [ ] 4.1 删除所有旧Path/Surface/Hull的运行时刚体旋转或平移。
- [ ] 4.2 committed trajectory generation或有效方向改变时创建唯一Revision。
- [ ] 4.3 Revision重新执行Landing Cast、Capsule采样、Edge、Reachability和Hull。
- [ ] 4.4 Rejected Revision保留不可变旧Plan作为交接旧侧，但不得改写旧Plan或让Current Grounding接管Swing。
- [ ] 4.5 新Plan成功后从上一完成Final Goal只交接一次。

## 5. Ground Path与Foot Motion

- [ ] 5.1 按GDC顺序生成feet-only Ground Envelope。
- [ ] 5.2 Foot Rate只按权威Phase映射局部Route segment。
- [ ] 5.3 最终Swing保留动画XZ与旋转，Y使用Ground Path加动画Clearance。
- [ ] 5.4 Surface Point必须重心化到当前验证Sole，局部法线不得超出鞋底覆盖无限外推。

## 6. Landing、Lock与Pelvis

- [ ] 6.1 原子提交Landing Sole、Support、Surface local anchor与Plan/Event identity。
- [ ] 6.2 实现数据定义的Locked、Sliding、Unlocked持续状态，不按逐帧Contact布尔值重捕获。
- [ ] 6.3 Support Leg与Body Path生成唯一Pelvis目标，上下坡分别处理。
- [ ] 6.4 Foot与Pelvis在同一事务边界提交三个Final Goals。

## 7. 诊断与发布

- [ ] 7.1 重建最小因果CSV，逐阶段发布输入、结果和typed failure。
- [ ] 7.2 重建GameplayLab平地、直线楼梯和转向压力入口。
- [x] 7.3 完成Runtime/Editor编译、Float32/Fixed Character Build和OpenSpec strict validate。
