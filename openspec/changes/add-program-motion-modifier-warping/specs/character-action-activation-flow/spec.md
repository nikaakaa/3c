## ADDED Requirements

### Requirement: ActionProfile 必须类型化声明目标快照要求

ActionProfile MUST使用`ActionTargetRequirement`明确声明`None`或`SnapshotRequired`，MUST不使用自由字符串TargetPolicy。Action catalog、Semantic IR和两个Numeric Target MUST保存同一typed值。未知值、缺失值或配置MotionWarp却声明`None` MUST在artifact发布前失败。

#### Scenario: 普通无目标闪避

- **WHEN** Dodge ActionProfile声明`None`
- **THEN** admission MAY在没有target snapshot时成功
- **AND** 该动作 MUST不包含需要目标的MotionWarp

#### Scenario: 目标攻击缺少快照

- **WHEN** ActionProfile声明`SnapshotRequired`
- **AND** candidate target snapshot为None
- **THEN** admission MUST返回`TargetSnapshotRequired`或等价typed原因
- **AND** MUST不创建ActionInstance或启动Timeline

### Requirement: 动作准入查询与提交必须读取同一目标候选

`CanActivateAction`与`ActivateActionInstance` MUST把同一显式Blackboard ActionTargetSnapshot或显式None传入唯一portable admission evaluator。纯查询与最终提交 MUST对目标要求得到相同结果；系统 MUST不允许查询忽略目标而提交阶段再失败，也 MUST不在激活后从scene补查目标。

#### Scenario: Transition 查询通过后激活动作

- **WHEN** transition条件使用CanActivateAction检查target-required动作
- **AND** target snapshot在同一准入输入中有效
- **THEN** 最终ActivateActionInstance MUST使用同一候选快照
- **AND** 创建的ActionInstance MUST固定保存该快照

### Requirement: MotionWarp 必须消费 ActionInstance 的固定目标快照

MotionWarp MUST只读取显式Action Context对应ActionInstance在激活时保存的target snapshot。运行期间target实体移动、消失或Presentation更新 MUST不改变该ActionInstance的Warp目标。MotionWarp MUST不按TargetId查询Transform、scene registry、Network Model或其它ambient状态。

#### Scenario: 目标在动作期间移动

- **WHEN** ActionInstance已经捕获target snapshot
- **AND** 目标实体随后移动
- **THEN** 当前动作的MotionWarp MUST继续使用已捕获pose
- **AND** 新目标位置只 MAY由后续正式Action activation重新捕获
