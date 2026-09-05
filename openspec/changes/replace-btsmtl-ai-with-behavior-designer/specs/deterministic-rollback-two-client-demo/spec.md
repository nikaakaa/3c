## MODIFIED Requirements

### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

Player MUST使用唯一共享 GameplayLab 场景和显式 Rollback Variant，不恢复旧 Bootstrap／Peer Scene 分裂入口。

每个Demo Run MUST从一个精确DeterministicRollback Candidate启动两个独立Unity Client、一个纯.NET Dedicated Relay和一个独立开发GM。两端 MUST加载相同CandidateId、Model、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KCC identity和stable actor roster。Relay MUST只拥有网络职责和GM窄只读查询桥，不执行Gameplay Program、KCC、Presentation或Unity Scene；GM MUST不参与gameplay handshake、canonical input、rollback history或hash。不同Slot中的Demo Run MUST拥有独立RunId、SessionId、endpoint、token、进程与日志。

正式 Bot 样例 MUST保持两个真实 Unity Peer 的进程拓扑，并以独立 Actor roster 装配两名真人角色、两名玩家角色 Bot 和一名中立 Actor。Candidate MUST显式指定既有 Peer 的 Bot 输入所有权，不新增 Bot 连接、PlayerId 或 Unity Host。玩家/怪物战斗规则未完成的部分 MUST如实标明；本能力交付移动与正式动作请求接入，不宣称完整 2v2vE 战斗。

#### Scenario: 双端开始模拟

- **WHEN** 一个Run的Client A/B完成精确Candidate与Session handshake
- **THEN** Relay MUST校验全部deterministic和Run identities后才允许SimulationTick推进
- **AND** GM MUST只查询该Run的Relay快照

#### Scenario: Demo 使用选择性输入时序

- **WHEN** 一个Run的双Client开始推进Rollback Session
- **THEN** 连续移动与Immediate request MUST使用0 Tick模型延迟，Corin Offensive request MUST使用2 Tick延迟
- **AND** confirmed frontier MUST继续使用独立confirmation delay

#### Scenario: 启动两个开发Session

- **WHEN** 作者用两个不同Slot启动两份Candidate
- **THEN** 每个Run MUST各自启动Relay、GM、Client A、Client B四个进程
- **AND** 两个Run MUST不共享endpoint、token、mutable runtime或日志目录

#### Scenario: 旧Unity Host或固定Product入口进入候选

- **WHEN** Candidate Session Plan、Scene closure或启动参数包含Canonical Host、Host Player、固定ProductRoot或StopExisting
- **THEN** Build或Run MUST失败
- **AND** MUST不保留旧入口作为fallback

#### Scenario: 启动开发产品

- **WHEN** 作者从正式 Candidate 入口启动本次开发 Run
- **THEN** MUST由该 Candidate 的工具与 Session Plan 启动 GM、Relay、Client A、Client B
- **AND** 四个角色 MUST绑定同一 Candidate／Run／Session，只有两个进程是 Unity Player

#### Scenario: 工具访问四个只读命令

- **WHEN** 作者在独立 GM 进程的文本控制台提交正式查询
- **THEN** 独立 GM MUST通过 Relay 查询桥获得该会话事实
- **AND** MUST不修改移动、Offensive 延迟、最大预测领先量或表现链路

#### Scenario: 旧 Unity Host 进入产品

- **WHEN** Scene closure、manifest 或参数包含旧 Canonical Host 或 Host Player role
- **THEN** Build MUST失败，不保留 fallback

#### Scenario: 两端 Handshake

- **WHEN** Client A 与 Client B 加入 Demo
- **THEN** Relay Server MUST校验全部deterministic identities后才允许SimulationTick推进
- **AND** Server MUST不加载Fixed Program或Collision World内容

#### Scenario: Demo 启动产品

- **WHEN** 作者选择已发布的精确 Rollback Candidate 与合法 Slot
- **THEN** Run MUST启动该候选的 Dedicated Relay、独立 GM、Client A 与 Client B
- **AND** 只有两个进程是 Unity Player，Run MUST不重新 Build

#### Scenario: 旧 Unity Host 资产进入产品

- **WHEN** Build scene closure、manifest或启动参数包含Canonical Host Scene或Host Player role
- **THEN** Build MUST失败
- **AND** MUST不把旧Host保留为fallback

#### Scenario: 两个真实Peer控制五个Actor

- **WHEN** Bot 示例 Candidate 启动并完成握手
- **THEN** 两端 MUST按相同所有权与 Fixed 内容模拟完整五 Actor roster
- **AND** Relay 的真实连接数 MUST仍为两个 Peer，Bot 输入 MUST只由指定 Peer 生产

### Requirement: Demo 必须限制并明确世界能力范围

Demo MUST只使用已编译的静态 DeterministicCollisionWorldArtifact、fixed capsule Actor contact profile和已声明 KCC capabilities。Rollback Variant MUST在共享GameplayLab场景中引用与Local Fixed相同的正式可见环境和Collision Artifact；该场景中唯一`DeterministicCollisionWorldAuthoring`及其显式surface marker MUST同时作为可见测试几何和Fixed Collision Artifact的唯一作者来源，Build MUST不创建隐藏临时碰撞世界。Rollback Composition MUST显式要求`WorldFeature.ActorCollision`。UI/文档 MUST明确支持静态世界与多Actor `SolidBodyBlock`，但未支持 Unity Physics、Rigidbody、moving platform、动态破坏、质量/冲量物理和完整竞技网络产品。

Bot 与中立 Actor MUST复用已有按稳定 ActorId 批处理的角色碰撞能力，不新增 AI 专用 KCC、Transform 运动或第二碰撞世界。样例 MAY复用稳定 Corin 角色与表现资源，MUST不以修复 TrainingEnemy 表现作为该输入能力的前置条件。

#### Scenario: 查看 Demo 能力

- **WHEN** 作者查看Dedicated Relay Server与Client Diagnostics
- **THEN** MUST显示静态几何、fixed capsule、ActorCollision、SolidBodyBlock与双 Actor 限制

#### Scenario: 作者调整通用移动测试环境

- **WHEN** 作者修改共享Prefab中的楼梯、坡面、墙体、门洞、台阶或不平整静态几何并执行Rollback Prepare/Build
- **THEN** Baker MUST从共享GameplayLab场景的同一正式Collider作者层级重新生成CollisionWorldHash
- **AND** MUST不从代码生成第二份隐藏测试地图

#### Scenario: 一个 Peer 冲刺撞向另一个 Actor

- **WHEN** Peer A的闪避或Timeline motion在一个Tick内会穿过Peer B
- **THEN** 两端 MUST由同一Fixed KCC batch阻挡或沿接触切向滑动
- **AND** rollback/replay后 MUST保持相同Actor Body、WorldHash与KCC hash

#### Scenario: 玩家与Bot发生身体接触

- **WHEN** 完整 roster 中玩家和 Bot 的胶囊发生接触
- **THEN** 同一正式 Fixed World batch MUST求解并提交所有相关 Body
- **AND** AI MUST不在求解后直接修正位置
