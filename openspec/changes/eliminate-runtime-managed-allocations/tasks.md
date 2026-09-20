本清单为实施项，不新增测试代码、手动验证任务或第二性能采集链。任务按依赖组织，不代表优化收益优先级。每个切片同时迁移生产者、全部消费者、正式容量和旧入口；已有正确改动冲突时先报告。

用户明确的反射边界：初始化／内容准备允许反射；游戏运行中的更新、事件处理、回放和状态恢复禁止反射。先确认调用阶段，初始化优化不得计为运行期收益。

## 1. 固定正式内存所有权与容量

- [ ] 1.1 在现有各领域准备入口提供由内容布局推导的容量结果，以及不能推导的正式配置引用，输出owner、上限和来源清单
- [ ] 1.2 为跨步结果、快照、消息和诊断明确借用/租用/归还合同，落实现有提交、丢弃、超窗和发送完成边界
- [ ] 1.3 将实际参与运行的第三方、UI、AI和服务端调用纳入audit归属，未使用源码与DLL/native待归因项保留独立状态

## 2. 模拟和状态事务

- [ ] 2.1 将 Fixed 角色与技能周期装配改为由现有Session/Actor长期持有，按逻辑步绑定输入和frame上下文，删除Evaluate中的重复装配集合与工厂
- [ ] 2.2 将 Float32 对应入口完成同一所有权迁移，保留其数值状态与执行后端，不复制另一套公共模型
- [ ] 2.3 将两数值域的值输入缓冲、递归栈和上下文scope改为准备容量内复用，移除周期class scope和临时字符串键，保持原求值次数与顺序
- [ ] 2.4 将角色/能力事务状态、保存点和GE/Equipment工作页接入独立Pending与Committed存储，删除周期字典Clone分配，保留原子提交和嵌套恢复
- [ ] 2.5 迁移Complete结果及全部Pipeline/表现/网络消费者，删除三列表复制入口，明确各结果最后消费者和归还边界
- [x] 2.6 在 Fixed／Float32 正式效果定义准备时生成来源／目标属性捕获名称清单，公共效果应用流程按下标消费，删除每次应用的 SortedSet 重建和接口枚举；属性值仍按本次上下文读取，Spec／快照／事务存储不在此小步完成范围
- [x] 2.7 参数声明匹配统一到公共应用流程的下标读取，删除两数值域的 LINQ Contains 入口；标签匹配直接消费 IReadOnlyList，保留 All／Any／None 短路顺序并删除数组副本、LINQ 闭包及运行期父链检查集合，目录准备时仍校验父引用与循环
- [x] 2.8 将两数值域标签 scratch 的 SortedSet 改为 HashSet 去重加已有列表的 Ordinal 排序，消除重复树节点分配；工作／已提交状态 HasTag 改为下标读取。容量增长、scratch 跨步寿命和来源字典遍历仍未完成
- [x] 2.9 将执行来源构造及 Fixed／Float32 移除效果选择器的运行期枚举校验改为显式成员比较，保留合法集合和原异常，删除三处装箱入口
- [x] 2.10 两数值域 SetTagSource 在规范化工作列表上先比较现有标签，相同则沿用已有数组，变化才生成独立结果；保留排序去重、空来源移除、脏标记和 scratch 原寿命
- [x] 2.11 公共动作标签来源键在固定栈缓冲格式化 ulong，删除中间数字字符串，只生成最终 action 键；身份格式、零值拒绝和状态归属不变
- [x] 2.12 两数值域效果快照标签汇总改为 HashSet 去重及最终数组 Ordinal 排序，删除逐标签树节点；活动效果列表复制按下标读取，保留深拷贝结果与事务边界
- [x] 2.13 两数值域效果快照字典复制源统一使用实际 SortedDictionary 类型，删除 IEnumerable 枚举器装箱；克隆内容、顺序及目标字典抽象不变
- [x] 2.14 两数值域属性快照 modifier 列表在深拷贝前按源 Count 一次准备容量，避免复制内逐级扩容；后续运行新增容量与整体快照分配仍未完成
- [x] 2.15 两数值域生命周期／属性／Cue／失败记录直接分配游标后构造并加入变更列表，删除八个捕获 lambda 入口和 Func 工厂；记录对象、游标及回滚语义不变
- [x] 2.16 两数值域效果定义准备时生成授予标签的 Ordinal 唯一清单，激活直接使用清单，删除每次激活的组件扫描及 SortedSet；活动效果标签状态和撤销边界不变
- [x] 2.17 两数值域效果标签激活／移除的句柄键统一使用公共栈格式化，删除数值插值装箱路径；动作键与效果键分别保留原文化格式和身份含义
- [x] 2.18 两数值域效果快照的来源／目标／来源表标签为空时复用 Array.Empty，删除空数组克隆；非空数组继续独立复制
- [x] 2.19 两数值域效果请求参数／属性／标签直接复制成最终数组再排序校验，生命周期输入直接复制数组，删除中间 List；输入隔离和重复项错误保留
- [x] 2.20 两数值域属性初始化与重算直接遍历已有字典键值对，移除这些入口对 Keys／Values 视图的创建需求，保持计算和 revision 更新顺序

- [x] 2.21 两数值域标签汇总、modifier 添加／移除和闭合校验直接遍历字典键值对，删除剩余 Values 视图访问；私有快照标签汇总使用实际 SortedDictionary，避免接口枚举装箱。Fixed 编译通过；Float32 最初受并行 GraphValueRuntime 语法错误阻断，后续在 7.6 同轮复编通过

- [x] 2.22 两数值域效果状态五类子块在同步解码内借用 ArraySegment，删除十处 ReadBytes 中转数组；每块头部／计数／尾部检查与结果独立存储保留，不改事务恢复所有权

- [x] 2.23 两数值域效果状态五类子块使用已有长度回填直接写入外层 writer，删除各子 writer／MemoryStream／最终中转数组；块格式和次序不变，聚合状态复制及角色外层封装仍未完成

- [x] 2.24 两数值域角色状态的效果封装直接编码并回填长度、解码同步借用原片段，删除效果外层 writer／流／数组及读取副本；角色输出、哈希入口与效果结果独立存储保持不变

- [x] 2.25 两数值域角色状态的装备封装直接编码并回填长度、读取借用有限片段，删除子 writer／流／数组及读取副本；目录／槽位／值类型校验与装备结果独立存储保留

- [x] 2.26 装备运行值类型及变更状态共用所属类型的显式成员校验，迁移构造、局部状态定义和两类解码，删除通用反射解码入口；保留非法成员及各层错误语义

- [x] 2.27 角色控制状态两类枚举解码改为强类型成员匹配，迁移四个读取点并删除 byte／ushort 泛型转换入口；保留原成员集合、字段宽度与错误文本，其他构造层校验仍未完成

- [x] 2.28 角色控制字段构造直接沿用已有合法类型／语义组合判断，删除被该判断完整覆盖的两次 Enum.IsDefined 装箱；保留身份检查、组合约束和原异常

- [x] 2.29 角色控制状态 canonical 校验共用原写入函数并直接比较 writer 内容，删除仅用于校验的完整输出数组；保留重新编码、长度及逐字节一致性检查

- [x] 2.30 角色控制状态写入统一为实际消费所需的长度前缀写入，迁移两数值域角色 codec，删除无剩余消费者的返回数组入口；字段编码与 canonical 比较共用原实现

- [x] 2.31 两数值域 MotionWarp 持续执行的已有状态校验直接匹配 Applied／AppliedClamped，删除运行期 Enum.IsDefined 装箱；保留非法状态错误与原生命周期判断

- [x] 2.32 Fixed 输入源状态通知直接匹配全部四种正式 disposition，删除提交／丢弃／恢复调用中的 Enum.IsDefined 装箱，保留通知顺序和原非法值异常

- [x] 2.33 两数值域 Pipeline Execute 返回结果直接校验 Pending／Committed，删除结果构造中的 Enum.IsDefined 装箱，保留事务身份和提交批次关联约束

- [x] 2.34 远端观测约束在预测帧构造／历史恢复中的采样类型校验改为三成员匹配，删除 Enum.IsDefined 装箱，保留身份、时序和接触配置约束

- [x] 2.35 两数值域角色恢复中的 MotionWarp 限制结果解码直接匹配全部三种序列化成员，删除该读取的 Enum.ToObject／IsDefined，保留与活动执行不同的合法范围及原异常

- [x] 2.36 两数值域角色状态值解码由已有完整 switch 直接判定类型，删除前置枚举转换与查询装箱；保留十一种成员、编号缺口拒绝和原非法值错误

- [x] 2.37 两数值域动作实例恢复的阶段／状态／转换类型按正式成员直接解码，删除六处通用枚举转换调用；保留各自合法零值、读取顺序和非法值错误，动作推进不变

- [x] 2.38 远端身体采样选择的上游构造直接匹配三种采样类型，补齐观测约束生成前的 Enum.IsDefined 装箱清理；采样插值／外推算法和历史所有权不变

- [x] 2.39 预测事件日志条目构造与恢复解码共用四种正式 disposition 判断，删除两处 Enum.IsDefined 装箱；保留各入口身份、重复检查和原异常

- [x] 2.40 运行期预测修正决策构造直接匹配三种决策和七种原因，删除两处枚举查询装箱；保留恢复 Tick、回放区间及原错误约束

- [x] 2.41 角色控制状态读取统一接收 ArraySegment，迁移两数值域嵌套消费者，schema／状态解析和 canonical 比较限定原片段，删除整体 ReadBytes 副本

- [x] 2.42 两数值域完整角色状态恢复的 canonical 校验直接比较 writer 内容，删除校验用完整输出数组及无消费者比较工具；保留唯一编码实现和完整字节校验

- [x] 2.43 动作生命周期运行提交、Ingress 与结束规则解析按正式连续成员边界校验 Transition，删除 Enum.IsDefined 装箱和元数据查询；保留非法值及终止状态约束

- [x] 2.44 两数值域 GameplayEffect 状态恢复按每个 modifier 的 Operation／ClampBound 正式连续值域校验，删除随 modifier 数量重复的四处 Enum.IsDefined 装箱

- [x] 2.45 运行黑板 OwnerToken 构造及 IsValid 按 Character 至 Frame 五种正式作用域值域校验，删除两数值域作用域解析与状态读取中的 Enum.IsDefined 装箱

- [x] 2.46 Float32 本地输入端口的 Prepared／Committed／Discarded／Restored 通知按正式连续值域校验，删除每次事务结果广播前的 Enum.IsDefined 装箱并与 Fixed 口径统一
- [x] 2.47 共享 PipelineTransactionCoordinator 的 Pending／Committed 控制结果直接匹配两种正式 outcome，删除每个外层 tick 返回控制结果时的 Enum.IsDefined 装箱
- [x] 2.48 共享 ExecutionPlan 的 status、step execution kind 与 tick source kind 按正式连续值域校验，删除每次计划／步骤／来源映射构造中的四处 Enum.IsDefined 装箱
- [x] 2.49 共享 Pipeline 每步事务上下文按 Forward 至 Authoritative 正式执行类型值域校验，删除 Coordinator 每执行 step 时的 Enum.IsDefined 装箱
- [x] 2.50 Timeline Runtime Snapshot 的 Once／Loop 模式及 Prepared 至 Disposed 状态按正式连续值域校验，删除 Host 捕获与两数值域恢复共用构造中的两次 Enum.IsDefined 装箱
- [x] 2.51 角色控制 Ability 停止请求直接匹配 Graceful／Force 两种正式模式，删除两数值域动作停止入口共用的 Enum.IsDefined 装箱
- [x] 2.52 两数值域 SimulationStep 的私有 actor 收集列表由基类直接接管，删除每个 step 的第二份 List 复制和 ReadOnlyCollection 包装，保留排序、去重及 IReadOnlyList 输出
- [x] 2.53 两数值域 SimulationStep 的独立 inputs／ingress 列表直接作为 IReadOnlyList 暴露，删除每个 step 两个 ReadOnlyCollection 包装对象
- [x] 2.54 两数值域 ExecutionPlan 的独立 steps／source mappings 列表直接作为 IReadOnlyList 暴露，删除每个外层 tick 计划的两个 ReadOnlyCollection 包装对象
- [x] 2.55 两数值域 SimulationStep 的 actor 收集列表按 inputs.Count 准确预备容量，删除多 Actor step 填充中的列表扩容
- [x] 2.56 共享 Pipeline 每 tick 事务身份按原七段文本和 U+001F 分隔直接写入池化 UTF-8 缓冲，删除三个数字字符串、params 数组、join 字符串和最终 UTF-8 数组
- [x] 2.57 共享 PipelineTransactionControlResult 改为只读值结果，删除 Coordinator 每个 Pending／Committed 外层 tick 返回的内部结果对象分配
- [x] 2.58 Fixed／Float32 Pipeline Execute 统一为 void，删除无人消费的内部／公开 TransactionResult 及三套 Outcome 枚举，移除每个外层 tick 的公开结果对象和完整死返回链
- [x] 2.59 两数值域 CommitBatch 的独立 steps／source egress 列表直接作为 IReadOnlyList 暴露，删除每个已提交外层 tick 的四个 ReadOnlyCollection 包装对象
- [x] 2.60 两数值域 CommitBatch 的事件覆盖列表按 OutputDispositions.Count 正式数量预备容量，删除正常提交路径的 List 扩容
- [x] 2.61 两数值域 CanonicalInputBatch 的独立输入列表直接作为 IReadOnlyList 暴露，删除每个 ingress tick 的两个 ReadOnlyCollection 包装对象
- [x] 2.62 两数值域 TypedIngressBatch 的独立事实列表直接作为 IReadOnlyList 暴露，删除每个 ingress tick 的两个 ReadOnlyCollection 包装对象
- [x] 2.63 两数值域 OutputDispositionSet 的独立 disposition 列表直接作为 IReadOnlyList 暴露，删除每个 egress tick 的两个 ReadOnlyCollection 包装对象及无消费者命名空间引用

- [x] 2.64 两数值域 LocalInputFrame 改为只读值帧，删除每个本地输入 ingress tick 只为同步传递两个 batch 引用而创建的外壳对象；两个 batch 的数据所有权与 Product 写入边界不变

- [x] 2.65 两数值域 TypedIngressBatch 提供不可变 Empty 实例，本地输入与 Fixed 回滚输入源统一复用，删除当前正式空 ingress 路径每 tick 的 batch 及空 List；非空构造和排序校验保留

- [x] 2.66 两数值域 CanonicalInputBatch 内部接管 SourcePort 独占输入数组并原地排序校验，删除每个本地输入 ingress tick 的中间 List 与数组复制；数组不向可变接口暴露

- [x] 2.67 通用 TargetSimulationPipelineStep 对 null 或已知空 typed ingress 直接复用 Array.Empty，删除本地 Fixed／Float32 单步调度每 step 的空 List；非空复制、排序和 Actor 归属校验保留

## 3. Timeline和事件图

- [ ] 3.1 为播放、活动Clip、决策退出和事件候选设置正式有界工作存储，删除周期List及只读包装分配
- [ ] 3.2 将Capture/Restore写入正式历史保留槽，保留分型私有状态与调用代次，迁移所有快照消费者
- [ ] 3.3 迁移正式EventGraph宿主中的周期临时集合与委托，保留既有原生节点执行和变量生命周期

## 4. 动画、IK、相机、输入和AI

- [ ] 4.1 将BlendStack PrepareFrame请求和去重集合改为原节点/实例持有的准备存储，移除每帧分配
- [x] 4.1.1 Blend Stack 运行目标只直接匹配 SourceOwner／SourcePose，删除每次 Push 目标校验的 Enum.IsDefined 装箱；构造期选择可用性策略同步改为两种正式成员判断
- [x] 4.1.2 Blend transition 身份按 SourceOwner／SourcePose／NoPose 显式校验端点和 OwnerIndex，删除每次 Push 比较两个 transition 身份时最多四次 Enum.IsDefined 装箱
- [ ] 4.2 迁移Pose源请求、观察结果和资源目录getter的热点副本，按Frame/Barrier/Seal落实租用寿命，不改原生求值
- [x] 4.2.1 Pose Native 四类连续枚举集中按正式值域校验，删除图准备合同及每帧 PreparationResult 构造／IsValid 中十一处 Enum.IsDefined 装箱，保留原状态组合约束
- [x] 4.2.2 Pose 源释放令牌构造与 IsValid 直接匹配 NativeClip／Acl 后端，删除 ACL StageRelease、journal 和释放消费中的 Enum.IsDefined 装箱
- [x] 4.2.3 Pose 源每帧 readiness key／entry／page 集中按 PreparationKind 与 Category 正式连续值域校验，删除构造、IsValid、Record、Remove 中五处 Enum.IsDefined 装箱
- [x] 4.2.4 Pose 源 preparation 转 readiness target 复用 Kind 值域并直接匹配三种 Input、两种 sampling backend，删除构造与 IsValid 中四处 Enum.IsDefined 装箱
- [x] 4.2.5 物理 Pose 源 metadata 按 NativeClip 对应无资源索引、Acl 对应非负资源索引的正式组合直接校验，删除注册、pending／committed 身份与诊断读取共用的 Enum.IsDefined 装箱
- [x] 4.2.6 ClipPlayer／BlendSpacePlayer 每帧 SetRelevant 按 Entry 至 TransitionSource 四种 DemandKind 值域校验，删除明确逐帧的 Enum.IsDefined 装箱
- [x] 4.2.7 ClipPlayer／BlendSpacePlayer／SelectedPosePlayer 各自复用单元素 SourceRequest 槽，Evaluator 仍同步复制到本帧汇总，删除每帧三份短命数组
- [x] 4.2.8 Pose Graph Evaluator 复用按 handler 数预备的 SourceRequest 汇总列表，Demand 合法路径改为无 HashSet、无错误文本构造的顺序校验，删除每个图每帧的容器与字符串分配
- [x] 4.2.9 Blend Stack 源 binding 复用 pending、request 和 source identity 工作区，并将每个活动源的 PendingSource 改为值记录，删除每帧容器及逐源对象分配
- [x] 4.2.10 Pose StateMachine 以当前态／目标态两槽工作区替代四阶段 yield 枚举，复用子请求汇总并直接持有最多两个输出值，删除每帧 List、迭代器和捕获式 Any 分配
- [x] 4.2.11 Pose Graph Evaluator 初始化时绑定唯一 OutputPose 或 GraphOutput 输入定义，逐帧直接读取已绑定边界，删除输出节点 List／数组及动态端口形状重建
- [x] 4.2.12 Pose StateMachine 构造时按 state 解析 alias 并按 priority／transition id 预排候选迁移数组，删除逐帧 Where／OrderBy／ThenBy／ToArray
- [x] 4.2.13 Pose StateMachine 按最大规则操作数预备 operations／values／visiting 工作区，每次候选规则求值前 Clear 复用，删除逐候选三份容器分配
- [x] 4.2.14 Pose Graph Runtime 在克隆图校验后缓存 node／port／direction 到端口定义的完整映射，BindGraphInput 和动态 ReadInputValue 删除逐次 RuntimeShape List 重建
- [x] 4.2.15 Blend Stack 运行 push 请求的目标端直接匹配 SourceOwner／SourcePose 两种正式值，删除 source 切换入口的 Enum.IsDefined 装箱
- [x] 4.2.16 Timeline Host 的 Ability Tree Clip invocation 按 OnEnable 至 Root 四种正式 hook 值域校验，删除 Root 更新及启停销毁调用中的 Enum.IsDefined 装箱
- [x] 4.2.17 删除 Pose Graph 只写不读的 LastCommittedOutput 及 Role 转发属性，明确端口值只属于当前 evaluation／commit 帧，为逐类型复用移除伪跨帧持有
- [ ] 4.3 沿Foot查询、Goal Assembly、FBBIK和最终写入链治理实际managed临时对象、装箱和vendor接口分配，保留查询容量和算法顺序
- [x] 4.3.1 左右脚正式 Motion Event Frame 按 Unavailable 至 Contact 连续相位值域校验，删除逐帧事件构造中的 Enum.IsDefined 装箱
- [x] 4.3.2 左右脚 Motion Runtime Sample 按 Unlocked 至 Locked 连续锁定模式值域校验，删除逐帧样本构造中的 Enum.IsDefined 装箱
- [ ] 4.4 沿Camera目标/效果、Input采样和Behavior Designer正式任务治理已确认分配，保持输入来源和目标身份

4.4 已完成小步与剩余范围（详见 [审计实施记录](audit.md#2026-09-20-相机独立小步实施)；以下勾选仅表示对应源码修改完成，不表示 Player 实测 0 GC）：

- [x] 4.4.1 删除相机目标键选择的 params 临时数组，保留键选择顺序（`0a1ed8e82`）
- [x] 4.4.2 将目标请求输入改为 IReadOnlyList 并按下标读取，删除枚举器装箱（`fe0162499`）
- [x] 4.4.3 删除相机每帧输入、碰撞和过渡计算中五处枚举校验装箱，保留合法值与原异常（`f3854c086`）
- [x] 4.4.4 轨道采样返回高度与半径的值结果，删除临时作者 Payload 对象（`b96234f54`）
- [x] 4.4.5 目标绑定及快照均失败后才构造错误文本，删除成功解析中的废弃字符串（`affbf9883`）
- [x] 4.4.6 将相机碰撞编号生产者、结果合同和消费者统一为 ColliderInstanceId 整数，删除旧字符串路径（`53f2e3a47`）
- [ ] 4.4.7 治理效果状态创建及各请求／贡献／去重集合的容量，沿正式停止、撤销、完成边界复用存储
- [ ] 4.4.8 完成 Input、Behavior Designer 与第三方相机正式调用中尚未处理的分配治理
- [x] 4.4.9 相机 Sequence／Effect／Response／Target 运行请求构造及 IsValid 按四种 Kind 与两种 Lifecycle 正式值域校验，删除每次激活／退役最多四处 Enum.IsDefined 装箱
- [x] 4.4.10 Fixed Unity 输入适配器的提交／丢弃／恢复 disposition 按四种正式状态值域校验，pending request 恢复按 Immediate／Offensive 值域校验，删除运行通知及按请求数重复的 Enum.IsDefined 装箱
- [x] 4.5 角色 locomotion 表现 Plan、FactLineage、PreparedBinding 与 DomainRuntimeFact 集中按五类正式连续枚举值域校验，删除运行有效性读取及事实构造中的六处 Enum.IsDefined 装箱

## 5. 世界求解、回滚和网络

- [ ] 5.1 迁移DotRecast/KCC正式批求解的接触、路径、结果及solver快照存储，保持碰撞排序和同事务提交
- [ ] 5.2 迁移Rollback调度、输出修正字典和历史保留容器，容量覆盖配置的重放窗口与单帧多步
- [ ] 5.3 将正式热点codec改为写入有界buffer并迁移全部调用者，保留canonical校验与协议bytes，删除重复编码数组及旧返回新数组入口
- [ ] 5.4 迁移客户端远端表现批和到期集合、可靠队列、重传与发送存储，沿完成回调归还，不提前复用payload
- [ ] 5.5 将服务端Scene/Room tick和可靠事件转发改为正式有界存储与Fantasy生命周期，迁移消息及payload拥有关系，保留共享portable源码唯一实现
- [x] 5.6 CanonicalWriter 的 Span 字节写入直接进入原 MemoryStream，删除中转数组租借和复制，保留长度前缀及输出寿命；流扩容、最终数组和网络／回滚结果租用仍未完成
- [x] 5.7 回滚输入及输入束的 canonical 校验复用正式写入函数，比较 writer 流内容而不生成临时 ToArray 副本；完整重新编码校验、尾部检查、异常及公开写入结果的独立寿命保持不变
- [x] 5.8 回滚消息封套及 canonical payload 校验接入同一流内容比较，输入束哈希直接使用原 writer 内容，删除三处只用于比较／哈希的完整字节副本；嵌套消息数组及发送存储仍未完成
- [x] 5.9 回滚协议的输入批次、转发输入、canonical bundle 和确认批次直接编码到外层 writer，回填原四字节长度，删除每个子项的中间 writer／流／数组；读取侧嵌套数组、快照和发送缓冲仍未完成
- [x] 5.10 四类回滚嵌套输入在同步解码内借用原包 ArraySegment，reader 按片段结束位置限制读取和尾部检查，canonical 比较限定同一片段，删除中间 ReadBytes 数组；返回输入数据仍独立持有，长期快照字节保留复制
- [x] 5.11 快照响应编码只读借用自身存储，解码片段直接交给响应构造函数复制一次，删除两处中转副本；响应自身所有权及 Endpoint 复制消费保留
- [x] 5.12 Datagram 分片创建和解码直接提供 Span，由 packet 复制一次拥有 payload，编码只读借用；不可变身份／MTU 的分片容量在通道准备时按原编码计算，清理运行期重复包头编码及两处 kind 装箱。重组、重传和最终数据报缓冲仍未完成
- [x] 5.13 DatagramChannel 的待确认／重组／接收容器按正式消息上限 N 准备，完成历史按原 2N 保留规则预留 2N＋1 存储，覆盖加入后淘汰的临时峰值；重传遍历不创建 Values 视图。消息与分片对象仍未复用
- [x] 5.14 单包容量判断复用正式封套编码函数后读取 writer.Length，删除只为长度产生的 ToArray 结果；不缓存可变 payload、不推进消息序号，批次裁剪和发送行为不变
- [x] 5.15 peer 本地发包冗余历史按 N＋1 准备 SortedList，候选列表按 N 准备并在 finally 清空，保留 tick 排序和最旧帧淘汰；批次仍独立复制。补齐通道准备失败时已创建 Endpoint 的释放，不改模拟回滚历史
- [x] 5.16 Datagram 接收线程同步借用本次 socket 缓冲片段解码，codec 统一接收 ArraySegment 并按片段限制读取，删除整包中转复制；返回 packet 仍独立持有 payload 和身份字符串，接收队列寿命不变
- [x] 5.17 分片重组保留不可变 packet 引用，完成时从只读 payload 拼接最终结果，删除逐片克隆及无消费者的 DatagramPacket.CopyPayload；保留重复片判定、长度校验和最终消息独立数组
- [x] 5.18 CanonicalWriter 整数写入改用按数值宽度确定的栈缓冲，删除每个 writer 的八字节托管数组，保留小端编码和同步写流；writer、流和最终数组分配仍未完成
- [x] 5.19 CanonicalWriter 哈希统一复用 SimulationCanonicalPayloadHash 的片段入口，使用 string.Create 直接填充最终小写十六进制字符串，删除重复格式化与中间字符数组；SHA 对象和摘要字节数组仍未完成
- [x] 5.20 网络检查点布局与内容哈希直接使用 writer.ComputeHash，删除两处仅为哈希生成的完整 ToArray 副本；检查点持有、编码字段和恢复生命周期不变
- [x] 5.21 权威同步数据报编码直接读取 packet 的只读 payload，删除编码前克隆；包头和解码共用显式合法 kind 判断，删除两处枚举装箱，保持独立 payload 消费者与异常语义
- [x] 5.22 权威数据报接收与解码同步借用实际缓冲片段，删除整包和 payload 两层中转数组；packet 构造复制一次拥有数据，保留发送端 byte[] 的 null 校验和统一构造实现
- [x] 5.23 快照数据报 delta 编码直接读取只读视图，解码片段交给统一构造复制一次，删除两处中转副本；重建模块所需 CopyDeltaPayload 和快照自身存储保留
- [x] 5.24 权威输入和基线 canonical 校验共用正式写入函数并直接比较 writer 内容，删除仅供比较的完整结果数组，保留重新编码、尾部与字段校验及公开独立写入结果
- [x] 5.25 权威输入解码删除 Enum.ToObject 和 Enum.IsDefined 泛型入口，来源类型直接校验三个成员，输入值沿原六分支解码并拒绝其它值；保留非法值异常文本及读取顺序
- [x] 5.26 四处权威输入嵌套编码统一直接写外层 writer 并回填长度，删除子 writer、流和中间数组；保留唯一输入编码实现及原协议字段、次序和结果寿命
- [x] 5.27 四处权威嵌套输入同步借用 ArraySegment 解码并限定 canonical 比较范围，删除 ReadBytes 子数组；返回输入独立持有数据，基线长期状态字节复制保持不变
- [x] 5.28 权威快照发包前的模型 MTU 检查共用正式编码函数读取流长度，删除仅供尺寸检查的最终数组；传输端预算与原超限处理保留，不缓存 payload 或跳过检查
- [x] 5.29 Egress 的本地输入、权威复制和远端表现 canonical 校验复用各自唯一写入函数，直接比较 writer 内容，删除三类比较用数组；公开独立结果与嵌套消息寿命不变
- [x] 5.30 权威复制消息内的基线和远端表现直接写入外层并回填原长度前缀，删除每个子消息的 writer、流和输出数组；公开独立编码与读取侧存储保持不变
- [x] 5.31 权威复制内的基线和远端表现同步借用子消息片段解码，读取及 canonical 比较按片段限界，删除外层 ReadBytes 副本；基线自身状态字节及表现结果独立持有
- [x] 5.32 Egress 的 gameplay fact 和表现命令类型直接按正式成员解码／校验，删除两处反射枚举转换与装箱，保留非法字节报错、字段读取顺序及 TimelineProgress 原解码链
- [x] 5.33 Egress 剩余动作转换／阶段／状态及效果操作／应用模式均显式匹配合法成员，删除泛型 ReadEnum 和该 codec 全部反射枚举入口；生命周期行为不变
- [x] 5.34 基线编码直接读取已有 CharacterStateBytes.Span，检查点 Capture 返回同一已校验对象，删除编码副本和重复检查点构造／复制／哈希；结果自身独立存储和完整校验保留
- [x] 5.35 紧凑检查点哈希写入使用 32 字节栈缓冲，读取借用固定片段直接生成最终字符串，删除写入字节数组及读取字节／字符数组；长度与小写编码不变
- [x] 5.36 公共数值配置 codec 按正式舍入／溢出模式直接校验，删除两处 Enum.ToObject 和无消费者泛型入口，保留非法字节报错及数值配置构造规则
- [x] 5.37 回滚输入 codec 的 provenance、值类型、Tick 来源直接匹配正式枚举成员，删除三处 Enum.IsDefined 装箱，保留非法值报错及原解码次序
- [x] 5.38 回滚输入帧构造与 codec 共用帧类型的 provenance 合法成员判断，删除构造时枚举装箱及重复规则，保留两层不同错误语义和输入哈希
- [x] 5.39 回滚协议消息种类和组件角色解码直接匹配正式成员，删除两处 Enum.IsDefined 装箱，保留角色编号缺口、非法值报错及协议分派
- [x] 5.40 公共身份哈希复用统一 SHA-256 结果格式化，删除逐字节 x2 小字符串和 StringBuilder，保留原字符串分隔符、UTF-8 输入与哈希身份规则
- [x] 5.41 统一 SHA-256 入口使用固定栈摘要和字符缓冲生成最终字符串，删除返回摘要数组的调用，保留实际片段与小写哈希；SHA 实例和底层分配仍待采样

- [x] 5.42 权威主机身份构造、IsValid 与产品描述共用显式路由成员校验，删除三处 Enum.IsDefined 装箱；保留两种合法路由、默认值拒绝、原错误与就绪判定

- [x] 5.43 会话／Pipeline／后端描述及权威身份哈希的十处枚举数值格式化改为 checked ulong 转换，删除 Convert 对象入口装箱，保留十进制文化格式及负值溢出拒绝；属于描述和身份构造阶段，未计为逐帧收益

- [x] 5.44 两套输入 codec 的空值／空请求数组及两套回放的空请求数组统一 Array.Empty，删除六处零长度数组分配，保留非空数据独立存储、计数校验与输入规范化

- [x] 5.45 CanonicalWriter 字符串编码统一使用固定栈缓冲分块写入，块边界保留完整代理对，删除整字符串 ArrayPool 租借；长度前缀、UTF-8 编码及输出所有权保持原路径，流扩容和编码器内部行为仍未实测

- [x] 5.46 CanonicalReader 的零长度原始字节读取复用 Array.Empty，覆盖 ReadBytes 的统一调用链，保留负长度／剩余字节检查及非空独立复制

- [x] 5.47 两数值域世界状态恢复的 canonical 校验直接比较 writer 内容，删除比较用完整输出数组及无消费者工具，保留求解器载荷独立存储和完整校验

- [x] 5.48 两数值域世界状态恢复的持久化模式直接匹配 Reconstruct／Snapshot，删除 byte 参数的 Enum.IsDefined 装箱，保留字段宽度及原非法值错误

- [x] 5.49 两数值域世界状态 Bodies 直接复制到最终数组后排序校验，保留只读包装及非空载荷克隆；空载荷复用 Array.Empty，删除中间 List 和空数组克隆

- [x] 5.50 两数值域世界状态解码借用 CanonicalReader 的 payload 片段，并由最终状态从只读片段复制一次，删除解码数组与构造克隆的双重复制；状态继续独立持有载荷

- [x] 5.51 DotRecast 每批世界求解的 ActorContactCandidate 直接匹配本地模拟／观测运动性，删除按候选角色重复的 Enum.IsDefined 装箱；保留候选身份和接触求解输入

- [x] 5.52 KCC 碰撞特征身份构造及 IsValid 按五种连续特征值域校验，删除胶囊距离／射线／三角形查询中的重复 Enum.IsDefined 装箱；碰撞图元准备同步按三种正式值域校验

- [x] 5.53 KCC 每步按 Actor 构造身体状态时按三种 Ledge 正式值域校验，删除重复 Enum.IsDefined 装箱；状态恢复的 Feature／Ledge 解码同步改为正式连续值域判断

## 6. UI、资源、渲染和生命周期

- [ ] 6.1 将状态History等热点getter改为正确寿命的视图，迁移UI刷新消费者，清理重复格式化与每次列表副本
- [ ] 6.2 沿运行特效/音频/角色/资源实例的正式准备与租用入口治理Active期间分配，保留加载取消和退出释放
- [ ] 6.3 沿正式RendererFeature/RenderPass及材质消费治理实际managed分配，分别记录native/GPU资源释放，不重写正确的复用实现
- [ ] 6.4 清理项目Editor/启动/离线工具中的已确认重复订阅和生命周期遗留，保持重操作显式触发并与运行0 GC结果分组
- [x] 6.5 资源快照发布复用同一次对象池查询结果，标签与作用域直接填充最终独立数组，删除中间 List 和复制；保留历史快照寿命、排序及维护前后重新采集，不表示资源链或 History 已无分配
- [x] 6.6 资源池统计统一调用正式填充 List 接口，运行时持有工作列表并在同步统计后清空引用，迁移发布和维护前后全部调用；初始容量来自当前池数量，新增池引起的容量增长及池内部统计分配仍未完成
- [x] 6.7 资源、启动状态、检查点和故障事件四类历史队列按正式历史上限 N＋1 在构造时准备，覆盖先加入后淘汰峰值；保留历史结果副本、事件顺序与 N 条保留规则，记录对象分配仍未完成

## 7. 诊断与正式性能交付

- [ ] 7.1 将运行诊断记录改为typed有界存储，移除采样窗口内字符串构造和热点索引重建，保留采样身份和完整性失败
- [ ] 7.2 扩展现有性能workflow的分配归因与报告数据，纳入客户端/后台/服务端、首次/峰值/稳态与采样器开销，不创建另一控制面
- [ ] 7.3 将采样导出与后台消费接入明确阶段及释放边界，输出Unavailable、溢出和未覆盖路径的真实状态
- [ ] 7.4 按领域小步中文提交，交付分配入口删除与消费者迁移清单、容量来源和原workflow产物链接，未完成项保持未完成
- [x] 7.5 正式性能 loopback 的专用线程改为同步读写，删除异步任务后立即等待的入口；发送缓冲按既有 256 字符上限及实际 HELLO 长度准备，复用 UTF-8 字节存储并直接追加换行。字符串与队列分配仍未完成
- [x] 7.6 两数值域边界／Pipeline／模型／世界诊断记录及 Pass 阶段按正式枚举成员直接校验，删除十处 Enum.IsDefined 装箱，保留原合法集合、短路与错误；记录字符串和采集存储仍未完成
- [x] 7.7 Fixed 输入回放哈希按固定字段数量直接填充最终字符串数组，删除每帧 List 与 ToArray 中间复制；保留字段顺序、格式、原哈希入口和证据采集时机，格式化字符串及哈希内部分配仍未完成
- [x] 7.8 两数值域回放来源身份在 PrepareReplay 构造一次，普通重映射及 Fixed 暂停保持输入复用，原重置入口清除模块引用；输入和请求结果仍独立。最初受并行 CameraProgramRequestFactory 类型引用错误阻断，后续 5.44 同轮两域编译通过
- [x] 7.9 诊断实时状态到达既有容量后复用被淘汰的链表节点，删除每个新键替换时的节点分配；保留最近使用顺序、淘汰计数、版本与全量同步条件，初次填充和 Clear 后重建仍分配
- [x] 7.10 捕获增量读取按 revision 定位连续后缀并一次复制，全量读取按现有记录数准备列表容量，删除结果列表增长中的重复存储分配；返回结果仍独立，采集存储和最终结果自身分配未完成
- [x] 7.11 实时状态读取按准确数量直接填充独立数组，删除中间 List 和增量读取扩容；当前确认消费者为 Editor 调试面板，不计为 Player 每帧收益
- [x] 7.12 会话诊断条目及快照构造直接匹配组件状态／生命周期／准备状态，删除三类枚举查询装箱；按需 Diagnostics 读取路径已确认，实际刷新频率未采样
- [x] 7.13 会话诊断组件直接复制到最终数组后排序校验，保留只读包装和独立快照，删除中间 List 对象；一般枚举输入内部增长及最终结果分配仍存在
- [x] 7.14 两数值域会话诊断生产者按六个固定组件加 Pass 数直接填数组，删除生产侧 List 及扩容；保留原顺序、动态状态读取和下游快照复制
- [x] 7.15 两数值域 Pass 阶段名称在句柄组装时按 Pass 数准备，诊断读取复用文本，移除运行读取中的枚举 ToString；初始化格式化保留，生命周期状态仍实时获取
