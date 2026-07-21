## 1. 依赖、基线与所有权清单

- [x] 1.1 使用UTF-8读取本change的proposal、design、tasks和全部spec delta
- [x] 1.2 使用UTF-8读取`refactor-gameplay-runtime-and-tooling-modules`的proposal、design、tasks和Kernel delta
- [x] 1.3 确认依赖change的6.14、6.15保持未完成，14.17不得提前勾选
- [x] 1.4 使用UTF-8读取current DeterministicRollback、Action相关规范与对应已归档设计
- [x] 1.5 记录当前Frontend、Operation Set、Semantic IR、Float32/Fixed Program和State codec版本
- [x] 1.6 使用正式portable Reader记录当前Corin ProgramId、SemanticHash、ProgramHash和LayoutHash
- [x] 1.7 记录当前Corin 998个operation、1572个constant、569个Value input、933条control-flow、433条reference和1398个state slot
- [x] 1.8 记录当前14个Timeline operation、25个producer和5086条source-map
- [x] 1.9 记录Value input字符串端口、Runtime HashSet、排序和集合复制调用链
- [x] 1.10 记录Timeline、State owner、Timeline child owner和Kernel兼容性的全Program扫描调用链
- [x] 1.11 记录Float32/Fixed State Transaction的dirty metadata、首次page copy和Commit二次copy调用链
- [x] 1.12 记录Blackboard字符串owner/provenance、Frame reset/clear和scope扫描调用链
- [x] 1.13 记录Evaluate、Pending、Finalize、Result与Actor workspace的当前集合owner
- [x] 1.14 记录Float32/Fixed working state与completed step重复构造state-set的调用链
- [x] 1.15 冻结必须删除的旧parser、旧reader、旧copy、旧clear和旧candidate路径清单

## 2. Numeric-Neutral Value Port Contract

- [x] 2.1 定义只服务Value graph的numeric-neutral `SemanticValueKind`
- [x] 2.2 将Boolean、Int32、UInt64、Number、Vector2、Vector3、Yaw和Identity纳入正式kind集合
- [x] 2.3 定义operation input/output port的稳定identity与canonical order
- [x] 2.4 定义固定kind端口合同
- [x] 2.5 定义BooleanLike受约束端口合同并保持现行truth conversion语义
- [x] 2.6 定义NumericLike受约束端口合同并保持现行Compare conversion语义
- [x] 2.7 定义同一constraint group多个端口的kind一致性规则
- [x] 2.8 定义BlackboardGet输出kind从declaration解析的规则
- [x] 2.9 定义BlackboardSet输入kind从declaration解析的规则
- [x] 2.10 定义Constant输出kind从Semantic literal解析的规则
- [x] 2.11 定义CameraBasisRead按output port解析kind的规则
- [x] 2.12 为ConditionResult、Compare、And、Or、Not和MoveFacingAngle声明完整Value签名
- [x] 2.13 为Input、Action、GameplayTag、GameplayAttribute、State query和Camera Value operation声明完整Value签名
- [x] 2.14 为没有Value端口的operation显式声明空合同
- [x] 2.15 让当前Operation Set中的每个operation code恰好对应一个port contract
- [x] 2.16 在Operation Set初始化时拒绝缺失、重复或unknown/object端口合同
- [x] 2.17 将Operation Set从`character-gameplay-operations/5`升为新版本
- [x] 2.18 删除Target runtime或Editor私有的第二套Value端口类型判断

## 3. Semantic IR Constant Input Binding

- [x] 3.1 定义`SemanticConstantInputBinding`的target operation、target port、constant index和resolved value kind
- [x] 3.2 在Semantic IR model中增加canonical constant-input binding table
- [x] 3.3 让Frontend从authoring PropertyPort读取稳定port id与实际类型
- [x] 3.4 让Frontend通过Operation Port Contract解析authoring input类型
- [x] 3.5 让未连接input constant生成Semantic literal与显式binding
- [x] 3.6 保持`ProgramControlFlowEdge(kind=Value)`作为linked input唯一真值
- [x] 3.7 在Semantic IR validation中校验Value edge source output port存在
- [x] 3.8 在Semantic IR validation中校验Value edge target input port存在
- [x] 3.9 在Semantic IR validation中校验linked source kind满足target约束
- [x] 3.10 在Semantic IR validation中校验constant literal kind满足resolved kind
- [x] 3.11 在Semantic IR validation中解析并校验constraint group
- [x] 3.12 拒绝同一target operation/port存在两个Value edge
- [x] 3.13 拒绝同一target operation/port同时存在Value edge与constant binding
- [x] 3.14 拒绝binding引用不存在的operation、port或constant
- [x] 3.15 按target operation、contract order和port identity建立canonical binding order
- [x] 3.16 将binding table编入Semantic IR payload与SemanticHash
- [x] 3.17 提升Semantic IR ArtifactVersion与PayloadVersion
- [x] 3.18 提升Character Semantic Frontend CompilerVersion
- [x] 3.19 从Frontend constant identity中删除`port:`业务编码
- [x] 3.20 删除Semantic IR旧payload reader和兼容分支
- [x] 3.21 在Semantic IR Inspector增加Value Inputs section与count
- [x] 3.22 在portable Reader增加`value-inputs` section、usage和text输出
- [x] 3.23 在portable Reader增加Semantic IR value-input JSON输出
- [x] 3.24 让Inspector与Reader显示target、port、resolved kind和constant source identity
- [x] 3.25 保持Value binding到原authoring port的SourceMap导航

## 4. Float32/Fixed Target Program Binding与ABI

- [x] 4.1 在Float32 Program model增加结构化constant-input binding table
- [x] 4.2 在Fixed Program model增加同语义constant-input binding table
- [x] 4.3 让Float32 Target从validated Semantic IR降低binding
- [x] 4.4 让Fixed Target从同一validated Semantic IR降低binding
- [x] 4.5 让两个Target拒绝无法降低的resolved value kind
- [x] 4.6 让两个Target再次校验operation、target port、constant与kind一致性
- [x] 4.7 将Float32 binding table编入Program canonical payload与ProgramHash
- [x] 4.8 将Fixed binding table编入Program canonical payload与ProgramHash
- [x] 4.9 将binding与typed Blackboard owner state变化编入LayoutHash
- [x] 4.10 提升Float32 Target ABI
- [x] 4.11 提升Fixed Target ABI
- [x] 4.12 提升Float32 Program ArtifactVersion与ProgramFormatVersion
- [x] 4.13 提升Fixed Program ArtifactVersion与ProgramFormatVersion
- [x] 4.14 提升Float32/Fixed LayoutFormatVersion
- [x] 4.15 更新Float32/Fixed Target artifact descriptor与store expectation
- [x] 4.16 更新Unity ProgramAsset metadata校验的Target ABI与Program identity
- [x] 4.17 在portable Reader program命令输出binding count
- [x] 4.18 在portable Reader program `value-inputs` section显示constant binding
- [x] 4.19 删除旧Float32 Program reader和旧binding缺失fallback
- [x] 4.20 删除旧Fixed Program reader和旧binding缺失fallback
- [x] 4.21 删除Float32/Fixed runtime的`/constant/port:` parser helper
- [x] 4.22 删除旧constant identity兼容映射

## 5. ProgramExecutionLayout索引与Kernel Binding

- [x] 5.1 定义`ProgramLayoutIdentity`且只包含Program固有身份
- [x] 5.2 确认`ProgramLayoutIdentity`不包含Pipeline、Source、Solver、Network Model或backend identity
- [x] 5.3 定义operation-indexed `OperationValueInputRange`
- [x] 5.4 定义连续`CompiledValueInputBinding` storage
- [x] 5.5 在layout构建时合并Value edge与constant binding
- [x] 5.6 在layout构建时校验target port唯一性、source互斥与canonical order
- [x] 5.7 在layout构建时把source/target port identity解析为紧凑port index
- [x] 5.8 让同Program的多个Actor复用同一immutable input layout
- [x] 5.9 让Float32 Value runtime按operation input span读取
- [x] 5.10 让Fixed Value runtime按operation input span读取
- [x] 5.11 保留每次读取对当前transaction state的重新求值
- [x] 5.12 删除Float32 Value input端口HashSet与ordered key/value buffer
- [x] 5.13 删除Fixed Value input端口HashSet与ordered key/value buffer
- [x] 5.14 删除Float32/Fixed input runtime string sort与Substring
- [x] 5.15 在layout建立紧凑Timeline operation handle数组
- [x] 5.16 让portable Timeline只遍历紧凑Timeline数组
- [x] 5.17 在layout建立Timeline child到唯一Timeline owner索引
- [x] 5.18 让portable Timeline child owner查询使用直接索引
- [x] 5.19 在layout建立State到所属StateMachine索引
- [x] 5.20 在layout建立State execution path state address索引
- [x] 5.21 让portable State execution owner查询使用直接索引
- [x] 5.22 在layout建立State OnEnter、Root与OnExit固定语义edge索引
- [x] 5.23 删除portable control中固定端口`FirstOrDefault`查询
- [x] 5.24 在layout建立operation reference按kind的连续索引
- [x] 5.25 删除Value、Blackboard和Action runtime的reference `FirstOrDefault`
- [x] 5.26 在layout预解析GameplayEffect与其它operation named constant
- [x] 5.27 删除Tick内named constant前缀或字符串查找
- [x] 5.28 定义backend-specific `KernelProgramBinding`
- [x] 5.29 让Float32 Program Runtime为catalog Program建立Kernel binding
- [x] 5.30 让Fixed Program Runtime为catalog Program建立Kernel binding
- [x] 5.31 在binding创建时一次性校验NumericProfile、Operation Set和backend完整性
- [x] 5.32 在binding创建时一次性校验Program全部operation code
- [x] 5.33 让Actor runtime port显式保存Program、Layout与Kernel binding
- [x] 5.34 让Float32 Evaluate/Finalize只做O(1)binding identity核对
- [x] 5.35 让Fixed Evaluate/Finalize只做O(1)binding identity核对
- [x] 5.36 删除Kernel热路径重复Program operation扫描
- [x] 5.37 删除任何把backend identity写入共享ProgramExecutionLayout的路径

## 6. Float32/Fixed State Transaction所有权

- [x] 6.1 定义共享的dirty page ownership状态含义
- [x] 6.2 在Float32 Actor workspace增加transaction epoch与layout-indexed dirty metadata
- [x] 6.3 在Fixed Actor workspace增加同形transaction epoch与dirty metadata
- [x] 6.4 按Float32 Program State Layout一次性建立partition/page slot lookup
- [x] 6.5 按Fixed Program State Layout一次性建立partition/page slot lookup
- [x] 6.6 让Float32 transaction Begin只推进epoch并重置dirty counts
- [x] 6.7 让Fixed transaction Begin只推进epoch并重置dirty counts
- [x] 6.8 让Float32首次写page只从base复制一次
- [x] 6.9 让Fixed首次写page只从base复制一次
- [x] 6.10 让Float32同transaction后续写复用WorkspaceOwned array
- [x] 6.11 让Fixed同transaction后续写复用WorkspaceOwned array
- [x] 6.12 让Float32 Commit使用take-ownership构造immutable page
- [x] 6.13 让Fixed Commit使用take-ownership构造immutable page
- [x] 6.14 让Float32 Commit后清除全部Published page可写引用
- [x] 6.15 让Fixed Commit后清除全部Published page可写引用
- [x] 6.16 让两个Target的新committed state只替换dirty page
- [x] 6.17 让两个Target复用未修改page与partition
- [x] 6.18 让Float32 Abort/Dispose只释放WorkspaceOwned数据
- [x] 6.19 让Fixed Abort/Dispose只释放WorkspaceOwned数据
- [x] 6.20 禁止已Published page array回到可写池
- [x] 6.21 保持Float32 GameplayEffect savepoint的LIFO与restore语义
- [x] 6.22 保持Fixed GameplayEffect savepoint的LIFO与restore语义
- [x] 6.23 让transaction diagnostics报告epoch、dirty count与owner status
- [x] 6.24 删除Float32 dirty partition/page Dictionary
- [x] 6.25 删除Fixed dirty partition/page Dictionary
- [x] 6.26 删除Float32 Commit的`CharacterStatePage(values, false)`二次copy
- [x] 6.27 删除Fixed Commit的dirty page二次copy
- [x] 6.28 确认Snapshot、History和前一committed state不引用workspace可写memory

## 7. Blackboard Typed Owner与Generation

- [x] 7.1 定义target-neutral `BlackboardOwnerToken`
- [x] 7.2 定义ScopeKind、CompiledOwnerIndex和Generation字段
- [x] 7.3 定义target-neutral `BlackboardWriteStamp`
- [x] 7.4 定义source operation、logic tick、action、timeline、clip和cycle字段
- [x] 7.5 在Program layout为每个scope分配稳定CompiledOwnerIndex
- [x] 7.6 将Character scope token在初始State中建立
- [x] 7.7 将Graph Config scope token在初始State中建立并保持只读
- [x] 7.8 让Graph Instance token使用Runnable activation generation
- [x] 7.9 让State token使用compiled execution owner与activation generation
- [x] 7.10 让ActionInstance token使用compiled scope owner与ActionInstanceId
- [x] 7.11 让Frame token使用compiled scope owner与SimulationTick
- [x] 7.12 在公共ProgramStateValueKind增加typed owner token与write stamp
- [x] 7.13 在Float32 CharacterStateValue增加两个typed value
- [x] 7.14 在Fixed CharacterStateValue增加两个typed value
- [x] 7.15 更新Float32 state slot schema与canonical codec
- [x] 7.16 更新Fixed state slot schema与canonical codec
- [x] 7.17 提升Float32 Character State CodecIdentity
- [x] 7.18 提升Fixed Character State CodecIdentity
- [x] 7.19 让Float32 Blackboard read在generation不匹配时返回declaration default且不写State
- [x] 7.20 让Fixed Blackboard read在generation不匹配时返回declaration default且不写State
- [x] 7.21 让Float32第一次真实write materialize value、token与write stamp
- [x] 7.22 让Fixed第一次真实write materialize value、token与write stamp
- [x] 7.23 让ActionWindow projection只接受当前generation的真实write stamp
- [x] 7.24 让默认读取和旧generation值不能产生projection
- [x] 7.25 删除Float32 BeginFrame全scope reset
- [x] 7.26 删除Float32 EndFrame全scope clear
- [x] 7.27 删除Fixed BeginFrame全scope reset
- [x] 7.28 删除Fixed EndFrame全scope clear
- [x] 7.29 删除Float32/Fixed actor、frame、graph、state和action owner字符串拼接
- [x] 7.30 删除Blackboard provenance字符串state
- [x] 7.31 将人类可读owner/provenance移入按需diagnostics formatter
- [x] 7.32 保持Graph、State和Action completion由各自正式lifecycle推进generation
- [x] 7.33 删除旧字符串owner reader、physical clear helper和兼容路径

## 8. Evaluate/Finalize Output Lease

- [x] 8.1 定义Actor output workspace lease identity与generation
- [x] 8.2 让lease绑定ActorId、Tick、Program、Layout和KernelProgramBinding
- [x] 8.3 让Float32 Evaluate开始唯一Actor lease
- [x] 8.4 让Fixed Evaluate开始唯一Actor lease
- [x] 8.5 让Float32 Pending只保存lease identity、transaction与WorldRequest
- [x] 8.6 让Fixed Pending只保存lease identity、transaction与WorldRequest
- [x] 8.7 让Float32 Pending不复制Facts
- [x] 8.8 让Float32 Pending不复制Presentation Commands
- [x] 8.9 让Float32 Pending不复制Trace
- [x] 8.10 让Fixed Pending删除同样三组复制
- [x] 8.11 让World ResolveBatch只读取独立WorldRequest
- [x] 8.12 让Float32 Finalize通过lease取得同一workspace builders
- [x] 8.13 让Fixed Finalize通过lease取得同一workspace builders
- [x] 8.14 让后置Motion Fact追加到同一Facts builder
- [x] 8.15 让Finalize Trace追加到同一Trace builder
- [x] 8.16 让Float32最终Result恰好冻结一次Facts、Presentation和Trace
- [x] 8.17 让Fixed最终Result恰好冻结一次Facts、Presentation和Trace
- [x] 8.18 保持最终`SimulationActorTickResult`对外immutable schema不变
- [x] 8.19 在同Actor lease未结束时拒绝下一次Evaluate
- [x] 8.20 在Actor、Tick、binding或lease generation不匹配时fail-fast
- [x] 8.21 让World Resolve失败释放lease并Abort transaction
- [x] 8.22 让Finalize失败释放lease并Abort transaction
- [x] 8.23 让outer transaction Abort释放全部未消费lease
- [x] 8.24 让成功Finalize在Result冻结后释放并清空workspace
- [x] 8.25 确认Snapshot、History、Network和diagnostics只消费最终Result或自己的canonical copy
- [x] 8.26 删除Pending的ReadOnlyCollection owner与copy helper
- [x] 8.27 删除Evaluate结束时提前End/clear Actor workspace的旧路径
- [x] 8.28 删除Finalize前从Pending AddRange回workspace的旧路径

## 9. Pipeline Canonical State与剩余热路径

- [x] 9.1 让Float32 Pipeline working state只持有canonical state-set引用
- [x] 9.2 让Fixed Pipeline working state只持有canonical state-set引用
- [x] 9.3 让working state创建直接引用StateStore.Current
- [x] 9.4 让Float32 BeginSimulationStep发布当前canonical引用
- [x] 9.5 让Fixed BeginSimulationStep发布当前canonical引用
- [x] 9.6 让Float32 CompleteStep只构造一个next candidate
- [x] 9.7 让Fixed CompleteStep只构造一个next candidate
- [x] 9.8 让Float32 ApplyCompletedStep只替换candidate引用
- [x] 9.9 让Fixed ApplyCompletedStep只替换candidate引用
- [x] 9.10 让Float32 PublishWorkingState发布同一candidate引用
- [x] 9.11 让Fixed PublishWorkingState发布同一candidate引用
- [x] 9.12 让多step schedule直接把上一candidate作为下一step输入
- [x] 9.13 让restore preparation构造完整canonical restore candidate
- [x] 9.14 让restore apply与rollback原子替换完整candidate引用
- [x] 9.15 保持WorldSolver restore由正式world participant拥有
- [x] 9.16 删除Float32 working state `ToStateSet`
- [x] 9.17 删除Fixed working state `ToStateSet`
- [x] 9.18 删除working state重复`FreezeActors`和等价state-set包装
- [x] 9.19 保持Snapshot与StateHash只在execution plan明确要求时生成
- [x] 9.20 搜索Float32/Fixed Tick路径中的`FirstOrDefault`、`AsReadOnly`、`ToArray`、`OrderBy`和`Sort`
- [x] 9.21 将保留的集合操作限制在artifact、composition、最终freeze、snapshot/history/network或异常路径
- [x] 9.22 确认diagnostics关闭时不构造SourceMap路径、scope owner或详细Trace字符串
- [x] 9.23 保留Logic Tick、Kernel、State Commit、Result Freeze、Pipeline和Presentation独立Profiler marker
- [x] 9.24 删除失去调用方的buffer、copy helper、scope formatter和旧workspace字段

## 10. 正式Artifact与产品发布

- [x] 10.1 汇总并连续提升Frontend、Operation Set、Semantic IR、Target ABI、Program和State版本常量
- [x] 10.2 更新Program Runtime、Kernel binding、Snapshot codec和产品identity中受版本影响的组成
- [x] 10.3 通过正式Character Simulation build workflow生成新Corin `.csir`
- [x] 10.4 通过同一build transaction生成新Corin Float32 Program
- [x] 10.5 通过正式Fixed Target workflow生成新Corin Fixed Program
- [x] 10.6 重新发布Corin ProgramAsset与Presentation Projection binding
- [x] 10.7 使用portable Reader读取新Semantic IR summary与value-inputs
- [x] 10.8 使用portable Reader读取新Float32 Program summary与value-inputs
- [x] 10.9 使用正式Fixed reader读取新Fixed Program identity与value-inputs
- [x] 10.10 重建Unity Authority Network Test Product manifest
- [x] 10.11 重建DotRecast Authority Network Test Product manifest
- [x] 10.12 重建Deterministic Rollback Network Test Product manifest
- [x] 10.13 确认三个产品锁定新Operation Set、Target ABI、ProgramHash、LayoutHash和State codec
- [x] 10.14 删除旧`.csir`、旧`.csim`和旧`.fixed-program`
- [x] 10.15 删除旧generated ProgramAsset metadata与旧产品manifest
- [x] 10.16 删除旧Snapshot/History fixture和旧State codec产物
- [x] 10.17 确认没有Unity batchmode、手改generated bytes或旧artifact fallback

## 11. 文档、编译与Strict Validation

- [x] 11.1 更新`implementation-inventory.md`记录六条新owner链路
- [x] 11.2 更新`openspec/project.md`的Value Port、Program Layout、Kernel Binding、State Transaction、Blackboard、Output Lease和Pipeline candidate真相
- [x] 11.3 更新受影响的current architecture文档并删除旧字符串binding与Frame physical clear描述
- [x] 11.4 根据真实实现更新依赖change的6.14与6.15状态
- [x] 11.5 只有依赖change全部任务真实完成时才标记其14.17
- [x] 11.6 使用规定参数编译portable Core与Semantic IR Reader
- [x] 11.7 立即执行`dotnet build-server shutdown`
- [x] 11.8 使用规定参数编译Float32 Target与Runtime
- [x] 11.9 立即执行`dotnet build-server shutdown`
- [x] 11.10 使用规定参数编译Fixed Target与Runtime
- [x] 11.11 立即执行`dotnet build-server shutdown`
- [x] 11.12 使用规定参数编译Unity Runtime与Editor程序集
- [x] 11.13 立即执行`dotnet build-server shutdown`
- [x] 11.14 使用规定参数编译Server portable products
- [x] 11.15 立即执行`dotnet build-server shutdown`
- [x] 11.16 确认普通.NET Reader与Server不引用UnityEngine或UnityEditor
- [x] 11.17 运行`openspec validate refactor-gameplay-runtime-and-tooling-modules --strict --no-interactive`
- [x] 11.18 运行`openspec validate refactor-simulation-tick-hot-path --strict --no-interactive`
- [x] 11.19 确认没有fallback、compatibility、legacy parser、temporary bridge、双写或第二runtime路径
- [x] 11.20 确认全部任务真实完成后将本文件所有任务标记为`[x]`
