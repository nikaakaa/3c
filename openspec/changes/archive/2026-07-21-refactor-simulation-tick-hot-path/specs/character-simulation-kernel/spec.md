## ADDED Requirements

### Requirement: ProgramExecutionLayout必须预解析Tick热路径静态查询

ProgramExecutionLayout MUST在Program Runtime composition时一次性构建按operation索引的连续Value input span、紧凑Timeline operation集合、Timeline child owner、State所属StateMachine/execution owner、固定语义edge、operation reference和named constant索引。Float32与Fixed Runtime MUST复用各自Program的immutable layout。正常Tick MUST不为这些查询遍历全部Program operation、解析端口字符串、建立端口HashSet、按字符串排序或执行LINQ materialization。Layout MUST只缓存路由，不得缓存依赖mutable state的Value结果。

#### Scenario: 同一Condition跨Tick求值

- **WHEN** 同一Condition在连续Tick读取相同Value graph
- **THEN** Runtime MUST复用同一immutable input span
- **AND** 每次读取 MUST仍按当前transaction state重新求值source operation

#### Scenario: Program扩张但active路径不变

- **WHEN** Program增加不活跃的State、Timeline或Value operation
- **THEN** 当前Tick查找active Timeline、State execution owner和Timeline child owner MUST不重新扫描新增operation
- **AND** 静态关系 MUST只增加composition时layout构建成本

#### Scenario: Layout关系不唯一

- **WHEN** Timeline child有多个owner、State owner无法唯一解析或binding table不canonical
- **THEN** Program Runtime composition MUST失败
- **AND** MUST不在Tick内搜索近似owner或使用SourceMap字符串fallback

### Requirement: Kernel Program Binding必须与共享Program Layout分离

ProgramExecutionLayout与ProgramExecutionServices MUST只持有ProgramId、ProgramHash、LayoutHash、OperationSetVersion和NumericProfile等Program固有身份。具体Kernel backend MUST由Program Runtime创建独立`KernelProgramBinding`，并在Session运行前一次性验证Program、Layout、NumericProfile、Operation Set与backend完整性。同一Program MAY在不同合法Pipeline、Source、Solver或Network Model中复用同一Layout。Evaluate与Finalize MUST只执行O(1) binding identity或引用校验，MUST不重新枚举Program operation。

#### Scenario: 同一Float32 Program用于Local与Authority

- **WHEN** Local Session与Unity Authority Session绑定同一Float32 Program
- **THEN** 两者 MAY复用同一ProgramExecutionLayout
- **AND** 各自 MUST拥有匹配自身Kernel specialization的binding，Layout MUST不被第一个backend改写

#### Scenario: Evaluate收到另一Kernel的Pending

- **WHEN** Finalize收到Actor、Tick、Program、Layout或Kernel binding不匹配的Pending
- **THEN** Kernel MUST明确失败并Abort该transaction
- **AND** MUST不通过backend字符串搜索另一个workspace或重新验证整张Program

### Requirement: Character State Transaction必须单次复制并移交Dirty Page

Float32与Fixed Character State Transaction MUST使用Actor-owned、layout-indexed transaction workspace复用dirty metadata。一个transaction中每个dirty page MUST最多从base state复制一次；Commit MUST以明确take-ownership把WorkspaceOwned array移交给新的immutable page，并立即删除全部workspace可写引用。未修改page与partition MUST继续共享。Abort或Dispose MUST只释放仍由workspace拥有的数据，MUST不改变base state、前一committed state或已发布page。已发布page array MUST不回到可写池。

#### Scenario: 一个Tick多次写同一page

- **WHEN** 多个operation在同一transaction修改同一typed state page
- **THEN** 第一次写 MUST复制base page一次，后续写 MUST复用同一WorkspaceOwned array
- **AND** Commit MUST不再次clone该array

#### Scenario: Finalize失败

- **WHEN** WorldResult或Finalize validation失败且transaction Abort
- **THEN** base state与全部已提交历史 MUST保持不变
- **AND** 只有未发布workspace data MAY被清理或复用

#### Scenario: Commit发布新状态

- **WHEN** transaction成功Commit
- **THEN** 新state MUST只替换dirty page并复用其它page
- **AND** workspace MUST不再持有任何可修改新state的引用

### Requirement: Evaluate与Finalize必须通过唯一Actor Output Lease冻结结果

每个Actor/SimulationTick MUST从Evaluate开始持有唯一output workspace lease直到Finalize成功或Abort。Pending evaluation MUST只保存Actor、Tick、Program/Layout/Kernel binding、lease generation、State Transaction与WorldRequest，不得拥有Facts、Presentation Commands或Trace副本。World ResolveBatch MUST只读取WorldRequest。Finalize MUST在同一workspace追加后置输出，并在正式`SimulationActorTickResult`边界恰好冻结一次。Snapshot、History、Network、Diagnostics与Presentation MUST只消费最终immutable result或在自己的持久边界复制数据。

#### Scenario: Evaluate等待WorldSolver

- **WHEN** Evaluate完成且World ResolveBatch尚未返回
- **THEN** output builders MUST仍由该Actor lease唯一持有
- **AND** 同Actor MUST不能开始下一次Evaluate或让Pending复制builders

#### Scenario: Finalize成功

- **WHEN** WorldResult匹配Pending并完成State Commit
- **THEN** pre-world与post-world输出 MUST在同一builder中形成最终集合
- **AND** Result构造 MUST是唯一一次跨workspace冻结

#### Scenario: Outer transaction中途失败

- **WHEN** 一个Actor已Evaluate而后续pass或另一Actor失败
- **THEN** outer Abort MUST释放全部尚未完成的lease并Abort对应transaction
- **AND** 下一次transaction MUST不观察到上一事务的Fact、Presentation或Trace
