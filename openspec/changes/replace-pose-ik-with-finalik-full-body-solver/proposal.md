# Change: 重建唯一预测式Foot Placement到FinalIK链

## Why

现有预测Foot IK经过多轮局部修补后仍在平地闪烁，实际脚不沿可视化动画Foot Route运行，转向时还会把旧Path、旧Surface和旧Ground Envelope直接刚体旋转。楼梯上的穿透、浮空和跳变因此无法区分是动画路线、轨迹、查询、凸包、Landing还是权重错误。

继续保留该实现会让重做继续建立在不可信事实之上。本change先删除现有Predictive、Stance/Anchor/Pelvis与诊断实现，只保留确认稳定的输入、查询、几何和唯一FBBIK边界，再按GDC 2016顺序重建。

## What Changes

- 保留`Original Component Pose`、Foot Analysis输入、Rig/Calibration、Sole几何、WorldQuery、Lyra Current Grounding基础、FootPlacement事务入口和唯一FinalIK FBBIK。
- 删除现有Plan、WorldProjection、Future Body到Foot Route合成、Query/Hull、Revision、Successor、Continuity、LandingHandoff、Stance/Anchor/Pelvis、Gizmo、CSV与Predictive Profile实现。
- 平地第一验收只证明同一Action Phase下Animation Foot Route、Native Sole和最终Sole一致且连续。
- 角色有效转向不得旋转旧地形结果；必须创建Revision，重新计算Landing、重新Physics采样并重新构造Edge、Reachability和Ground Envelope。
- 新Plan完成前旧Plan只作为交接旧侧，不得被改写成新路径；Rejected不得由Current Grounding伪装成预测成功。

## Impact

- 当前工作区在重做实现接回前允许编译失败和资产Missing Script；不会用no-op、fallback或兼容adapter掩盖删除状态。
- Corin Foot Placement Profile、GameplayLab自动Foot IK Variant与旧Character产品必须在新实现完成后重新创建并精确Build。
- 唯一正式目标链保持：`FootPlacement -> optional Predictive Modifier -> FinalIK FBBIK`。
