## ADDED Requirements

### Requirement: Ground Path Revision必须冻结真实步伐端点

每只脚的Ground Path Revision MUST在创建时只从上一成功Seal的实际Completed Sole捕获起点，并以同Frame Accepted Landing、Landing Event identity、Motion Timeline generation与authority tick、Future Body Translation source identity、Component Up和Profile revision建立其余事实。Revision MUST冻结起点、终点、Landing Surface identity、查询请求和结果，不得在后续Frame按动画Sole、Body或Visual Root变化旋转、平移或重投影旧命中。Runtime没有上一成功Frame时 MUST发布`OriginUnavailable`，不得从当前Transform、默认地面或固定高度补起点。

同一Landing Event、运动权威、Accepted Landing几何和Profile revision未变化时 MUST复用Committed Revision，不得因后续动画Sole移动而逐Render Frame重复执行Physics查询。Landing Event、运动权威、Accepted Landing几何或Profile revision变化时 MUST从当时的上一完成Frame捕获新起点，创建新Revision并重新执行Landing与Ground Path查询；Rejected Revision只可在新的authority tick重新尝试，不得用旧Revision冒充当前事实。

#### Scenario: 同一Revision跨多个表现帧

- **WHEN** Landing Event、运动权威、Accepted Landing与Profile revision均未变化
- **THEN** Runtime MUST复用同一Committed Ground Path Revision
- **AND** 后续动画Sole移动 MUST不触发新的Capsule Ground Detection

#### Scenario: 转向产生新运动权威

- **WHEN** committed运动权威改变并产生新的Accepted Landing
- **THEN** Runtime MUST从上一完成Sole创建新的Ground Path Revision
- **AND** MUST重新执行Capsule查询而不是旋转旧查询结果

### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿上一Completed Sole到Accepted Landing的脚步路径构造唯一Capsule请求。完整Capsule包络的两个端点 MUST分别为`PathStart + ComponentUp * CastAbove`与`PathEnd + ComponentUp * CastAbove`，查询方向 MUST为`-ComponentUp`，查询距离 MUST为`CastAbove + CastBelow`，且`CastAbove` MUST大于半径。请求 MUST显式携带两个端点、半径、最大轴段长度、方向、距离、Ground Layer和固定命中容量；Capsule表示路径采集包络，不表示鞋底矩形或脚掌凸包。

Unity World Query Backend MUST按最大轴段长度把完整轴确定性切成首尾连续的短段，并对每段执行实际Capsule Cast。分段 MUST只由请求几何与正式配置决定；不得根据命中情况切换到Raycast、Sphere Cast或其它查询。同一MeshCollider在不同短段产生的合法接触 MUST能够作为不同候选保留。

查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与同分段重复命中，并把每个保留候选的分段索引、位置、法线、Surface identity、查询距离和稳定候选identity写入预分配结果页。原始候选不得在本阶段按坡度、台阶高度、Reach或Hull删除；固定容量溢出、没有合法候选和非法请求 MUST产生不同typed rejection，不得生成默认地面。

#### Scenario: Capsule经过多个表面

- **WHEN** 实际Capsule查询命中多个合法地面或障碍表面
- **THEN** Ground Detection MUST在固定容量内发布对应原始位置与法线
- **AND** MUST不提前把候选压成单个落点、Edge或Envelope

#### Scenario: Capsule没有合法命中

- **WHEN** 查询没有返回合法原始接触
- **THEN** Ground Path Revision MUST发布明确NoContact rejection
- **AND** MUST不复用旧候选或执行第二种查询

### Requirement: Ground Path查询抽象必须与Unity适配器分离

Foot Placement Runtime MUST只依赖Ground Path查询合同和预分配结果页。纯Revision构造 MUST不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型；唯一Unity World Query Backend MUST负责把正式Capsule请求适配到当前`PhysicsScene`、过滤自身Collider并写入规范结果。

#### Scenario: 使用纯查询实现检查Revision

- **WHEN** 非Unity查询实现接收同一合法Capsule请求和规范接触集合
- **THEN** Revision构造 MUST产生与Unity类型无关的同结构结果
- **AND** Foot Placement Runtime MUST不需要查询实现的具体类型

## MODIFIED Requirements

### Requirement: 当前Landing阶段必须保持Pose恒等

当前阶段只验证未来落点与Ground Path原始接触，不实现Foot Motion、FootLock、Constraint、Anchor、Pelvis、Ground Envelope、Edge、Hull或Reachability。Ground Path Revision和Capsule Ground Detection只能生成世界查询事实；Pelvis与双脚Goal的位置和旋转权重 MUST全部为零，唯一FullBodyIK MUST在验证Goal lineage后跳过FBBIK求解并保持输入Pose不变。

#### Scenario: Ground Path查询完成

- **WHEN** 任一脚得到Accepted Landing与原始Ground Contact集合
- **THEN** Landing与Ground Path事实 MUST只进入同一成功Seal后的diagnostics
- **AND** 脚、骨盆和其它Physical Bone MUST继续使用原动画Pose

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST只显示Current Animated Sole、Raw Landing、实际Landing SphereCast、Accepted或Rejected Landing、实际Ground Path Capsule请求及其原始接触位置和法线。诊断 MUST从成功提交的只读摘要发布，不得重新采样动画、重新查询世界、保存完整Foot Feature或伪装Grounding、Predictive Modifier、Anchor、Pelvis、Edge、Hull和Ground Envelope语义。

Capsule Gizmo MUST直接使用Runtime请求中的两个端点、半径、方向与距离显示简单完整包络。短胶囊首尾连续且并集等于该包络，Gizmo MUST不重复绘制内部接缝；不得用矩形鞋底、脚掌凸包、伪Path或文字替代真实请求。

#### Scenario: 查看Ground Path诊断

- **WHEN** 用户打开Foot Placement Scene诊断
- **THEN** 显示内容 MUST与最近一次成功Seal的Landing与Ground Path事务一致
- **AND** 读取诊断 MUST不改变下一帧结果或再次执行Physics查询
