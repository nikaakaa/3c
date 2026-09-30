## MODIFIED Requirements

### Requirement: 已有动画数学与阶段语义必须保持

既有动画计算的迁移 MUST保持Unity原XZ水平平面、速度/方向零值分支、角度计算、加速度历史及原阈值。MotionPhase MUST保持原四个类型化枚举值；地面分类使用原HasMotion或speed>0.0001条件，空中分类使用原VerticalSpeed>0条件。系统 MUST不照抄UE示例阈值，不在迁移已有计算时夹带步频、播放倍率、平滑或状态策略变更。显式新增的 Corin 跑动 lean MUST使用独立声明的参数与历史表达其公式和平滑，MUST不改变原七项动画派生值、状态判定或其消费者。

#### Scenario: 原地面移动分类
- **WHEN** Grounded为true
- **THEN** 图 MUST按原HasMotion或水平速度大于0.0001的规则产出GroundedMoving或GroundedStationary
- **AND** MUST不改变现有动作选择语义

#### Scenario: 原空中分类
- **WHEN** Grounded为false
- **THEN** 图 MUST在VerticalSpeed>0时产出AirborneRising，否则产出AirborneFalling

#### Scenario: 增加侧倾计算
- **WHEN** Corin 动画更新图增加跑动侧倾、平滑和 Quaternion 输出
- **THEN** 原七项计算 MUST保持原值与原更新顺序，新增输出使用独立变量
- **AND** MUST不把 HorizontalAcceleration 或 FacingError 改成另一种含义供 lean 使用

## ADDED Requirements

### Requirement: 新增侧倾历史必须归动画实例并遵守原更新事务

lean 的方向历史、有效性和当前倾角 MUST归当前动画图实例所有，在一次正式更新内使用上次历史计算并最后保存本次历史。侧倾 MUST使用宿主 delta 和正式只读事实，发布同一变量帧。EventGraph 更新成功后 Source Pending MUST继续保留已成功事件状态，不由 Pose 回退或重复推进；Reset、身体不连续及图替换 MUST与现有历史一并重置。

#### Scenario: 两个角色共享作者图
- **WHEN** 两个角色使用相同作者图但一个左转一个右转
- **THEN** 两个实例 MUST拥有独立的方向历史和倾角，输出互不干扰

#### Scenario: 同帧多处读取侧倾
- **WHEN** 多个姿势节点在同次求值读取侧倾旋转
- **THEN** MUST读取相同的已发布 Quaternion，不再次计算平滑或推进方向历史
