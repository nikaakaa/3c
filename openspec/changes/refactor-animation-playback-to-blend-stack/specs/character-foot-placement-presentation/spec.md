## MODIFIED Requirements

### Requirement: Body与Presentation重置必须原子清除Foot Placement历史

Foot Placement Runtime MUST跟踪Body ResetSequence与FinalAnimationPoseFrame completion identity。Initialization、branch replacement、selected stream reset、Presentation Reset、dispose、RequireOutput缺失或显式非法pose MUST在应用新计划前清除历史并从当前最终动画pose重新锚定。正常slot CrossFade、Stored capture、Inertial rebase或Pose Graph空间composition只要completion声明连续 MUST不触发硬reset。

#### Scenario: Rollback替换Body分支

- **WHEN** Body ResetSequence增加
- **THEN** 两脚 MUST同帧释放旧锚点
- **AND** MUST不从旧锁点向新Body缓慢拉伸

#### Scenario: Action淡出回Base

- **WHEN** Body未reset且action slot/pose graph声明连续
- **THEN** Foot Placement MUST保留合法surface lifecycle
- **AND** policy weight MUST按最终AnimationFootPoseInput连续变化

#### Scenario: 最终Pose无效

- **WHEN** source/Stored/Inertial或Pose Graph产生非法值
- **THEN** Foot Placement MUST在写IK前reset并拒绝该frame
- **AND** MUST不使用上一帧pose掩盖错误

### Requirement: Foot Placement 必须提供统一诊断且保持热路径有界

Runtime diagnostics MUST只读暴露Body、Projection/Rig identity、AnimationChannelId、PoseSlotId、live/Stored/Inertial slot contribution、Pose Graph最终左右脚contribution、sample time、生成feature、support、constraint、surface、lock/replant、最终weight、pelvis、query与solver结果。Runtime MUST复用固定容量workspace；热路径 MUST不采样AnimationClip，不使用LINQ、反射、字符串查找、临时List或每帧托管分配。Diagnostics MUST不重新求值Stack、Pose Graph、query或solver。

#### Scenario: 排查CrossFade误释放

- **WHEN** 一只脚在多个slot/source贡献间进入Free
- **THEN** Debug MUST显示slot内部和Pose Graph最终每项权重
- **AND** 读取 MUST不改变下一帧状态

#### Scenario: 排查Inertial残差

- **WHEN** contact在Inertial期间变化
- **THEN** Debug MUST显示capture aggregate、target feature、foot progress与最终空间贡献
- **AND** MUST不把Inertial显示为伪producer

### Requirement: Preview 必须遵守正式世界上下文边界

Play Mode完整角色在具有Body、Rig、Profile和PhysicsScene时 MUST复用正式Foot Placement。纯动画Timeline Preview没有Body和scene query时 MUST不创建Foot Placement Runtime，也 MUST不生成默认平面或假Grounded。

#### Scenario: 纯动画预览Attack

- **WHEN** Timeline窗口只创建Animation Playback/Pose Graph Runtime
- **THEN** Preview MUST显示正式Pose Graph最终动画pose
- **AND** MUST明确Foot Placement不可用
