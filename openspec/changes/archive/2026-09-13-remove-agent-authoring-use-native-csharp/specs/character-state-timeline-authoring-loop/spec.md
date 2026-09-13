## MODIFIED Requirements

### Requirement: Corin生成产物必须显式重建

Corin迁移 MUST先用`AnimationClipAnalysisInputHash`与新Phase Validation Descriptor显式重建Foot Analysis Artifact，再通过正式作者API写入注册Curve、Profile、Pose Graph与Timeline；Curve写回 MUST不使该Artifact stale。正式authoring保存成功后，Presentation Projection、Float32 Program wrapper与Fixed Program wrapper MUST通过精确Definition的正式显式Build入口按依赖顺序重建。Program MUST不包含BaseLocomotion animation producer；Projection MUST包含PoseStateMachine、Clip/BlendSpace state-local source、Locomotion Phase endpoint、AnimationSlot、完整Rig v4与唯一ordered Pose Plan。产物 MUST共享匹配的source revision闭包，不得自动Build、部分发布或使用旧wrapper、Clip plan或Phase relation。

#### Scenario: 迁移后显式Build

- **WHEN** Corin新schema Foot Analysis已Ready且正式authoring保存成功
- **THEN** 作者 MUST显式触发Projection、Float32 Build与Fixed Build
- **AND** 任一阶段失败 MUST保留明确typed diagnostic且不得发布混合revision

## REMOVED Requirements

### Requirement: Corin资产迁移必须通过正式Agent Document事务

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“Corin资产迁移必须通过正式业务作者入口”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

## ADDED Requirements

### Requirement: Corin资产迁移必须通过正式业务作者入口

有限Action Timeline、Gameplay Graph、Blackboard、Presentation Binding、Pose Graph、Locomotion Sync Group、AnimationClip注册Curve与旧Locomotion数据清理 MUST通过各自正式类型化作者API完成，人工入口与C#入口 MUST共享同一业务约束。跨owner的同一次业务修改 MUST使用已有编辑事务的组合边界，明确引用、Undo与保存结果；普通单对象修改 MUST不依赖全角色Document或Agent事务。实现 MUST不直接修改Unity YAML、不恢复旧Patch链、不创建第二mutation service。作者修改 MUST只影响authoring及其生成物stale状态，不得自动Build。

#### Scenario: C#修改Corin相关资产

- **WHEN** 调用方明确指定Corin Definition及同一次需要修改的Clip、Profile、Pose Graph、Timeline与Gameplay owner
- **THEN** 正式作者入口 MUST按业务引用关系执行修改并提供一致的Undo与保存结果
- **AND** MUST不生成目录包、整包hash或要求反向导出Clean
- **AND** MUST不恢复Sequence、Marker、BaseLocomotion、ActionOverride或旧Selection字段
