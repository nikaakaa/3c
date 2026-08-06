## ADDED Requirements

### Requirement: 连续楼梯必须显式分离Gameplay Traversal与Foot Placement Surface

每段从Lower到Upper单调上升的连续梯段 MUST通过唯一`StairTraversalSurfaceAuthoring`声明一个Gameplay Traversal Ramp和一个Foot Placement Surface根。包含上行、顶平台和下行的往返路线 MUST把上行与下行拆成两个绑定，顶平台 MUST继续作为普通`Ground`；Gameplay Traversal Ramp MUST是对应梯段进入Deterministic Collision Artifact的唯一连续通行表面。真实踏面Collider MUST只供Foot Placement查询，不得进入该Artifact。同一梯段 MUST不保留逐级Gameplay Collider、Ramp失败后的Step fallback或按运行状态切换表面的模式。

#### Scenario: 角色跑上连续楼梯

- **WHEN** Fixed KCC沿该楼梯移动且Foot Placement查询当前与未来落面
- **THEN** Fixed KCC MUST只接触Gameplay Traversal Ramp
- **AND** Foot Placement MUST只从真实踏面与普通共享地面获得support

### Requirement: 楼梯作者绑定必须拥有稳定身份和完整引用

`StairTraversalSurfaceAuthoring` MUST显式保存非空且场景内唯一的Stair identity、唯一Traversal Ramp Collider、唯一Foot Surface根、Lower Transition与Upper Transition。全部引用 MUST属于同一Scene或Prefab作者上下文且保持持久化；系统 MUST不按名称、Tag、相邻层级、Renderer bounds或最近Collider猜测缺失引用。

#### Scenario: 楼梯缺少上端过渡引用

- **WHEN** 作者请求校验或Bake该楼梯
- **THEN** 系统 MUST报告对应Stair identity和缺失字段并阻止操作
- **AND** MUST不从最高踏面或最近平台自动猜测Upper Transition

### Requirement: 楼梯表面角色必须通过Layer与作者所有权双重隔离

普通共享地面 MUST使用`Ground`层，并 MAY同时被Deterministic Surface作者拥有和Foot Placement查询。Traversal Ramp MUST使用`CharacterTraversal`层、被且只被一个`DeterministicCollisionSurfaceAuthoring`拥有，并 MUST被Foot Placement查询Mask排除。真实踏面Collider MUST使用`FootPlacementSurface`层、被Foot Placement查询Mask包含，并 MUST不被任何`DeterministicCollisionSurfaceAuthoring`拥有。Layer MUST不替代Deterministic Surface作者所有权，作者所有权 MUST不替代Foot Placement LayerMask。

#### Scenario: 真实踏面仍位于Deterministic Surface作者子树

- **WHEN** 楼梯validator发现Foot Surface Collider会被Collision Baker收集
- **THEN** validator MUST阻止Bake并报告Collider与冲突作者
- **AND** MUST不通过临时禁用Collider或Layer过滤继续生成Artifact

#### Scenario: Traversal Ramp被Foot Placement Mask包含

- **WHEN** Corin Foot Placement配置会查询`CharacterTraversal`
- **THEN**楼梯配置校验 MUST失败并报告Profile与Ramp
- **AND** Foot Placement MUST不在Runtime按命中优先级选择真实踏面

### Requirement: Traversal Ramp必须与可见楼梯和过渡地面形成合法几何

Traversal Ramp MUST是启用、非Trigger、无Renderer且持久化的受支持Collider。作者校验 MUST证明Ramp上表面的下端和上端分别匹配Lower/Upper Transition，Ramp宽度覆盖Foot Surface可行走宽度，Ramp前进方向与楼梯上行方向一致，并且入口地面、Ramp和顶平台在唯一固定容差内连续。Ramp与Foot Surface MUST不共享Collider引用，也不得形成两个同时进入Gameplay Artifact的重叠支持面。

#### Scenario: Ramp顶端高于楼梯顶平台

- **WHEN** Ramp上端与Upper Transition高度差超过固定容差
- **THEN** validator MUST拒绝该作者数据并报告实测差值与允许边界
- **AND** MUST不夹紧Ramp、移动平台或在Bake结果中插入连接面

### Requirement: Traversal Ramp必须由显式作者操作持久化

楼梯作者工具 MAY通过明确命令创建或更新Traversal Ramp，但结果 MUST保存为Scene或Prefab中的持久化Collider。Deterministic Collision Bake MUST只读取该持久化Collider，不得根据踏面临时生成Ramp、创建隐藏Scene、修改作者对象或在Runtime补齐。`OnInspectorGUI`、`OnValidate`、资源导入、场景打开与Play入口 MUST不执行Ramp生成或Collision Bake。

#### Scenario: 作者修改楼梯踏面尺寸

- **WHEN** 已保存Ramp不再通过楼梯几何校验
- **THEN** 后续显式Bake MUST被阻止并要求作者显式更新Ramp
- **AND** 系统 MUST不在Inspector重绘或Bake内部静默重建Ramp

### Requirement: 连续楼梯与KCC Step能力课程必须保持独立

Gameplay Lab中的连续Low、High与OverLimit往返楼梯 MUST各自使用上行与下行两个Traversal Ramp作为Gameplay Collision，共六个稳定梯段身份。项目 MUST另外保留只由真实Gameplay Collider组成的Step Capability Course，分别表达0.14m与0.24m合法孤立Step以及0.40m超限阻挡。Step课程 MUST不注册为连续楼梯，也 MUST不提供Foot Placement Ramp分离fallback。

#### Scenario: 检查0.40m Step拒绝边界

- **WHEN** Fixed KCC接近Step Capability Course中的0.40m孤立障碍
- **THEN** Collision Artifact MUST包含该真实障碍而不是Traversal Ramp
- **AND** 该结果 MUST不依赖连续楼梯的Foot Surface Collider

### Requirement: 楼梯表面诊断必须暴露唯一所有权

Editor诊断 MUST按Stair identity只读显示Ramp Collider、Foot Surface Collider数量、Layer、Deterministic Surface owner、Ramp端点误差、宽度覆盖、前进方向和可Bake状态。诊断 MUST不修改Layer、层级、Collider、Profile Mask或Artifact，并 MUST不在Repaint中遍历或Bake完整Collision World。

#### Scenario: 作者排查重复表面所有权

- **WHEN** 同一个Collider被Ramp和Foot Surface同时引用
- **THEN** 诊断 MUST显示Stair identity、Collider和两个冲突角色
- **AND** Collision Bake MUST保持未执行
