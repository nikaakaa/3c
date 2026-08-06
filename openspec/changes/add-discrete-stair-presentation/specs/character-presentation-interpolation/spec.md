## ADDED Requirements

### Requirement: 接地竖直不连续必须由Body Presentation唯一有界吸收

`CharacterBodyPresentationRuntime` MUST在正式Body target采样与最终`CharacterBodyPresentationFrame`发布之间唯一拥有接地竖直不连续响应。该响应 MUST由独立Presentation-owned有限阶段执行，只读取previous/current Body position、`GroundedBefore/After`、interval Tick identity、stream update、Reset sequence和显式`CharacterBodyPresentationProfile`设置；MUST不读取KCC Step diagnostics、Collision Artifact、Unity Collider、楼梯对象、Foot Placement、Animation、Camera或Network私有状态。

`CharacterBodyPresentationProfile` MUST显式选择`Direct`或`BoundedDiscontinuityCorrection` Grounded Vertical Response，并在有界模式提供正有限的Discontinuity Threshold、Half-life、Maximum Error与Settle Distance。该Mode与参数 MUST独立于`CharacterVisualTrajectoryMode`、Body Source Mode、Network Model、Camera capability和Actor identity；缺失、未知或非法配置 MUST拒绝runtime创建，不得使用默认fallback。

普通Append interval只有在`GroundedBefore/After`均为真、绝对Source Y差达到Threshold且没有发生Initialization、SelectedStream Reset或Committed Branch Replacement时 MAY分类为`Up`或`Down`接地竖直不连续。进入该interval时，竖直target MUST使用current Body endpoint Y，阶段 MUST从当前最终visible Y与vertical velocity建立唯一有界offset并按Half-life临界阻尼收敛。连续不连续interval到来时 MUST从当前visible状态重新定向同一状态，不得叠加第二条弹簧、固定时长队列或累计旧target offset。普通Grounded连续interval MUST直接重采样target并只回收已有残差；Airborne、Reset、branch replacement、runtime Reset与dispose MUST清除或明确重锚定接地竖直状态。

最终visible Y与vertical velocity MUST在同一个`CharacterBodyPresentationFrame`中发布并用于VisualRoot、Foot Placement与默认Camera。竖直offset、velocity、Kind、active、clamped与settled MAY进入只读diagnostics，但 MUST不写回WorldState、CharacterState、Body history、Snapshot、Hash、GameplayFact、PresentationCommand、Animation transaction或network packet。

#### Scenario: 角色跨上离散台阶

- **WHEN** 普通Body interval两端都Grounded且current Y比previous Y高出达到Threshold的距离
- **THEN** 阶段 MUST分类为`Up`并让Gameplay target立即保持current Body高度
- **AND** visible Y MUST从当前可见高度有界收敛到该target

#### Scenario: 前一个台阶尚未收敛又进入下一台阶

- **WHEN** 当前竖直offset仍active且新的合法接地竖直不连续interval到达
- **THEN** 阶段 MUST从当前visible Y与velocity重新定向唯一offset
- **AND** MUST不叠加第二条修正尾巴或让visible pose瞬间回到旧target

#### Scenario: 角色沿Ramp连续上升

- **WHEN** 相邻Grounded Body interval的Y差均低于Discontinuity Threshold
- **THEN** visible Y MUST继续直接消费连续target trajectory
- **AND** MUST不因启用有界竖直响应产生持续低通拖尾

#### Scenario: Grounded分支被Rollback替换

- **WHEN** Committed Branch Replacement改变当前Grounded target Y
- **THEN** 系统 MUST按既有branch replacement与Reset语义处理该变化
- **AND** MUST不把canonical分支差异分类为离散台阶

#### Scenario: 角色离地

- **WHEN** interval任一端Grounded为假
- **THEN** 接地竖直阶段 MUST清除旧台阶offset并停止分类
- **AND** Jump、fall与landing MUST继续消费正式Body target而不借用台阶修正

#### Scenario: 观察竖直修正

- **WHEN** 接地竖直offset尚未settle
- **THEN** diagnostics MUST显示interval identity、Kind、Source Y差、target Y、visible Y、offset、velocity与clamp状态
- **AND** diagnostics MUST不反向修改Follower或Body Runtime
