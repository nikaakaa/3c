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

- [x] 2.68 Float32 控制源事务能力一致性与本地会话已提交观测能力改用位掩码判断，清除运行代码最后两处 Enum.HasFlag 装箱；两处均为初始化／组装路径，不计为每帧收益

- [x] 2.69 Pipeline step 投影捕获按 Include／ReconstructForRestore 连续值域校验 participant 模式，删除每次投影遍历的 Enum.IsDefined 装箱及重复属性读取

- [x] 2.70 Fixed Neutral 控制源按 Prepared 至 Restored 连续值域校验事务 disposition，删除每次状态通知的 Enum.IsDefined 装箱，并与正式 UnityFixedCharacterInputAdapter 现有校验统一

- [x] 2.71 通用 ExecutionPlan 对 null 或已知空 steps／source mappings 直接复用 Array.Empty，删除 Pending／NoStep 外层 tick 的两个空 List；非空计划继续独立复制、排序和完整性校验

- [x] 2.72 TargetSimulationPipelineStep 按已物化 input 数直接生成 ActorId 数组并由 base 接管，删除每 step 的 Actor List 外壳；排序、非空、身份合法与重复 Actor 校验保持

- [x] 2.73 TargetSimulationPipelineStep 对数组 inputs 克隆后原地排序并直接只读保存，删除本地及其它数组输入 step 的 List 外壳；非数组枚举继续复制到独立 List

- [x] 2.74 ExecutionPlan 对非空数组 steps／source mappings 克隆后直接只读保存，mapping 在副本上排序，删除本地与权威单步计划的两个 List 外壳；非数组枚举路径不变

- [x] 2.75 两数值域 OutputDispositionSet 按 IReadOnlyList.Count 复制到最终数组并原地排序校验，空集合复用 Array.Empty，删除每个 egress tick 的结果 List 外壳并保留独立所有权

- [x] 2.76 两数值域 CommitBatch 按 IReadOnlyList.Count 将 completed steps 与 source egress 复制到最终数组，空集合复用 Array.Empty，删除每个已提交 tick 的两个结果 List 外壳并保留跨提交独立寿命

- [x] 2.77 两数值域 CommitBatch 按 OutputDispositions.Count 直接填充事件所有者数组并原地排序校验，删除每个已提交 tick 的覆盖校验 List 外壳，事件数量不匹配继续抛原参数错误

- [x] 2.78 PipelineStateSnapshot 按 IReadOnlyList.Count 复制 participant 到最终数组并原地排序校验，空集合复用 Array.Empty，删除每次完整／step 投影快照的结果 List 与 ReadOnlyCollection 包装

- [x] 2.79 Pipeline 完整状态捕获按 participant 数、step 投影按 Include 数直接填充 snapshot 数组，删除每次捕获的上游收集 List；投影模式仍先完整校验再捕获

- [x] 2.80 PipelineStateSnapshot 增加程序集内部数组接管入口，Coordinator 完整／step 投影捕获直接转移新建 participant 数组，删除每次捕获的第二份数组复制；公开 IReadOnlyList 构造仍独立复制

- [x] 2.81 Pipeline 状态 Coordinator 四个入口统一接收 IReadOnlyList，ValidateParticipantSet 按运行 participant 与 plan 期望 participant 数直接填充并排序数组，删除每次 checkpoint／snapshot／restore 校验的两只 List 外壳

- [x] 2.82 Pipeline checkpoint 捕获按已验证 participant 数直接填充 checkpoint 数组并交给 CheckpointSet，删除每个外层事务的收集 List 与 ReadOnlyCollection 包装；失败仍逆序释放已接纳项

- [x] 2.83 PipelineTransactionRuntimeServices 在组装时创建不可变已验证 participant set，事务期 checkpoint／snapshot／restore 直接复用排序数组，删除每次调用的运行与期望 participant 数组分配；原始列表和 plan 不匹配仍完整失败

- [x] 2.84 FinalizedStepResult append Product 直接承载 SimulationActorTickResult，迁移 Fixed／Float32 本地、Rollback 与 ServerAuthoritative 全部读写端，删除两套 FinalizedActorResult 类型及每 Actor 每 step 的包装对象

- [x] 2.85 EventGraphValue typed 读取按已验证具体值类型直接重解释返回，删除 bool／int／float／Vector2／Vector3／Quaternion 的 object 装箱拆箱桥接；显式 ToObject 与枚举 object 边界保持

- [x] 2.86 两数值域角色状态恢复按 Timeline 模式、状态、停止原因和黑板作用域的正式连续值域直接解码，删除最后一条泛型 Enum.ToObject／IsDefined 反射与装箱路径

- [x] 2.87 角色控制 Motion binding catalog 按双值 EvaluationMode 直接解码，删除单一调用泛型 Enum.IsDefined／ToObject 入口；catalog 格式和 canonical 校验保持

- [x] 2.88 两数值域 Timeline snapshot 加入／移除按下标扫描并按准确结果容量复制，删除 stop 的捕获 All、两条 RemoveAll 捕获委托及 LINQ 依赖；角色状态克隆边界保持

- [x] 2.89 两数值域运行 OperationModule 的字符串 catalog field 查询统一为 IReadOnlyList 下标扫描，删除 Constant／Identity／TryIdentity 六处捕获 FirstOrDefault 和 LINQ 依赖

- [x] 2.90 两数值域 SimulationActorState 改为只读值状态，CompleteStep、初始组装和恢复直接写入 Actor 数组，删除每 Actor 的状态外壳对象；restore 缺失查询同步改为 TryFindActor

- [x] 2.91 共用 SimulationSessionExecutionPlan 将 steps 与 source mappings 按 IReadOnlyList 准确复制到最终数组并原地排序校验，删除 rollback／server-authoritative 计划构造的第二只结果 List 与容量冗余；计划仍独立持有输入

- [x] 2.92 SimulationSessionExecutionPlan 增加显式数组所有权入口，Fixed／Float32 本地单步与 Float32 权威单步直接转移本方法新建 mappings／steps 数组，删除这些每 tick 计划的两次数组克隆；公开构造继续独立复制

- [x] 2.93 两数值域 SimulationActorTickResult 将 gameplay facts／presentation commands／trace records 按可计数输入直接复制到最终数组，删除每 Actor 每 completed tick 的三只 List 与三只 ReadOnlyCollection；结果继续独立持有输入

- [x] 2.94 两数值域 CharacterEvaluationResult 直接接管角色评估末尾从五只聚合 List 物化的 facts／presentation／trace／timeline advance／timeline stop 数组，删除每 Actor 每 step 的五只结果 List、五只 ReadOnlyCollection 及二次元素复制

- [x] 2.95 两数值域 CharacterEvaluationResult 在 Consume 成功后通过一次性 TakeOutputs 转移 facts／presentation／trace 数组，AbilityFinalize 以内部 owned 入口构造 SimulationActorTickResult，删除每 Actor 每 completed tick 的三次数组复制
- [x] 2.96 两数值域 CompleteStep 直接按 finalized 数量组装 SimulationActorTickResult 最终数组并转交 SimulationTickResult，删除 workspace ActorResults List 及其重复复制；公开构造仍隔离外部输入

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
- [x] 4.4.7 治理效果状态创建及各请求／贡献／去重集合的容量，沿正式停止、撤销、完成边界复用存储
- [ ] 4.4.8 完成 Input、Behavior Designer 与第三方相机正式调用中尚未处理的分配治理
- [x] 4.4.9 相机 Sequence／Effect／Response／Target 运行请求构造及 IsValid 按四种 Kind 与两种 Lifecycle 正式值域校验，删除每次激活／退役最多四处 Enum.IsDefined 装箱
- [x] 4.4.10 Fixed Unity 输入适配器的提交／丢弃／恢复 disposition 按四种正式状态值域校验，pending request 恢复按 Immediate／Offensive 值域校验，删除运行通知及按请求数重复的 Enum.IsDefined 装箱
- [x] 4.4.11 Fixed Unity 输入适配器在装配后缓存正式 SourceIdentity，删除每个 BuildInput／CaptureState 的重复字符串插值；身份文本和状态校验语义不变
- [x] 4.4.12 Fixed Unity 输入适配器的 PendingRequest 改为值记录；调度结果显式写回当前槽位，删除采样、手动入队和状态恢复中的每条请求对象分配
- [x] 4.4.13 Fixed Unity 输入适配器复用恢复 scratch 列表；pending state 校验通过后原子提交，失败或完成后清空引用，删除每次恢复的 PendingRequest 临时数组
- [x] 4.4.14 Fixed Unity 输入适配器在 binding 构造期缓存 request id UTF-8；恢复命中正式 binding 时复用同一字符串，未知 request id 仍解码为独立字符串
- [x] 4.4.15 Fixed 控制源在构造期缓存 source identity UTF-8；恢复状态头直接比较 UTF-8 segment，删除每次恢复的 source identity 字符串
- [x] 4.4.16 Fixed Local Input Source Port 复用按锁定 roster 准备的 actor input 数组；读取成功后交给 OuterTransaction batch，异常时清空 scratch，不改变 source tick 和校验顺序
- [x] 4.4.17 Float32 Local Input Source Port 复用按锁定 roster 准备的 actor input 数组；读取成功后交给 Canonical batch，异常时清空 scratch，不改变 source tick 和校验顺序
- [x] 4.4.18 两数值域 Canonical Input Batch 改为 readonly struct，排序 lambda 固定为静态函数；Exclusive Product Slot 空值合同同步区分值与引用产品，删除每 tick 输入批外壳
- [x] 4.5 角色 locomotion 表现 Plan、FactLineage、PreparedBinding 与 DomainRuntimeFact 集中按五类正式连续枚举值域校验，删除运行有效性读取及事实构造中的六处 Enum.IsDefined 装箱

## 5. 世界求解、回滚和网络

- [ ] 5.1 迁移DotRecast/KCC正式批求解的接触、路径、结果及solver快照存储，保持碰撞排序和同事务提交
- [x] 5.2 迁移Rollback调度、输出修正字典和历史保留容器，容量覆盖配置的重放窗口与单帧多步
- [x] 5.3 将正式热点codec改为写入有界buffer并迁移全部调用者，保留canonical校验与协议bytes，删除重复编码数组及旧返回新数组入口；帧构造内的 InputHash/GameplayHash 计算与状态快照 codec 归属帧生命周期与状态事务任务（2.x），不在本项内
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

- [x] 5.54 Float32 ObservedWorldConstraintFrame 以最终数组保存约束，空帧复用 Array.Empty，程序集内解码／历史转换直接转移新建数组，删除每步空 List、ReadOnlyCollection 及内部数组二次复制

- [x] 5.55 两数值域 WorldSolveBatchRequest 按复用 workspace 请求数组的 Count 复制到独立最终数组并原地排序校验，删除每 step 的结果 List 与 ReadOnlyCollection 包装；Evaluate 完成后清空 workspace 不影响 Batch

- [x] 5.56 两数值域 WorldSolveBatchResult 按 solver 结果集合 Count 复制到独立最终数组并原地排序校验，删除每批求解结果的 List 与 ReadOnlyCollection 包装；保留 solver 输入数组与 Batch 之间的隔离

- [x] 5.57 WorldSolveBatchResult 提供显式 FromOwnedResults 所有权入口，DeterministicKcc／DotRecast／UnityCharacterController solver 直接转移方法内新建结果数组，删除每批求解的第二份结果数组复制；普通集合构造仍独立复制

- [x] 5.58 两数值域 CharacterWorldSolveResult 改为只读值结果，删除 KCC／DotRecast／UnityCharacterController 每 Actor 每 step 的结果对象分配；数组、Batch 校验和公开读取契约保持

- [x] 5.59 两数值域 CharacterWorldSolveRequest 改为只读值请求，Evaluate workspace 以 default 清空引用字段，删除每 Actor 每 step 的请求对象分配；Batch 身份校验和三套 solver 消费契约保持

- [x] 5.60 两数值域 AbilityEvaluatePass 按锁定 roster 持有可重置 WorldSolveBatchRequest workspace，复用 Batch 对象与内部请求数组，删除每 simulation step 的请求 Batch 和数组分配；RequestHash 仍逐 step 重算

- [x] 5.61 两数值域 SimulationWorldStateSet 按 actor workspace Count 复制到独立最终数组并原地排序校验，删除每 completed step 的 Actor List 与 ReadOnlyCollection 包装；状态集继续独立持有 Actor 状态引用

- [x] 5.62 两数值域 SimulationWorldSnapshot 以最终 Actor 数组保存，并由程序集内部构造接管 Factory／Codec／权威合并的新建数组与 world-state bytes，删除快照结果 List、ReadOnlyCollection、Actor 数组二次复制和字节克隆；公开构造仍独立复制

- [x] 5.63 两数值域 SimulationWorldSnapshotFactory 按可计数 Actor 状态输入直接复制到排序工作数组，删除每次快照捕获的 List 对象；工作数组继续隔离可复用 step workspace 并维持任意输入顺序兼容

- [x] 5.64 Session Host 回滚分支提交复用按正式 32 条 checkpoint 上限准备的 tick 清理缓冲，删除未来 checkpoint 清理的 Where 迭代器与 ToArray；删除期间不直接修改枚举中的字典

- [x] 5.65 Session Host 的表现 checkpoint 能力检查改为注册列表下标遍历，最旧 checkpoint 查询改为排序字典具体枚举器，删除剩余 All／First LINQ 与命名空间依赖

- [x] 5.66 两数值域 WorldSimulationState 按可计数 body 输入直接复制到最终数组并原地排序，直接以 IReadOnlyList 保存，删除 LINQ ToArray 与 ReadOnlyCollection 包装；公开状态继续独立持有 body 和 solver payload

- [x] 5.67 WorldSimulationState 提供显式 FromOwnedState 所有权入口，codec、KCC step／create、DotRecast step、Unity step 与权威基线合并直接转移方法内新建 body／payload 数组，删除对应第二份数组复制；普通构造仍独立复制

- [x] 5.68 两数值域 WorldSimulationState 统一提供一次复制的 Clone，迁移 KCC／DotRecast／Unity create、reconstruct 与 step 结果边界，删除三套重复 CloneState 和 solver payload 的二次克隆

- [x] 5.69 权威预测历史 replay 结果直接返回独立 List，确认裁剪一次遍历构造保留字典，删除 ReadOnlyCollection 包装、remove List 与已复制字典的逐项删除

- [x] 5.70 权威预测 disposition confirmation 在遍历当前 journal 时直接写入独立目标字典，checkpoint 直接保存独立 List，删除 updates List 与 ReadOnlyCollection 包装；确认／拒绝顺序和 cursor 保持

- [x] 5.71 Rollback 输入／快照／peer hash 历史按排序 tick 反复移除最小 key 至确认边界，删除共用 RemoveThrough 的临时 key List；输入历史捕获直接返回独立 List

- [x] 5.72 Rollback Endpoint 按 Actor 的显式输入历史在确认释放时按排序 tick 逐个弹出最小记录并保留最后确认帧，删除每次释放、每 Actor 的 remove List

- [x] 5.73 Rollback RuntimeBridge 三张 tick 索引 report／request 表统一为 SortedDictionary，历史窗口释放按最小 tick 弹出，删除每次 Pump 的 remove List 并固定诊断／恢复遍历顺序

- [x] 5.74 RollbackIngressBatch 直接接管 Endpoint 本 tick 新建的 relayed explicit／canonical arrival 数组并原地排序校验，删除两只结果 List、两只 ReadOnlyCollection 与 canonical 数组二次复制

- [x] 5.75 Rollback Actor input／relayed input／canonical bundle／confirmation 四类协议对象按可计数输入直接复制到最终数组并原地排序，删除每包结果 List 与 ReadOnlyCollection 包装；公开构造继续独立持有输入

- [x] 5.76 Rollback 输入协议增加显式数组所有权入口，解码、canonical 组装、预测重建、relay 转发与 confirmation 捕获直接转移本方法新建数组，删除这些路径的协议结果二次复制；可复用发送列表继续走公开复制构造

- [x] 5.77 RollbackStateHashReport 按可计数输入直接保存最终 Actor hash 数组，并接管 hash 生产与协议解码新建数组，删除每个报告的结果 List、ReadOnlyCollection 和数组二次复制；公开构造继续独立持有输入

- [x] 5.78 Rollback policy 与 Server manifest 按两个正式 missing-input 值及唯一 snapshot-authority 值直接校验，泛型 enum parser 拆为具体解析，删除模型构造和配置解析中的 Enum.IsDefined 装箱

- [x] 5.79 Rollback relay 按 input redundancy 正式上限持有 accepted input 工作列表，assembler 直接填充调用方工作区，转发数组生成后在 finally 清空引用，删除每个输入包的 accepted List 与 ReadOnlyCollection

- [x] 5.80 Rollback Endpoint 跨 tick 复用 relayed explicit arrival 工作列表并在 finally 清空引用，只为 ingress batch 生成最终独立数组，删除每次 Read 的 List 本体并保留观测峰值容量；不按整个历史窗口预分配大存储

- [x] 5.81 canonical payload hash 增加 ReadOnlySpan 入口，Rollback hash egress 直接读取 world solver payload span，删除每个 hash tick 为 KCC hash 创建的完整 payload 数组副本

- [x] 5.82 Rollback schedule 按确定的 replay 区间与 current-step 条件准确分配最终 steps／source mappings 数组并转交 ExecutionPlan，删除每 outer tick 的两只动态 List、增长存储及计划二次复制

- [x] 5.83 Float32 预测权威 schedule 按 replay 数与 0～2 个 current step 准确分配最终 steps／source mappings 数组并转交 ExecutionPlan，删除每 outer tick 的两只动态 List、增长存储及计划二次复制；remote-body 输出寿命保持独立

- [x] 5.84 RollbackOutputCommitter 跨 Actor/tick 复用 existing slots、current records、seen slots 与 confirmed release 四只工作集合，并在 finally 清空引用，删除每 Actor 三只集合及每次确认释放一只 List 的重复创建

- [x] 5.85 RollbackOutputCommitter 跨 Commit 复用 disposition 索引与 output operation 列表，外层 finally 清空键值和记录引用，删除每次提交的 Dictionary／List 对象及稳定容量后的底层存储分配；records 事务副本保持独立

- [x] 5.86 RollbackOutputCommitter 以正式 registry／tentative workspace 两张 Dictionary 轮换，成功后交换、失败或完成后清空非正式表，删除每次 Commit 的 records Dictionary 克隆分配并保持发布前事务隔离

- [x] 5.87 Rollback output disposition pass 跨 Execute 复用 disposition 组装列表并在 finally 清空，统一 Fixed／Float32 本地 pass 的现有模式，删除每 outer tick 的 List 对象及稳定容量后的增长存储；最终 set 继续独立复制排序
- [x] 5.88 RollbackOutputCommitter 按正式 registry 容量池化 GameplayFact／PresentationCommand 输出 record，替换、取消、确认释放后回池；提交失败只回收本批新建对象，旧正式 record 保持事务原子性
- [x] 5.89 Rollback schedule 直接消费 Fixed Character Runtime 已排序的 roster descriptor，删除每 outer tick 的 actor 数组复制、descriptor 重建和 roster hash 重算
- [x] 5.90 Rollback input／snapshot／state-hash 历史边界与确认裁剪直接使用具体 KeyValuePair enumerator，删除 SortedDictionary Keys／Values 包装集合
- [x] 5.91 RollbackOutputCommitter 的工作集合与正式／tentative 输出修正表按 MaximumOutputRecords 一次准备，删除重放窗口内的集合底层存储扩容
- [x] 5.92 远端表现 Target 直接复用 TickQueue 到期 workspace，发布委托固定在 target 生命周期，删除每帧 due List／闭包和诊断 Values 包装
- [x] 5.93 Prediction Evidence Drain 按 Module 生命周期复用 baseline／body／sample／event workspace，最终 Observation Batch 继续独立复制排序
- [x] 5.94 Rollback reliable pending 按 Channel 生命周期池化 wrapper 和最大分片槽位数组，ACK 后清空回池；不可靠路径、packet 本体、payload 和 Endpoint 发送字节仍在后续小步处理
- [x] 5.95 Rollback reliable pending 的 packet 本体和最大分片 payload 缓冲随 wrapper 复用，Reset 显式携带实际 payload 长度；接收重组、不可靠消息和 ACK packet 继续独立分配
- [x] 5.96 Rollback Datagram Endpoint 用线程复用 bounded writer 和容量内发送缓冲编码入队，PendingSend 携带实际长度并在发送或 Dispose 后回池；删除无调用方的 byte[] Write 入口
- [x] 5.97 Rollback Datagram Channel 复用 Channel 生命周期的零 payload ACK packet，同步编码后更新身份；接收 packet、reassembly 和 Endpoint clone 仍在后续小步
- [x] 5.98 Rollback Datagram Endpoint 复用 UDP ReceiveFrom endpoint scratch，删除每个轮询周期的 IPEndPoint；实际入队接收记录继续独立 clone
- [x] 5.99 Rollback Datagram Channel 按正式消息容量池化 reassembly wrapper 和最大分片槽位，完整消息复制到独立 byte[] 后释放 wrapper；接收 packet、payload 和协议结果继续独立分配
- [x] 5.100 Rollback Datagram Channel 复用按最大消息容量准备的组装 buffer，Protocol Read 接收 ArraySegment 并删除 byte[] 入口；协议 envelope 和 payload 结果继续独立分配
- [x] 5.101 Rollback Datagram Channel 复用不可靠发送 packet 和单分片 payload buffer，同步编码后更新身份；接收 packet 和 reliable pending 所有权不变
- [x] 5.102 Rollback Datagram Channel 发送路径直接用既有身份、消息序号和 payload 编码协议 header，删除一次性发送 envelope；接收 envelope 和独立 payload 所有权不变
- [x] 5.103 Rollback received datagram 改为 assembly 内只读引用记录，接收队列不再为每条收包创建 wrapper 对象；接收 packet、payload 和真实 UDP endpoint clone 继续独立持有
- [x] 5.104 Rollback Datagram Endpoint 按接收队列容量复用真实来源 endpoint 记录，消费完成后显式归还；Peer 和 Relay 异常路径同步归还，新建 Channel 仍独立 clone
- [x] 5.105 Rollback Datagram Endpoint 池化接收 packet 和 payload buffer，Codec 直接重置租用对象；完整重组、ACK 和重复完成后显式归还，incomplete 分片继续由 reassembly 持有
- [x] 5.106 Protocol envelope header 使用 Channel 准备的 identity binding 比较 wire UTF-8，命中预期身份时复用同一 string；payload 字符串和 Datagram header 身份仍在后续小步
- [x] 5.107 Datagram Endpoint 构造期接收 Session 和正式 sender 清单，Datagram header 命中预期身份时复用 canonical string；错配仍解码后交给 Channel 异常校验
- [x] 5.108 DotRecast 批求解直接把新建的正式 WorldSimulationState 转交给 WorldSolveBatchResult，删除返回前的 bodies 克隆；state 自身不可变，后续批只会替换 m_Current 引用。results 数组仍为正式结果，KCC 和捕获/恢复边界仍在后续小步
- [x] 5.109 KCC 批求解直接把新建的正式 WorldSimulationState 转交给 WorldSolveBatchResult，删除返回前的 bodies 和 solver payload 克隆；state 自身不可变，m_KccStates 每批替换精确数组引用。results 数组仍为正式结果，捕获/恢复边界仍在后续小步
- [x] 5.110 DotRecast 的 Create 返回、Reconstruct 当前 state 和 Capture 快照直接转移不可变 WorldSimulationState，删除三个 lifecycle 边界的 bodies/payload 克隆；保留 roster、revision、localized 和事务校验，Restore 仍走 Reconstruct
- [x] 5.111 KCC 的 Create 返回、Reconstruct 当前 state 和 Capture 快照直接转移不可变 WorldSimulationState，删除三个 lifecycle 边界的 bodies/payload 克隆；保留 body、payload、roster 和 revision 校验，Reconstruct 读取 payload 的中间复制仍在后续小步
- [x] 5.112 CanonicalReader 增加正式 ReadOnlyMemory 入口并要求 array backed segment，KCC state codec 及唯一 Reconstruct 调用直接读取 SolverStatePayload，删除 ToArray 中转；payload 校验、顺序和异常语义不变
- [x] 5.113 DotRecast 接触候选从 List 改为 solver 生命周期 ActorContactCandidate 数组 scratch，批内用 ArraySegment 暴露 active+observed 有效区间；候选排序改固定比较器，按真实数量复用或扩展存储，稳定 roster 下不再新建 List/扩容/排序闭包。contact solver 和碰撞顺序不变
- [x] 5.114 KCC 构造期准备精确 m_KccStates 工作数组，Create 和批求解写回同一数组并只把值序列化进 solver payload，删除每批 states 数组；bodies/results 仍由 state/result 独立持有，Reconstruct codec 返回新数组的分配仍在后续小步
- [x] 5.115 KCC state codec 删除返回数组入口，改为正式原位填充精确 roster 数组；Reconstruct 读入 scratch 并在校验通过后交换当前/scratch 数组，恢复失败不改写当前状态。canonical、identity、数量和 Actor 校验语义保持
- [x] 5.116 Rollback Output Disposition Pass 的正式 disposition List 按模型 MaximumOutputRecords 在构造期准备容量，Runtime factory 从同一 Rollback policy 传入上限；收集顺序、事务写入和 finally 清理不变，稳定负载下删除 List 底层存储扩容
- [x] 5.117 Rollback relayed explicit 输入先扫描当前 predicted roster 判定 gameplay 是否变化，只在变化时分配 replacement Actor 数组；未变化的 provenance promotion 不再创建临时数组，缺失 Actor、冲突判定和 bundle 所有权不变
- [x] 5.118 Rollback Hash Egress 直接把 completed step 或 snapshot history 的 SimulationWorldSnapshot 交给 world hash 构建，删除只为读取 World 新建的 FixedSimulationSessionSnapshot 及其 SnapshotHash 计算；payload 和 report 的正式所有权不变
- [x] 5.119 Rollback simulation projection 复用 Runtime State 生命周期的 CanonicalWriter 和 restore scratch SortedDictionary；identity、数量、重复 Tick、尾部和 confirmed horizon 全部校验后才替换当前 applied hashes，返回 payload 仍由外部快照独立持有
- [x] 5.120 Rollback input history checkpoint 合同收紧为精确数组和 IReadOnlyList 恢复，删除 restore 时的 IEnumerable 接口枚举分配；checkpoint 数组仍每次独立构造并由事务持有，释放和 conflict 校验不变
- [x] 5.121 Rollback History Pass checkpoint 使用私有 ISimulationPipelinePassStateCheckpoint 实现直接持有 Runtime checkpoint，删除 lambda 闭包和通用 delegate 包装；恢复一次、Dispose 后报错和每次 checkpoint 独立所有权不变
- [x] 5.122 Rollback Relay diagnostics 的 explicit frontier 使用 Relay 生命周期精确 roster scratch，Assembler 删除返回数组入口并改为正式填充合同；diagnostics 的 RelayPeerInputFrontier 数组仍由外部读取者独立持有
- [x] 5.123 World state codec 增加正式 solver payload 零复制定位入口，Rollback Hash Egress 直接计算 snapshot 内 solver payload 哈希；删除每条 hash report 的完整 WorldSimulationState 解码、body 数组和 payload 复制，header、profile、body 与尾部校验保留
- [x] 5.124 Rollback Hash Egress 直接把本地 snapshot 编码为 canonical StateHash payload，删除只为编码创建的 RollbackStateHashReport 和 RollbackActorHash 数组；网络报告对象、canonical 校验、字段顺序和接收侧所有权不变，report/snapshot 编码共用同一 core
- [x] 5.125 Rollback Output Committer 的生命周期统计和 Actor/tick 已存在槽位扫描直接枚举 Dictionary KeyValuePair，删除每次读取的 Values／Keys 视图对象；扫描顺序、排序和事务结果不变
- [x] 5.126 Rollback canonical input history 裁剪直接枚举 SortedDictionary KeyValuePair 取最旧 Tick，删除每次淘汰的 Keys 视图对象；保留容量、淘汰顺序和 explicit 计数清理
- [x] 5.127 删除无消费者的 RollbackStateHashHistory 旧报告保留容器，state hash 保留链路统一为 snapshot history 和 canonical payload egress；input/snapshot history 裁剪合同不变
- [x] 5.128 Rollback projection 恢复校验复用 Runtime State 生命周期 CanonicalWriter 直接计算哈希，删除校验用完整 payload 数组；Capture 仍返回快照独立持有 payload
- [x] 5.129 Rollback schedule 缓存正式 replay clock identity，同一 Source Clock 的多次 rollback 不再重复字符串插值；mapping、step source 和 clock 归属不变
- [x] 5.130 Rollback projection 恢复借用 state snapshot 拥有的 payload 视图，删除每次 Apply 的 CopyPayload 数组；Core snapshot 仍构造期独立复制和校验
- [x] 5.131 Rollback output diagnostics code 按三种正式 operation 直接映射固定字符串，删除每次发布诊断的 enum ToString、小写化和 code 插值；detail 与 sink 合同不变
- [x] 5.132 Rollback input history 查询直接输出 predicted/canonical bundle 引用，删除每次读取的只读 entry 包装和异常控制流；capture checkpoint 仍独立持有精确 entry 数组
- [x] 5.133 Rollback schedule 按长度复用 execution plan 的 step-source mapping scratch，删除每个外层 tick 的 1/2 元素数组；mapping 内容按本次计划完整覆盖，可见寿命仍限定 OuterTransaction
- [x] 5.134 Rollback schedule 按 plan step 槽位复用 actor input scratch，删除每次 forward/replay 构造步骤的 roster 数组；FixedSimulationStep 对象和 OuterTransaction 所有权不变
- [x] 5.135 Rollback schedule 按步骤数量复用 execution plan 的 step 数组，删除每个可执行计划的 steps 数组分配；FixedSimulationStep 对象和 OuterTransaction 所有权不变
- [x] 5.136 Fixed Simulation Step 增加正式 owned inputs/actors 构造，Rollback schedule 填充槽位 scratch 后直接转移；删除 Target Step 的 input clone 和 ActorId 收集数组
- [x] 5.137 Fixed Simulation Step 的 owned 构造扩展到 typed ingress，Rollback schedule 按 step 槽位复用精确数组；删除 current step 的 List 复制和排序闭包
- [x] 5.138 Simulation Input 增加正式 source rebind 零复制入口，Rollback schedule 保留 canonical 输入数组；删除每个 replay/forward actor 的 values/requests 复制和排序
- [x] 5.139 Rollback output disposition 使用 pass 生命周期精确 disposition scratch 并转移给正式 set；删除每次 egress 的 set 数组复制
- [x] 5.140 Rollback input/gameplay/bundle hash 共用线程生命周期 CanonicalWriter，bundle gameplay hash 改为版本化 canonical 字段；删除每帧 hash writer、字符串数组、Tick 字符化和逐 Actor 插值
- [x] 5.141 Simulation Input 增加带 input source identity 的零复制重绑入口，Rollback canonical assembler 复用显式输入的已排序 payload 数组；删除每个 canonical Actor 的 values/requests 复制和排序
- [x] 5.142 Rollback input history checkpoint entry 改为只读值记录，checkpoint 精确数组直接承载 predicted/canonical 引用；恢复空值仍显式失败
- [x] 5.143 Rollback runtime transaction checkpoint 改为只读值记录，历史恢复和 checkpoint 字段直接承载捕获值；Owner 归属仍显式校验
- [x] 5.144 Rollback relay canonical confirmation 使用按区间长度保留的 bundle scratch 并同步编码后清空引用；删除每次确认广播的区间数组分配
- [x] 5.145 Rollback relay relayed explicit input 使用按批次长度保留的 frame scratch 并同步编码后清空引用；删除每次转发的 frame 数组分配
- [x] 5.146 Rollback peer input batch 按冗余批长保留 frame scratch，MTU 裁剪后同步编码并清空引用；删除临时 batch List 和公共构造复制
- [x] 5.147 Rollback Datagram Endpoint 按发送队列容量租用并归还待发 endpoint 记录；删除每个数据包的远端 IPEndPoint 克隆，发送 buffer 和 payload 归还边界不变
- [x] 5.148 Rollback protocol envelope 改为只读值记录；接收队列直接承载 Session、Sender、Sequence 和 payload 引用，删除每条完整消息的信封堆对象，payload 所有权不变
- [x] 5.149 Rollback state hash Egress 只读借用 Fixed Source Egress record payload 进行 canonical 解码，删除 Bridge 每次提交的 payload 克隆和无消费者 CopyPayload 旧入口
- [x] 5.150 Float32 远端表现 Egress 只读借用 Source record payload 解码，读取入口改为 ReadOnlyMemory；删除 committed output 每次提交的 payload 克隆
- [x] 5.151 Float32 owner input 与 authority replication Egress 只读借用 Source record payload 解码，迁移最后两个消费者并删除 CopyPayload 旧入口
- [x] 5.152 Float32 gameplay datagram payload codec 只读借用 packet 自有 payload，hello／ack／command／snapshot 全部删除 CopyPayload 中转
- [x] 5.153 Float32 received datagram 改为只读值记录，接收队列直接承载 packet 与来源 endpoint 引用；删除每条收包的信封堆对象，packet 和 endpoint 所有权不变
- [x] 5.154 Float32 Datagram Endpoint 复用接收线程的 ReceiveFrom endpoint scratch；每个轮询周期不再新建 IPEndPoint，入队来源仍独立 clone
- [x] 5.155 Float32 Authority Client Route 复用按命令队列上限准备的过期输入 key scratch；Select 每次只做原地移除，不改既有输入选择顺序
- [x] 5.156 Float32 accepted authority input batch 直接持有按 roster 数量构造并原地排序的最终数组；删除 IEnumerable 复制、List、ReadOnlyCollection 和排序闭包
- [x] 5.157 Float32 authority reliable event 使用单事件 Egress codec 直接编码，删除每条事件的 RemotePresentationBatch、空集合、单元素数组和只读包装
- [x] 5.158 Float32 authority reliable event batch output 直接持有按事件数量构造的最终数组；删除 IEnumerable 复制、List 和 ReadOnlyCollection，路由校验与事件顺序不变
- [x] 5.159 Float32 authority reliable event 与 full checkpoint output 改为 owned payload 合同，删除 codec 产出后的完整数组 clone
- [x] 5.160 Float32 authority gameplay datagram packet 使用 owned payload 构造直接接管 codec 数组，删除发送 packet 的中间 payload clone
- [x] 5.161 Float32 Datagram Endpoint 按发送队列容量租用并归还待发 endpoint 记录；删除每个数据包的远端 IPEndPoint clone
- [x] 5.162 Float32 Datagram Endpoint 复用发送线程有界 writer 和容量内发送 buffer；删除每个数据包的 CanonicalWriter 扩容、完整 wire byte[] 分配和无消费者 RequireFits 旧入口
- [x] 5.163 Float32 gameplay payload codec 显式填充 owner 线程 writer；Authority Source 与客户端 Channel 复用有界 payload writer，删除每包 writer 和内部缓冲分配
- [x] 5.164 Float32 Datagram Endpoint 按接收队列容量租用并归还来源 endpoint 记录；路由校验直接使用 ReceiveFrom scratch，消费边界显式归还
- [x] 5.165 Float32 Datagram Endpoint 池化接收 packet 和最大 MTU payload buffer；坏包、溢出、消费完成和 Dispose 清队显式归还，snapshot payload 仍独立复制
- [x] 5.166 Float32 接收 payload 池化补齐 wire 到租用 buffer 的原位拷贝，修复 5.165 只切换所有权未复制内容的问题
- [x] 5.167 Float32 接收 packet 校验前先绑定租用 payload buffer；坏包失败路径也能成对归还 packet 和 buffer
- [x] 5.168 Float32 Datagram route 保存 canonical identity 与 UTF8 片段，接收读取按 bytes 匹配并复用已知身份；未绑定 Hello 仍分配身份后进入正式 BindRemote
- [x] 5.169 Float32 CommandDatagram 改为 owned sample 数组合同，发送侧从 command history 复制一次，接收侧直接接管解码数组；删除每次命令包的 List 外壳、ReadOnlyCollection 和二次复制
- [x] 5.170 Float32 Authority route 用正式 sequence order queue 维护已发 snapshot 淘汰顺序；确认和满员裁剪直接消费队首，删除每次 ack 的临时 List 和每次裁剪的字典枚举器
- [x] 5.171 Float32 prediction pending request 输出与 correction checkpoint 直接持有精确数组；schedule、ack、baseline、capture 和恢复解码不再经过 Values 视图、List 与 ReadOnlyCollection
- [x] 5.172 Float32 prediction disposition journal checkpoint 直接持有精确 pair 数组；确认遍历和过期裁剪使用 owner scratch，删除 Values 视图与每次裁剪的临时 key List
- [x] 5.173 Float32 prediction history checkpoint 直接持有精确 record pair 数组；记录扫描和 replay 输出不再经过 Values 视图或临时 List
- [x] 5.174 Float32 remote body selection、actor checkpoint 和 timeline checkpoint 改为 owned array 合同；capture、select、restore 解码删除中间 List、Values 视图和只读包装
- [x] 5.175 Float32 Authority tick schedule 直接消费 Character Runtime 的 locked roster descriptor，删除 Pending 和 Executable 计划每次重建 ActorId 数组与 roster descriptor 的分配
- [x] 5.176 Float32 Simulation Step 新增与 Fixed 一致的 owned inputs 入口；Authority tick schedule 使用精确 Actor input/Actor 数组，删除 List 中转和 Step 构造复制
- [x] 5.177 Float32 Authority held input 改为可更新工作对象；同一 Actor 的更新 sequence 直接复用 holder，删除每条新输入的对象替换分配
- [x] 5.178 Float32 Authority replication batch 直接持有精确 owned arrays；Authority 生产端按 actor 数量填充，删除顶层 List 复制和 ReadOnlyCollection 包装
- [x] 5.179 Float32 RemotePresentationBatch 改为 owned array 合同；Authority 过滤结果先计数后填充，接收和合并消费者生成精确数组，删除 IEnumerable 复制与 ReadOnlyCollection 包装
- [x] 5.180 Float32 SelectedRemoteBodyBatch 改为 owned array 合同；prediction schedule 按 current selection 容量一次分配 bodies，删除 List 收集、lambda 排序和只读包装
- [x] 5.181 Float32 AuthoritativeObservationBatch 改为 owned array 合同；Evidence drain 只生成最新 baseline 精确数组，删除 drain baseline List、IEnumerable 复制、lambda 排序和只读包装
- [x] 5.182 Float32 OutputDispositionSet 补齐 owned array 入口；Local Immediate 先计数后填充精确 dispositions，删除 builder List 收集、构造复制、lambda 排序和只读包装
- [x] 5.183 Float32 Authority replication disposition 先按 Actor 输出数量填充精确数组，再直接进入 owned disposition set；删除每 tick 的 List 收集、构造复制和临时清理壳
- [x] 5.184 Float32 Prediction disposition 先校验多 step 容量，再填充精确数组；Add 的 GameplayFact/PresentationCommand 分支改为专用类型入口，删除 List、lambda 和构造复制
- [x] 5.185 Float32 Prediction command history 改为固定 newest-first 数组和按 1–4 容量准备的发送 scratch；命令包直接转移 owned samples，删除 List 插入删除和每次发包复制
- [x] 5.186 Float32 Evidence drain 用常驻 bodies/samples/events scratch 合并多个远端 batch，再生成精确 arrays；删除三个 List、AddRange 扩容和 ToArray 复制
- [x] 5.187 Float32 Prediction pipeline 合并先统计保留 participants，再填充精确数组并追加三个 prediction states；删除合并 List 和扩容
- [x] 5.188 Float32 Remote presentation TickQueue 用常驻 due tick 数组记录已发布 tick，删除 ulong List 和扩容壳；发布完成后再移除和归还 tick 分组
- [x] 5.189 Float32 Remote presentation body stream 用私有 array-backed scratch 生成 intervals，保留 IReadOnlyList 边界；删除 List 收集和扩容壳
- [x] 5.190 两数值域 Ability Evaluate 的 per-Actor ingress 改为常驻数组和显式 count；Evaluation 内部分发改为 array/count 边界，删除 List 外壳和 Clear 临时集合
- [x] 5.191 两数值域 Simulation Committer 的 per-Actor outputs 改为常驻数组和显式 count，用常驻类型化 comparer 排序；删除 List 外壳和 Clear 临时集合
- [x] 5.192 Fixed Local Immediate output 先统计后填充精确 dispositions，并直接进入 owned disposition set；删除 List 收集、Clear 壳和构造复制，补齐 Float32 既有正式链路
- [x] 5.193 两数值域 Pipeline Committer 的 per-step dispositions 改为常驻数组和显式 count；Simulation Committer 边界改为 array/count，删除 List 外壳和 Clear 临时集合
- [x] 5.194 Authority route 的过期 input key 改为按队列容量准备的常驻 ulong 数组和显式 count；删除每次 Select 的 List、Add 和 Clear 临时壳
- [x] 5.195 Authority route 的发送顺序队列改为按 checkpoint 容量加一准备的环形数组；确认、淘汰和未确认保护语义不变
- [x] 5.196 Authority route 的命令队列改为按容量准备的有序 sample/tick 常驻数组；保留同 tick 替换、过期前缀删除和容量溢出语义
- [x] 5.197 Authority route 的已发送 checkpoint 索引并入发送顺序环形数组；查找改为有界 ring 扫描，删除 SortedDictionary 和重复索引
- [x] 5.198 Authority source 的 reliable event 和 full checkpoint 输出改为按 policy 容量构造的共享有界环形存储；保留 overflow、发送顺序和同步 flush 语义
- [x] 5.199 Prediction disposition journal 的 prune scratch 改为按 journal 容量准备的 EventId 数组和显式 count；删除 List、Add 和 Clear 临时壳
- [x] 5.200 Authority source 的 evidence route metrics 改为常驻 string scratch 和显式 count；发布后清空引用，保留路由顺序和诊断文本
- [x] 5.201 两数值域 Character Control Motion 的 per-Actor contributions 由稳定 Actor Binding 持有；每 tick Begin 清空并重绑，scratch 改为数组加显式 count，异常路径统一清理，删除每 tick runtime 新建、List 外壳和 Action 提交委托
- [x] 5.202 两数值域 Ability invocation 的 motion contributions 改为 execution workspace 内数组加显式 count；Accumulator 直接提交，Evaluation 通过显式复制消费并清空使用区间，删除 List 外壳和只读集合边界
- [x] 5.203 两数值域 Character Evaluation 的 motion contribution 聚合改为 Actor Binding 持有的常驻数组加显式 count；Control、Ability 和 Timeline 产出直接进入同一 scratch，Resolver 按精确 count 求值，异常路径统一清理
- [x] 5.204 两数值域 Character Evaluation 的 per-Actor invocations 改为 Actor Binding 按 ability installation 容量持有的数组加显式 count；成功结果构造后清空，异常路径先 Dispose 再清空，删除 List 外壳和只读集合边界
- [x] 5.205 两数值域 Character Evaluation 的 action runtime lookup 改为 Actor Binding 按 ability installation 容量持有的 Dictionary；每 tick 清空重填，成功和异常路径统一清理，删除每 tick Dictionary 新建
- [x] 5.206 两数值域 Character Evaluation 的 shared gameplay effect execution scratch 改为 Actor Binding 持有；每 tick 和成功/异常边界统一 Reset，删除每 tick scratch 新建
- [x] 5.207 两数值域 Ability 的 execution service factory 和 stateless domain runtime factory 改为 Actor Binding 常驻实例；Evaluation 直接复用，删除每 tick 工厂新建
- [x] 5.208 两数值域 Character Evaluation 的 facts、presentation、trace 和 character trace 聚合外壳改为 Actor Binding 持有；每 tick 与成功/异常边界统一清空，删除四个 List 新建，结果数组合同不变
- [x] 5.209 两数值域 Character Trace Sink 改为 Actor Binding 常驻实例；每 tick Begin 重绑诊断身份并重置 sequence，删除每 tick sink 新建
- [x] 5.210 两数值域 Ability Execution Input 改为 Actor Binding 常驻实例；每 tick Begin 重绑 sequence 和 values，成功/异常边界清空引用，删除每 tick wrapper 新建
- [x] 5.211 两数值域 Character Input Runtime 改为 Character Runtime 常驻实例；每 tick Begin 重绑事务请求端口，请求身份在准备期定序定形，删除每 tick wrapper、中间 List 和只读包装
- [x] 5.212 Character Control State Schema 由 immutable Module Contract 准备期生成并共用；两数值域 Control Runtime、初始状态和合同校验消费同一 schema，删除每 tick schema 与字典重建
- [x] 5.213 两数值域 Character Runtime State Transaction 以自身引用作为 Ability 绑定身份，保留同事务校验和拒绝语义，删除每 tick 身份 object
- [x] 5.214 两数值域 Action Trace Context 改为 Frame 内 readonly struct 作用域，外层 execution scope 持有具体类型；保留 push/pop 嵌套语义，删除每次进入 Skill execution 的 trace context class 和接口装箱
- [x] 5.215 两数值域 Character Control Runtime 在准备期持有 State Port；tick 继续传入同一 state transaction 和 schema，删除每次 Control tick 的 port class
- [x] 5.216 两数值域 Ability State Port 和 Operation State Reset 改为 readonly struct；继续按 invocation Frame 构造并传递具体类型，保留 owner、access policy 和 state slot 校验，删除 assembly 准备期的两个 wrapper class
- [x] 5.217 两数值域 Ability Event Sequence 和 Fact Sink 改为 readonly struct；继续由 invocation Frame 构造并直接进入 runtime 与 presentation sink，保留 event sequence、Gameplay channel 和 fact 输出顺序，删除两个 immutable wrapper class
- [x] 5.218 两数值域 Ability Diagnostic Sequence 改为 Trace Sink 内部值存储；字段随 Trace Sink 保存并在 Begin 重置，继续按原始 invocation Frame 生成 Trace header，删除每次 invocation 的 sequence class
- [x] 5.219 通用 Skill Execution Manager 在准备期持有 Scope；同一 manager 仍只允许一个 active frame，Enter 绑定 identity、Dispose 退出并清空，删除每次进入 Skill execution 的 scope class
- [x] 5.220 两数值域 Ability Domain Tick 改用常驻 current actions 和 stopping instances scratch；State Store 提供显式 CopyCurrentActions 并删除旧返回新 List 的入口，保留两次快照、InstanceId 排序和异常清空
- [x] 5.221 两数值域 Action State Store 复用 Trace Execution Scope；进入前仍先创建 manager frame 和 trace context，退出时按原顺序 Dispose 并清空引用，禁止嵌套由 manager 单 active frame 保障
- [x] 5.222 两数值域 Action State Store 按 Skill execution Stack 深度复用 Skill Execution Scope；push 取归还实例并重绑 owner/reference/trace，pop 后归还并清空引用，保留嵌套、unbalanced 和重复 Dispose 保护
- [x] 5.223 两数值域 Equipment Runtime 按 mutation 深度复用 Mutation Scope 和 values scratch；Begin 仍创建 savepoint/output savepoint，Complete/Dispose 后归还并清空 values，保留 savepoint 栈、restore 顺序和重复结束保护
- [x] 5.224 两数值域 State Transaction 按 savepoint 弹出顺序复用 Ability Execution Savepoint；归还前清空 depth、aggregate、allocator 和 event 引用，Dispose 只清空未结束 savepoint，保留栈顶校验、Restore/Release 顺序和异常时不归还
- [x] 5.225 两数值域 Ability Execution Frame 接管 TreeClip 状态并把 Presentation Sink 改为 readonly struct；调用继续按具体 struct 传递，保留 active TreeClip 校验、Begin/End 顺序和 presentation event 通道
- [x] 5.226 两数值域 Gameplay Effect Execution Scratch 常驻 Target 并跨 invocation 重绑；Begin 重建 committed causes、End 清理 working state、prediction 和事务绑定，删除每次 ability invocation 的 Target、Control Runtime 和 Admission Runtime
- [x] 5.227 两数值域 Ability Installation 常驻 Operation Control Runtime 和 TreeClip Link；每次 invocation 只重绑 execution target，保留 transient state 校验、Begin/End evaluation、操作计数上限和 TreeClip invoker push/pop
- [x] 5.228 两数值域 Ability Execution ServiceSet 改为 readonly struct 并删除 services 接口；Operation Control Runtime 直接持有具体 ServiceSet，EndEvaluation 后用 default 清理，保留 Begin/End 顺序、可选 Gameplay Effect/Equipment 服务和 Timeline 只读列表边界
- [x] 5.229 两数值域 Ability Execution Assembly 改为 readonly struct；继续按工厂构造校验和一次性交接 runtime 引用，删除每次 Ability invocation 的临时装配外壳 class 分配
- [x] 5.230 两数值域 Ability Trace Sink 改为 Execution Context 常驻索引；Frame 构造时重绑 invocation 和诊断 sequence，Begin 重置开关与采样计数，End 清空 frame 引用，删除每次 invocation 的 Sink 新建和 Source Map 索引重建
- [x] 5.231 两数值域 Handle Allocator 改为 readonly struct；删除无业务意义的 OperationModule Access 依赖，继续转发 Next/Capture/Restore 到 state port，删除每次 invocation 的 Allocator class 分配
- [x] 5.232 两数值域 Input Runtime 改为 readonly struct 并删除 Input Port 接口；Action 和 Value Runtime 直接持有具体绑定，保留 Blackboard 投影、请求有效期/消费和 tick value 读取顺序
- [x] 5.233 两数值域 Ability Execution Workspace 改为 Actor Binding 常驻；Evaluate 开始 Reset，成功/异常终点统一清空，Float32 value workspace 所有权并入 Actor Binding 并删除 Evaluate 外部数组入口
- [x] 5.234 两数值域 Character Evaluate 的 Timeline logic motion 和 motion warp scratch 改为 Actor Binding 常驻；成功/异常终点统一清空，删除每角色每 tick 的两个 List 外壳
- [x] 5.235 Rollback 与 ServerAuthoritative 的数组/值类型迁移同步调用方：struct envelope 检查 Payload，数组诊断与远端 Body 判空读取改用 Length，预测历史 Body 输出统一为精确数组
- [x] 5.236 ServerAuthoritative Owner Canonical Input Batch 改为 readonly struct；历史记录、Capture、编解码和 Product Slot 传值不再创建 batch 外壳，default 值统一用 IsValid 显式拒绝
- [x] 5.237 ServerAuthoritative AcceptedAuthorityInput 改为 readonly struct；Authority route 选择结果和 accepted batch 元素按值传递，default 输入由 IsValid 与 batch 构造显式拒绝
- [x] 5.238 ServerAuthoritative AcceptedAuthorityInputBatch 改为 readonly struct；accepted input Source 到 Product 与 Authority Schedule 的整批传递不再创建外壳，default batch 用 IsValid 拒绝
- [x] 5.239 同步 GameplayNetwork Datagram Channel 的 Owner Canonical Input Batch 调用方；发送入口改用 IsValid 校验，补齐 Unity 程序集编译
- [x] 5.240 ServerAuthoritative SelectedRemoteBodyBatch 改为 readonly struct；Prediction Schedule 到 Remote Presentation Egress 的 Body 选择结果按值传递，default batch 用 IsValid 拒绝
- [x] 5.241 ServerAuthoritative AuthoritativeInputAck 改为 readonly struct；Authority 复制、观测、Checkpoint 重建和 Evidence 证据按值携带 ack，default 表示无 ack 并用 IsValid 统一拒绝
- [x] 5.242 ServerAuthoritative RemotePresentationBatch 改为 readonly struct；Body、表现命令和可靠事件批按值穿过 Product、History、Checkpoint、Evidence 和表现 Host，default 表示无远程表现并用 IsValid 统一拒绝
- [x] 5.243 ServerAuthoritative AuthoritativeObservationBatch 改为 readonly struct；Prediction Evidence、Observation Source、Product Slot 和 Schedule 按值携带观测批，default 表示无 canonical 观测并用 IsValid 显式拒绝
- [x] 5.244 ServerAuthoritative AuthorityReplicationBatch 改为 readonly struct；Authority egress、Product Slot、canonical codec 和 Authority Source 按值携带复制批，default 用 IsValid 显式拒绝
- [x] 5.245 ServerAuthoritative 可靠事件输出与输出批改为 readonly struct；Authority Source 到控制传输和 Fantasy 连接按值携带，default 输出用 IsValid 显式拒绝
- [x] 5.246 ServerAuthoritative 全量 Checkpoint 输出改为 readonly struct；控制传输到 Fantasy 连接按值携带快照回包，default 输出用 IsValid 显式拒绝
- [x] 5.247 ServerAuthoritative 数据面 Ticket 改为 readonly struct；Fantasy 入队、Source 消费、Client Route 保存和 consumed 回包按值携带，default 用 IsValid 显式拒绝
- [x] 5.248 ServerAuthoritative PredictionCorrectionDecision 改为 readonly struct；Reconciler、Prediction State、Correction Schedule 和 Egress 按值携带每 Actor 决策，default 用 IsValid 显式拒绝
- [x] 5.249 ServerAuthoritative CanonicalInputSample 改为 readonly struct；Datagram 发送历史、packet sample、接收 codec 和 Authority Route 输入队列按值携带，default 用 IsValid 显式拒绝
- [x] 5.250 ServerAuthoritative CommandDatagram 改为 readonly struct；Owner command 发送构造、payload codec 和 Authority Source 接收按值携带，default 用 IsValid 显式拒绝
- [x] 5.251 ServerAuthoritative SnapshotDatagram 改为 readonly struct；快照发送、Prediction 事件队列和 Checkpoint reconstruction 按值携带，default 用 IsValid 显式拒绝
- [x] 5.252 ServerAuthoritative AuthoritativeActorBaseline 改为 readonly struct；Authority 复制、Checkpoint、Prediction Evidence 和 Reconciler 按值携带 baseline，default 用 IsValid 显式拒绝
- [x] 5.253 ServerAuthoritative NetworkCheckpoint 改为 readonly struct；Authority Source、Client Route、快照 codec 和 Checkpoint reconstruction 按值携带 checkpoint，default 用 IsValid 显式拒绝
- [x] 5.254 ServerAuthoritative RemoteBodySelectionFrame 改为 readonly struct；Prediction Schedule 的远端 Body 采样选择按值携带，default 用 IsValid 表达未选择状态
- [x] 5.255 ServerAuthoritative DatagramPacket 改为 readonly struct；发送合同改为 header 加 payload span 直接入队，接收租用 buffer 按值携带并统一归还，删除 packet 外壳池和发送 payload 数组
- [x] 5.256 ServerAuthoritative SnapshotDatagram 使用 owned delta payload 合同；发送侧直接接管 WriteDelta 独立数组，接收侧继续按 wire 长度复制，重建副本按实际长度生成
- [x] 5.257 ServerAuthoritative NetworkCheckpoint 发送编码改读 owned StateSpan；WriteFull 与 delta 状态写入/比较删除发送前数组克隆，layout 校验的 byte[] 合同保持
- [x] 5.258 Float32 Character Runtime State Codec 读取合同改为 ReadOnlyMemory；NetworkCheckpoint layout 校验直接读 owned state memory，删除校验前数组克隆
- [x] 5.259 ServerAuthoritative ActorBaseline 使用 owned state bytes；Checkpoint Capture 共享内嵌 baseline 状态，unchanged delta 不再克隆 acknowledged 状态
- [x] 5.260 ServerAuthoritative Checkpoint decode 与重建 baseline 共享 immutable 状态；删除构造期克隆和无消费者 StateBytes 入口
- [x] 5.261 ServerAuthoritative Authority baseline 校验直接读取 owned CharacterStateBytes；删除临时 SimulationActorSnapshot 和状态数组克隆
- [x] 5.262 Float32 SimulationActorSnapshot 使用 owned state bytes；Authority restore merge 直接引用 immutable baseline StateBuffer 并删除旧复制入口
- [x] 5.263 Float32 World Snapshot codec 复用线程生命周期 hash 与 canonical writer；Read 校验不再生成完整临时编码数组，wire 内容和失败语义不变
- [x] 5.264 两数值域 World Snapshot 嵌套编码改为长度前缀流式写入；Fixed 同步复用校验 writer，Session Snapshot 与 Prediction History 删除内层数组中转
- [x] 5.265 两数值域与 ServerAuthoritative Pipeline 状态编码直读 immutable Payload；Session Snapshot 与 Prediction History 保存不再克隆 participant 状态
- [x] 5.266 Pipeline restore 前的 participant payload hash 校验直读 immutable Payload；删除只供 hash 使用的完整数组克隆
- [x] 5.267 Pipeline snapshot hash 改用线程生命周期 CanonicalWriter；删除 string 数组、插值字符串和 UTF-8 中转数组
- [x] 5.268 Pipeline participant payload 使用 owned bytes 合同；全部捕获和 decode 生产者直接移交新建数组，构造器不再克隆
- [x] 5.269 Pipeline participant hash 入口拆分 owned capture 与 wire decode；生产者删除重复 payload SHA-256 计算，decode 保留 expected hash 校验

## 6. UI、资源、渲染和生命周期

- [x] 6.1 将状态History等热点getter改为正确寿命的视图，迁移UI刷新消费者，清理重复格式化与每次列表副本
- [ ] 6.2 沿运行特效/音频/角色/资源实例的正式准备与租用入口治理Active期间分配，保留加载取消和退出释放
- [x] 6.3 沿正式RendererFeature/RenderPass及材质消费治理实际managed分配，分别记录native/GPU资源释放，不重写正确的复用实现
- [ ] 6.4 清理项目Editor/启动/离线工具中的已确认重复订阅和生命周期遗留，保持重操作显式触发并与运行0 GC结果分组
- [x] 6.5 资源快照发布复用同一次对象池查询结果，标签与作用域直接填充最终独立数组，删除中间 List 和复制；保留历史快照寿命、排序及维护前后重新采集，不表示资源链或 History 已无分配
- [x] 6.6 资源池统计统一调用正式填充 List 接口，运行时持有工作列表并在同步统计后清空引用，迁移发布和维护前后全部调用；初始容量来自当前池数量，新增池引起的容量增长及池内部统计分配仍未完成
- [x] 6.7 资源、启动状态、检查点和故障事件四类历史队列按正式历史上限 N＋1 在构造时准备，覆盖先加入后淘汰峰值；保留历史结果副本、事件顺序与 N 条保留规则，记录对象分配仍未完成
- [x] 6.8 Product startup／checkpoint／fault／resource History 使用 owner 长寿命 bounded 只读视图，删除四次 getter ToArray；发布和超窗淘汰顺序保持不变
- [x] 6.9 资源租约与实例的内部所有权记录按 runtime 生命周期池化，acquire/instantiate 租用，release 清空身份并归还；公共 lease 对象、加载等待、链接取消源和外部 payload 仍保持独立
- [x] 6.10 删除共享物理资源加载的 InFlightLoad 包装，Dictionary 直接持有 UniTaskCompletionSource；首载、并发 join、异常传播和移除时机不变
- [x] 6.11 资源 scope 持有私有 dispose id buffer，closing 后按当前 lease/instance 数量扩容并跨两次复制复用；先复制后释放的顺序和 closing 阻止新注册保持不变
- [x] 6.12 删除无消费者的 Main startup History 合同、队列和容量常量，跨线程源只保留 Current 与 SnapshotChanged，避免加锁枚举活历史或每次读取生成快照数组
- [x] 6.13 资源维护持有无主物理身份 scratch，RemoveUnownedPhysicalKnowledge 改为显式收集后移除，删除 lambda 闭包和委托；收集列表在维护结束后清空引用
- [x] 6.14 BlockImpact VFX 与 ScreenSpaceDot 控制器在实例构造期创建 MaterialPropertyBlock，删除首次触发的懒加载分配和空检查；RendererFeature/RenderPass 既有 pass、material、CommandBuffer、workspace、GPU buffer 与 RTHandle 复用释放链保持不变
- [x] 6.15 删除无调用方的 ResourceInstanceLease、InstantiateAsync 入口、scope instance 注册表和 runtime instance 记录池；资源快照与 Product Shell 不再暴露恒为零的 instance 诊断
- [x] 6.16 删除无消费者的 ResourceLease 外壳和 PreloadPlanResult，AcquireAsync 改为 UniTask 并由 scope 唯一持有资产租约；barrier 保持并发等待和失败传播
- [x] 6.17 资源快照 scope 排序器在类型准备期固定，删除每次 PublishSnapshot 的捕获 lambda 和比较委托；scope 与 tags 快照数组继续保持独立寿命

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

- [x] 7.16 脚本化与实时捕获的 Presentation Schedule 帧按连续双值 ClockMode 直接校验，删除逐帧 Enum.IsDefined 装箱；帧时序、范围和投递契约保持

- [x] 7.17 Action Presentation 时间快照按可发布生命周期与投影类型的正式连续区间校验，删除每次快照构造 IsValid 中的两次 Enum.IsDefined 装箱

- [x] 7.18 Camera Projection 九类 payload 按 CameraSpace／TimeDomain／Stacking／FovVariation／SequenceStage 五个正式连续 byte 区间直接校验，删除 RequireValid 中十四处 Enum.IsDefined 装箱

- [x] 7.19 Rollback Endpoint diagnostics 按已锁定 Actor 字典准确创建远端快照数组并原地排序，删除 CaptureDiagnostics 的中间 List 与 ToArray 复制；结果仍保持独立数组

- [x] 7.20 FBBIK Profile 在 authoring/content preparation 统一完成 schema、枚举和值域校验；运行 Solver、Pose Buffer Backend 不再重复校验 Rig/Profile，提交已准备 tuning 不再二次校验；保留 tuning 输入、目标血缘、帧缓冲和求解结果校验，删除 FBBIK 枚举装箱

- [x] 7.21 Pose Graph 在 Prepare 已完成完整拓扑、端口和边界校验后，实例初始化直接消费 PreparedBinding，删除 InitializeGraph 的第二次整图校验；保留端口表构造、handler 绑定和运行帧事务校验

- [x] 7.22 Pose handler 与 InstanceContext 直接消费已由正式入口校验的 Rig，删除构造阶段重复 Rig schema 校验；保留 binding 身份、节点配置、资源形状和运行帧事务校验

- [x] 7.23 Pose Animation Slot、handler registry/evaluator、Modify Bone 已由 authoring/content preparation 定型的枚举不再在正式运行链调用 Enum.IsDefined；保留来源一致性、节点身份、重复注册和骨骼索引校验

- [x] 7.24 Pose Domain ServiceFactory 只在创建阶段准备一次 SourceCatalog；Clip Player 与 Foot Motion 通过已建索引按 SourceIndex 读取，删除重复字典、数组、资源注册和 LINQ 查找分配

- [x] 7.25 Blend Stack 在准备 payload 时完成 policy、curve、profile、transition 的静态校验；运行实例删除已准备内容的 Rig、catalog entry 和 transition 重复校验，保留 final buffer layout 与 owner/provider 绑定

- [x] 7.26 Motion Matching DatabasePayload 已在内容准备阶段校验全部 Clip binding 后，选样解析不再重复调用 binding.RequireValid；保留 sample/index、时间、Loop、Foot 参数和输出有效性校验

- [x] 7.27 Timeline NumericTarget 只在 PrepareRequest 边界校验，PlaybackRequestFactory 不再对同一 composition 配置重复 Enum.IsDefined；保留动态 PlaybackMode、generation、handle 和 capture/restore 事务校验

- [x] 7.28 Pose Source/Foot/Managed resource catalog 直接消费外层已校验 Rig，删除三处重复 Rig schema 遍历；保留资源 identity、plan/descriptor、calibration、bone index 和 Native shape 校验

- [x] 7.29 删除 SourceCatalog 未使用的 Plans/Resources ToArray 接口，统一只通过已建索引 RequirePlan/RequireDescriptor 读取，消除死 API 和临时数组分配

- [x] 7.30 AnimationBlendSourcePoseWorkspace 复用外层已校验 Rig，删除四类 Pose player 共用构造器中的重复 Rig schema 遍历；保留 null、bone/parameter/source capacity 和 Native buffer shape 校验
- [x] 7.31 删除无调用者的旧 CharacterAnimationBlendStackKernel 及其 Unity meta，统一只保留 AnimationBlendStackRuntime 正式 Blend Stack 链；不改变现行实例、source workspace、frame plan 和提交事务
- [x] 7.32 Blend Stack source binding 按已准备的 EntryCapacity 预分配 Pending、Request 和 source identity 工作集合，删除首个运行帧的扩容分配；保留 source 去重、请求顺序和 ResetFrame 清理语义
- [x] 7.33 Pose Action command source 按 Inbox 固定容量复用命令工作区，删除每帧命令数组创建；保留读租约、命令顺序、frame identity 和 Commit/Discard 语义
- [x] 7.34 ACL Pose sampling backend 复用 Presentation 工厂已校验的 Rig，删除 backend 构造阶段重复 Rig schema 遍历；保留 Rig binding、Animator、PlayableGraph 和 Native capacity 校验
- [x] 7.35 删除全项目无调用者的旧 CharacterMotionMatchingProviderRuntime 及其 Unity meta，统一保留 Pose Graph 的 MotionMatchingPoseSourceRuntime 正式链；不改变当前数据库、选样和 source completion 语义
- [x] 7.36 Motion Matching Pose handler 复用稳态输出包装对象，删除每帧 `CharacterPoseNativeLocalPoseValue` 创建；保留当前 lineage、Native layout、完成标记和提交页索引语义
- [x] 7.37 Modify Bone 复用稳态 Component Pose 输出包装对象，删除每帧 `CharacterPoseNativeComponentPoseValue` 创建；保留当前 lineage、Component layout、完成标记和提交页索引语义
- [x] 7.38 Space Conversion 按固定输出空间复用 Local/Component Pose 包装对象，删除每帧输出包装创建；保留空间转换、Native 双页和提交事务语义
- [x] 7.39 Blend、Additive、Layered Bone Blend 复用 Local Pose 输出包装对象，删除三个纯 Pose 组合节点的稳态逐帧包装创建；保留连续性、Native 双页和提交状态语义
- [x] 7.40 Entry Pose 与 Linked Pose 复用 Local Pose 输出包装对象，删除 source 复制节点的稳态逐帧包装创建；保留 source completion、Native 双页和提交状态语义
- [x] 7.41 Clip Player 与 Blend Space Player 复用 Local Pose 和 discontinuity 输出包装对象，删除两类播放器稳态逐帧包装创建；保留当前 frame identity、Native 双页和提交事务语义
- [x] 7.42 Blend Stack handler 复用 Local Pose 输出包装对象，删除正式 Blend Stack 稳态逐帧包装创建；保留 stack 完成、source reset、待提交校验和页索引提交语义
- [x] 7.43 Parameter Resolve、State Machine、Root Orientation Warp 复用 Local Pose 输出包装对象，删除三个 Pose 节点的稳态逐帧包装创建；保留参数、状态和 warp 提交语义
- [x] 7.44 Inertialization 与 History Collector 复用 Local Pose/History 输出包装对象，删除两个节点的稳态逐帧包装创建；保留 inertialization 状态交换、history source commit 和 Native 双页语义
- [x] 7.45 Selected Pose Player 复用 Local Pose 与 discontinuity 输出包装对象，删除 Motion Matching 选择播放器稳态逐帧包装创建；保留 sample、Playable job、source reset 和提交页索引语义
- [x] 7.46 Animation Slot handler 与 source 复用 Local Pose 输出包装对象，删除 slot 三种输出分支及 source Evaluate 的稳态逐帧包装创建；保留 slot 连续性、source completion 和提交事务语义
- [x] 7.47 Foot Placement、Pose Bone IK、Goal Assembler、Full Body IK 复用约束输出包装对象，删除四类约束节点稳态逐帧 wrapper 创建；保留约束结果、Native 双页和提交事务语义
- [x] 7.48 State Machine source 复用 Local Pose 输出包装对象，删除子状态图合成结果的稳态逐帧包装创建；保留子图事务、状态迁移和 source completion 语义
- [x] 7.49 Graph Evaluator 的 Program Parameter 与 Action Playback 输入复用值对象，删除正式 Pose 输出读取中的逐次托管对象创建；保留输入值、channel 命令和 frame identity 语义
- [x] 7.50 Clip Player 删除已由 SourceCatalog 准备边界完成的 source schema/foot feature 重复校验，Blend Space Player 删除已准备 plan 的重复完整校验；保留 descriptor identity、参数绑定和运行时 solver/page 事实
- [x] 7.51 Presentation Runtime 装配入口按 LocalOwner 和 SimulatedActor 直接校验角色表现角色，删除 CharacterPresentationRole 的 Enum.IsDefined 反射装箱；保留原装配边界和参数错误语义
- [x] 7.52 Session Host 活跃逻辑 tick 只保留 LaunchPlan 生命周期检查，OuterTickKind 由 SimulationSessionPreparedRuntime 准备边界校验一次；删除运行期重复 Enum.IsDefined
- [x] 7.53 Final Pose Physical Writer 复用 Presentation 工厂已验证的 RootHierarchy 与 RigBinding，删除构造器第二次整遍绑定 schema 校验；保留 PoseRoot 归属、引用姿态和运行帧事务检查
- [x] 7.54 Fixed 和 Float32 Ability Execution Data Codec 按 SemanticValueKind 连续正式值域直接校验 payload；ProgramConstantInputBinding 删除已验证输入的重复 Enum.IsDefined
- [x] 7.55 Fixed 和 Float32 Ability Execution Data Codec 的 Numeric Profile、Source Map 和 Constant byte 枚举改为正式成员显式匹配；保留 payload 边界和 InvalidDataException，删除 Enum.ToObject/IsDefined 装箱
- [x] 7.56 Fixed 和 Float32 Ability Execution Data Codec 的 SimulationOperationCode int payload 按 GameplayAbilityOperationSet 显式成员校验；保留数值范围和 InvalidDataException，删除 ReadEnum 反射读取
- [x] 7.57 Semantic IR 和两数值域执行数据的 SimulationOperationCode 解码统一走 GameplayAbilitySemanticsCodec 的 typed 入口；保留当前 OperationSet 成员、ushort 范围和 Semantic IR 版本校验
- [x] 7.58 OperationExecutionDescriptor 构造按 GameplayAbilityOperationSet 校验 OperationCode，删除 Enum.IsDefined 反射；保留 ArgumentOutOfRangeException，错误文案改为当前 OperationSet 不支持
- [x] 7.59 Semantic IR Codec 的 literal、document 和 constant input byte 枚举改为连续正式值域直接校验；保留 payload 边界和原异常类型，删除该 codec 全部 Enum 反射
- [x] 7.60 RuntimeCaptureStore 的全局 capture change 索引改为 maxEvents+1 环形数组，淘汰旧 segment 时前进 head 并清空槽位；发布顺序、segment 边界、丢弃计数、全量同步和读取独立结果保持不变
- [x] 7.61 RuntimeCaptureStore 按 maxSegments 准备私有 segment 归还池，裁剪后的段和事件列表在同一 capture 内重置复用；Domain/Position 分组、满段丢弃和 Freeze 独立快照不变
- [x] 7.62 RuntimeCaptureSnapshot 持有不可变 segment 数组和一次组装的事件数组，GetEvents 返回 offset 后缀 span；三个 Editor 消费者迁移到 Length 和下标读取，删除每次读取的 List 重建
- [x] 7.63 RuntimeLiveStateStore 按 maxChanges 准备私有 recency node 归还池，Clear 后重建优先复用节点并清空旧 key；现有 LRU 顺序、满员替换和状态语义不变
- [x] 7.64 图状态读取改为调用方 Copy 工作列表，删除 GetGraphStates 返回 List、Tree overlay 的 ToList/OfType LINQ 和 authoring trace 的 LINQ 中转；过滤身份与排序器长期持有，最终 projection 数组仍独立返回，Editor 读取路径不计为 Player 每帧收益
- [x] 7.65 图节点执行状态读取复用 ViewModel latest 字典和调用方结果 List，删除 GetGraphExecutionStates 每次新建 Dictionary 与返回 List；每次读取先清空 scratch，同 Source 仍取 Position 和 Sequence 最新，invalid instance 返回空列表语义不变
- [x] 7.66 执行时间线与历史构建直接接管 SelectEvents 的独立事件 List，删除 BuildCore 的 ordered 复制和 BuildHistory 的 historyEvents 复制；事件排序、边界补充、分组来源和 complete 判定保持不变，Builder 其余临时集合仍未完成
- [x] 7.67 RuntimeCaptureStore 的 active segment 外层改为 maxSegments+1 环形缓冲，删除淘汰 RemoveAt(0) 前移；归还原池同步扩到 maxSegments+1，覆盖 append 后 trim 的满容量换段峰值，发布顺序、segment 边界、丢弃计数和 Freeze 快照不变
- [x] 7.68 RuntimeLiveStateStore 的 current、changes 和 recency node 映射按 maxChanges+1 在构造期准备容量；覆盖 active 上限和先入队后出队的瞬时峰值，LRU、淘汰计数、全量同步和读取结果不变
- [x] 7.69 执行 history 的 tick/frame 事件 List 和 ticks/checkpoints/frames 结果 List 改为 internal 构造直接接管，删除 AsReadOnly 包装和旧 null Array fallback；公开只读接口、排序、checkpoint 去重和 presentation 可用性判断不变
- [x] 7.70 RuntimeExecutionTickRecord 的外部结果和 13 类身份摘要改用 Collector 产出最终数组，删除每次 tick 的 List 对象和 AsReadOnly 包装；外部结果重复保留，身份去重顺序和 EqualityComparer 比较合同不变，payload 字符串仍在后续边界
- [x] 7.71 图实例读取改为调用方持有结果 List，ViewModel 复用 sequence scratch 和固定排序器，删除 GetInstances、GetGraphInstances 与 CollectInstances 的每次 List、Dictionary、闭包和委托；技能执行筛选、图 ID 匹配、最高 sequence 去重和降序排序不变，Timeline 实例读取仍在后续边界
- [x] 7.72 执行 history 分组改用按 Position/Branch/Sequence 预排序的连续 EventGroup 列表，删除 tick 与 presentation 两个 SortedDictionary 和每个 key 的排序树节点；group 内仍按 Position/Sequence 排序，builder 的事件与 span 比较委托改为静态缓存，输出组顺序、checkpoint 去重和 record 归属不变，spans/history 输出集合在 7.81 数组化
- [x] 7.73 Tree 节点诊断状态改传 State／NodeStopStatus 枚举，入口用固定常量映射原文本并删除旧 string 状态入口；RunnableNode 全部调用方不再在采样判断前 ToString，未知枚举显式抛错，节点停止 Cause、边 Detail、图状态文本和 OwnerId 字符串仍在后续边界
- [x] 7.74 Tree 节点停止与状态退出 Cause 改传 NodeStopOriginCause／StateExitCause 枚举，诊断发布边界用固定常量映射原文本并删除诊断链内全部 Cause ToString；状态机内部退出和外部树停止保留两条正式 cause 入口，未知枚举显式抛错，边 Detail、图状态文本和 OwnerId 字符串仍在后续边界
- [x] 7.75 Tree graph 生命周期状态改在发布边界用固定文本映射 GraphCreated／GraphDestroyed，删除 PublishGraph 的 kind ToString；其他 RuntimeTraceEventKind 进入 graph 生命周期入口显式抛错，节点停止 Cause 已由 7.74 处理，边 Detail 和 OwnerId 字符串仍在后续边界
- [x] 7.76 执行 selection 的结果 List、sequence 去重、related graph 和 presentation frame 集合改为 builder 静态工作集合，调用前清空并保留容量；无过滤路径不再新建完整事件 List，两遍扫描、branch 收敛、去重和输出顺序不变。该 scratch 只用于 Editor 同步读取链，BuildCore 的 spans 与 history 返回结果集合在 7.81 数组化
- [x] 7.77 BuildCore 的 PendingSpan 从 class 改为值类型，四个 Last 更新点显式写回 open 字典；open Dictionary 复用 builder 静态外壳并调用前清空，保留既有桶容量。span 配对键、配对结果和未完成 span 语义不变；结果 spans 在 7.81 数组化
- [x] 7.78 history checkpoint 去重 HashSet 复用 builder 静态外壳并调用前清空，CheckpointKey 连续 struct 比较和 first-seen 去重不变；checkpoint、ticks、presentation frames 和 spans 结果集合会随返回值被外部消费，在 7.81 数组化
- [x] 7.79 history 会话边界补充的 sequence 与 branch HashSet 改为 builder 静态工作集合，调用前清空；基线 checkpoint 查找、边界范围、去重和插入顺序不变。该阶段仍同步复用 selection List，未做跨线程或重入假设
- [x] 7.80 history 分组删除每个 tick／presentation frame 的 List：EventGroup 先按排序后的连续 key 统计数量，再分配精确 RuntimeTraceEvent 数组并在第二遍填充；TickRecord 和 PresentationFrame 直接持有最终数组，全局 Position/Branch/Sequence 排序覆盖原 group 内排序，外层分组 scratch 调用后清空。返回结果集合在 7.81 数组化
- [x] 7.81 execution timeline 与 history 的 spans、ticks、checkpoints、presentation frames 改用 builder 静态结果 List 填充排序，再转换为精确数组交给返回对象；返回接口、排序、未完成 span、checkpoint 去重和 presentation 可用性不变。数组是正式最终结果，scratch 只服务 Editor 同步读取链
- [x] 7.82 RuntimeDebugChangeSet 直接按 pending HashSet 数量生成 source／instance 精确数组快照，删除中间 List 复制壳；FullSync、空集、遍历顺序、Affects 查找和 ViewModel pending 集合生命周期不变。数组是本次变更集的正式结果，生产者 HashSet 不外借
- [x] 7.83 RuntimeDebugSession.Targets 按 registry 当前 Count 生成 RuntimeDebugTargetInfo 精确数组，空集复用 Array.Empty，删除中间 List 壳；registry 顺序、目标信息构造和公开只读列表接口不变。数组是本次读取快照，registry List 不外借
- [x] 7.84 RuntimeDebugSession.GetTargetCandidates 按 registry 当前 Count 生成 RuntimeDebugTargetCandidate 精确数组，空集复用 Array.Empty，删除中间 List 壳；registry 顺序、MatchTarget 判定和公开只读列表接口不变。数组是本次候选快照，registry List 不外借
- [x] 7.85 RuntimeDebugSourceMapSnapshot 的 graph invocations 按源 map Count 生成精确数组，空集复用 Array.Empty，删除 Capture 的 List 复制和 ReadOnlyCollection 包装；path 字典、TryGetInvocation、公开只读列表和 source map 冻结时序不变。entries 与 hash 冻结分配仍在后续小步
- [x] 7.86 RuntimeDebugSourceMapSnapshot 的 source hash 冻结改为先统计每个 source 数量再填充精确数组，删除每个 source 的中间 List 与扩容数组；hash 顺序、最终字典键顺序、Match 查找和 source map 冻结时序不变。hash 字符串和 entries 字典仍由 source map 快照独立持有
- [x] 7.87 Timeline 实例读取改为调用方 Copy 工作列表并复用 ViewBinding scratch，删除 GetTimelineInstances 的每次 List、排序闭包和旧返回入口；Timeline/Graph 筛选、最高或最新 sequence 降序、Follow/Pinned 判断不变
- [x] 7.88 RuntimeElementDebugState.Status 的空 payload fallback 改用类型准备期 RuntimeTraceEventKind 名字表，删除 Tree overlay 与技能 observation 每次读取的 Kind.ToString；Payload.Status 原文和正式枚举名不变
