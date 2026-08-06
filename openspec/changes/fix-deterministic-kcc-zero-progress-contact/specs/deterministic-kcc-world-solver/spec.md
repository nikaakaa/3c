## MODIFIED Requirements

### Requirement: Editor Collision Baker 必须使用显式且唯一的场景作者来源

每个Deterministic Collision World Scene MUST只有一个`DeterministicCollisionWorldAuthoring`。Baker MUST只收集该根下显式`DeterministicCollisionSurfaceAuthoring`标记所拥有的活动Collider子树，MUST按稳定层级与组件身份排序，并拒绝无来源、重复归属、Trigger和不支持的Collider。轴对齐BoxCollider MUST降低为Box primitive；旋转BoxCollider MUST按固定顶点、三角形winding和adjacency规则降低为同一indexed-triangle surface。Baker MUST不创建隐藏临时Scene、代码生成替代几何或运行时Unity Physics读取路径。

Baker MUST在写入Artifact前按相同稳定Collider顺序检查双方均为walkable且进入Artifact的闭合`BoxCollider` pair。检查 MUST先从量化八顶点判定双方局部Y支撑轴不平行，并以双方上表面四边形的水平XZ投影SAT证明存在超过一个quantization cell的正面积交叠，再执行Fixed 15轴OBB SAT。只有三项同时成立且正体积穿插超过一个quantization cell时 MUST失败，并报告两个Collider identity、Surface identity及最小穿透深度。平行支撑实体拼装、上表面投影只在边界相接、或OBB只在面/边相接 MUST允许。Baker MUST不通过删除triangle、Surface优先级、自动CSG、临时禁用Collider或修改Scene继续生成Artifact；失败时既有Artifact MUST保持不变。

场景存在`StairTraversalSurfaceAuthoring`时，Baker MUST在生成Artifact前调用唯一楼梯作者validator。每段已注册连续Ramp梯段的Traversal Ramp MUST位于唯一Deterministic Surface作者子树并进入Artifact；其Foot Placement Surface Collider MUST位于全部Deterministic Surface作者子树之外并从Artifact排除。未注册为Ramp绑定的普通Ground和真实Step Collider MUST继续按普通显式Surface owner规则进入Artifact。Baker MUST不按Layer自动收集Foot Surface，不得跳过非法楼梯、临时禁用Collider或回退逐级Gameplay碰撞。

#### Scenario: 两个walkable旋转Box形成竞争支撑面

- **WHEN** 两个进入同一Artifact的walkable闭合Box具有不平行支撑轴、正面积上表面水平投影交叠，且量化后具有超过一个quantization cell的正体积穿插
- **THEN** Baker MUST在写入Artifact前失败并报告两个稳定Collider与Surface身份
- **AND** MUST不把相互穿插的内部面和内部边发布进Collision World

#### Scenario: 两个walkable Box只在边界相接

- **WHEN** 两个walkable闭合Box只形成面或边接触且残差不超过一个quantization cell
- **THEN** Baker MUST允许继续canonical lowering
- **AND** MUST不把合法连续边界误报为正体积交叠

#### Scenario: 平行支撑实体或坡面平台边界合法拼装

- **WHEN** 两个walkable闭合Box的支撑轴平行，或双方上表面水平投影只在一个quantization cell内形成边界接触
- **THEN** Baker MUST允许继续canonical lowering
- **AND** MUST不把交叉脊、墙板拼装或Traversal Ramp与Top平台的合法边界误报为竞争支撑面

### Requirement: Deterministic KCC 必须统一处理去穿透和多平面 Collide-And-Slide

单次Motor movement MUST按固定阶段执行初始penetration recovery、最早TOI移动、contact offset、完整active-constraint remaining displacement投影和最终overlap validation。Active-constraint求解 MUST在三维中按固定顺序枚举原始位移、全部单平面投影、全部非平行双平面交线投影和零向量，选择满足全部active planes且与原始位移Fixed平方距离最小的候选；等距结果 MUST按candidate kind、plane index和fixed raw vector稳定裁决。求解 MUST使用预分配buffer，不得只取最早两个或三个plane、顺序重复clip、使用float或动态扩容。

一面接触 MUST保留切向位移，两面独立接触 MUST限制到交线，约束封闭时 MUST停止剩余位移。若连续两轮同时满足`TOI=0`、safe position不变、canonical blocking contact identity与normal raw值相同、active constraint没有产生新解且projected remaining完全不变，Motor MUST把当前状态认定为确定性的受阻收敛，清零remaining并继续Ground Probe与最终validation。任一事实变化时 MUST清除旧零进展证据并继续正常求解。

#### Scenario: 相同内部边连续返回TOI零

- **WHEN** canonical query连续两轮返回同一`TOI=0`contact set且safe position与active-constraint结果均未变化
- **THEN** Motor MUST在最后safe position结束当前剩余位移并返回成功的受阻BodyResult
- **AND** MUST不重复到MaximumContactIterations后关闭Session

#### Scenario: 零时刻接触产生新的合法切向方向

- **WHEN** 当前`TOI=0`contact为active constraints增加新法线且完整求解得到与输入不同的合法切向位移
- **THEN** Motor MUST清除旧零进展证据并继续该切向位移
- **AND** MUST不因单轮TOI零直接停止

#### Scenario: 三个以上约束仍有唯一合法方向

- **WHEN** movement累计三个以上active planes且其中部分plane等价或不封闭全部方向
- **THEN** Motor MUST从全部active planes选择满足完整约束的唯一Fixed候选
- **AND** 结果 MUST不依赖只有前两个或前三个contact的旧特判

### Requirement: KCC 失败必须终止确定模拟而不回退

Overflow、query capacity、无法证明为确定性受阻收敛的iteration non-convergence、invalid artifact或unsupported dynamic state MUST产生精确deterministic solver failure。连续两轮相同`TOI=0`、相同safe position、相同canonical blocking contact set和相同active-constraint结果形成的零进展状态 MUST视为已经收敛到合法受阻位置，MUST正常结束当前remaining并保留已完成位移；它 MUST不冒充capacity或query failure。系统 MUST不回退Unity Physics、CharacterController、float solver或直接应用request displacement。

#### Scenario: Contact Iteration 无法证明收敛

- **WHEN** KCC达到固定iteration limit且contact、position或projected remaining仍在变化，无法形成完整零进展证明
- **THEN** MUST报告明确failure并停止该simulation session
- **AND** MUST不把未知状态静默转换成BlockedNoProgress

#### Scenario: Contact Iteration 已证明受阻收敛

- **WHEN** KCC在预算内连续两轮形成完全相同的零进展证明
- **THEN** MUST清零当前remaining并继续完成同一Motor事务
- **AND** MUST不抛iteration non-convergence异常

## ADDED Requirements

### Requirement: Gameplay Lab 粗糙地面必须使用连续Gameplay Collision

Gameplay Lab `RoughTile_*` MAY继续作为可见粗糙地面外观，但 MUST不各自保留进入Deterministic Collision Artifact或Foot Placement查询的相互穿插闭合Box。粗糙地面 MUST使用一个持久化、无Renderer、`Ground`层、非Trigger的连续MeshCollider表达相同小坡度顶面；该Collider MUST由唯一`DeterministicCollisionSurfaceAuthoring`拥有并同时供Fixed Artifact与Foot Placement普通Ground查询。周边课程地面 MUST使用在粗糙区域精确开孔的持久化顶面Collider，并与粗糙Mesh只共享同一外边界，不得在粗糙区域下方保留第二层Ground。系统 MUST不按Tile名称、Surface优先级或Runtime模式选择碰撞表面。

#### Scenario: 角色通过粗糙地面Tile接缝

- **WHEN** Fixed KCC和Foot Placement处理由多个可见RoughTile形成的粗糙区域
- **THEN** 两者 MUST消费同一个连续Ground Collider的外部顶面
- **AND** Collision Artifact MUST不包含各Tile相互穿插的封闭侧面、底面或内部边
