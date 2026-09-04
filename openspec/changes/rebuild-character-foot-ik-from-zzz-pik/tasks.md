## 1. 证据基准与文档更新

- [x] 1.1 固定design的E1–E6正式来源、真实名称与同构建版本；核对82方法／11978指令和137＋22＋7字段的已有核验记录，文档保留来源与覆盖边界
- [x] 1.2 重新核对46份CSV与磁盘GameAssembly的SHA256，并读取接触阈值倍率常量；验证46/46文件匹配、映像匹配且RVA02816B84为Float32的3
- [x] 1.3 修正主入口、普通／预测混合、落点／法线真名、进入区域阈值和OnFootPlant先于remainTime写入的口径；验证proposal、design和delta使用相同规则
- [x] 1.4 展开82方法逐项覆盖表，登记算法、宿主适配、只读诊断与非业务运行壳；验证每项有原签名／RVA、输入输出、状态写者、项目Owner与未完成分支
- [ ] 1.5 按design第8节逐项检索已有raw、资源、元数据和指令，登记区段、参数覆盖、LockFoot数组与FBIKSetting缺项；验证每项有精确资源／状态／字段、已搜索来源和恢复结果，不能以总体采样行数标完成

旧任务1.1–1.4的“基线与配置已完成”不继承为本完整复刻的完成状态：9b6b6aaec是旧基线恢复，3d75913b3只登记部分脚锁配置，9cfe69505只新增状态槽位。已有代码和历史证据保留到对应单一责任被正式替换，不把这些提交当作整条新算法已实现。

## 2. 现行规格、输入基线与实施边界

- [ ] 2.1 按design第9节逐项处理旧stabilize change和project的冲突口径，保留不受影响的已完成工作；验证旧世界残差、旧骨盆硬Reach、第二Pose混合及过期CaptureViewLease不再成为并行实施要求
- [ ] 2.2 对照当前Foot Analysis、Pose Graph与Agent authoring合同确定原区段的唯一正式写入和编译入口；验证不创建独立配置旁路，必要的跨能力delta先通过OpenSpec补齐后再实施相关接口
- [ ] 2.3 固定实施时精确代码提交／工作区文件哈希、Corin Profile／Calibration／Projection／Float32与Fixed Program、World及Presentation Schedule；用现有正式Replay保存同一基线manifest，验证已回退f2dbbf8a9不作为当前候选
- [ ] 2.4 将43357ff3cd384e5cba75d2c31175b116输入与对应有效初始状态、世界和表现时钟绑定；验证输入SHA与design一致，历史proof仅作定位且新baseline具有当前身份
- [ ] 2.5 固定原AMLegIK目标、正式Goal编码、Solver与Physical四层对账字段和数值容差；验证沿用现有诊断规则、没有新评分器或第二运行求解器

## 3. 原作者数据、配置和绑定

- [ ] 3.1 按E4恢复Corin实际使用状态的原区段与归属，保留StartFrame、EndFrame、IsSliding和GroundPositionL；验证零长度段、表顺序、角色／状态身份与原记录一致，Walk／Run缺页不得用旧曲线补齐
- [ ] 3.2 恢复NextAnimStateNameHash、NextAnimTransitionOffset、IsPredictedLoop、停止帧、运行EnableKneeSmooth及区域调度配置；验证作者／运行表区别和未复制KneeSmooth的原构建行为有明确处理
- [ ] 3.3 将完整原默认参数、当前有效七项状态参数、静态查询参数及一次性命令装入唯一Profile／Tuning入口；验证每个被消费字段有原始字节来源、单位、范围和Revision
- [ ] 3.4 用可琳Rig779516与当前Rig校准核对Owner、Animator根、Rigidbody、Foot／Toe／Pelvis、绑定旋转及坐标变换；验证这些输入分开绑定且不按名称或PoseRoot默认补齐
- [ ] 3.5 恢复现有资源／raw中的FBIKSetting379248、控制参数布局与必要原生配置；验证547字节缺页被真正恢复或仍明确为未完成，不能以项目默认配置填空
- [ ] 3.6 对原版完整／低质量查询、两种骨盆模式、预测、步幅、脚锁和平台条件配置逐项登记；验证完整实现分支与Corin当前启用值分开，禁止擅自打开基准关闭项

## 4. 区域、时间与模式输入

- [ ] 4.1 在现有Source／PoseState输入生产阶段实现原区域筛选、进入／更新／退出、循环、IsSeam与有序队列语义；验证每条单脚命令有状态、区域、时间与调用顺序来源
- [ ] 4.2 实现当前区段闭区间与FindNextSegmentIndex的双端点wrap01搜索；验证原枚举次序、相等距离、单帧区段、未归一化输入t和循环分支与指令一致
- [ ] 4.3 实现remainTime的当前状态、下一状态偏移和停止帧公式，按OnFootPlant后写入；验证60帧换算常量、旧／新remainTime和GroundPositionL直接写入位置
- [ ] 4.4 实现原kneeState生产及其输入分型；验证enablePIK、KneeSmooth标签和运行配置分别参与，未证明消费前不连接ForceEnableKneeSmooth或额外骨骼约束
- [ ] 4.5 发布当前／下一状态PIK标记、原过渡量与平台条件，复刻IsInPIKState和PreprocessPredictionIK；验证四类状态过渡权重、bool返回、needPIK与逐脚enablePIK各自一致
- [ ] 4.6 将事件和参数输入绑定到同一根Bank与正式表现时钟；验证同帧多事件的顺序、失败帧丢弃及不重复执行整条Foot链

## 5. 实例运动、单脚状态与生命周期

- [ ] 5.1 复刻Initialize、InitializeFootLockState、Start／OnEnable、状态参数Set／Get／Reset及场景参数初始化；验证原初值、清理范围、大小写默认／当前字段分离
- [ ] 5.2 复刻ApplyPlayerMotion与Update的Owner运动、lastHeight、isRaising、isMoving、速度、步幅输入和deltaTime；验证不使用旧Prediction EMA或KCC期望速度替代
- [ ] 5.3 用完整PredictState替换现有只记事实的接触槽位，按原单脚转移表应用OnFootPlant；验证未写事件保持、time重置、旧remainTime比较和另一脚不变
- [ ] 5.4 实现Finalize对time、lastFrameState、原命中／动画／目标缓存及六项事件／请求标志的更新；验证两脚Finalize均先于骨盆，事件不会稳定重复出现
- [ ] 5.5 将全部持久状态和原重置原因接入现有Prepare／Seal／Discard；验证无独立提交、无失败帧状态泄漏且正常热路径无每帧托管分配

## 6. 普通地面几何与查询

- [ ] 6.1 从原GetRaycastHit、GetDeltaFromRigidbodyToTransform及下游调用绑定展开完整请求／结果合同；验证形状、方向、距离、过滤、所有候选及坐标差量均有操作数来源
- [ ] 6.2 完整翻译HitGroundSimpleImpl的几何、候选组合和出口；验证原方法全部普通计算分支有对应实现，不使用旧双SphereCast公式
- [ ] 6.3 完整翻译HitGroundImpl的脚／Toe多点布局、延伸、宽度、位置／法线组合及退化出口；验证全部1237条原指令的相关计算归属有覆盖记录
- [ ] 6.4 复刻IsFootMoving、OrdinaryIkHitGround与真实查询高度选择，接入唯一Query adapter；验证实际查询、缓存复用、无命中和非法输入分别可追踪
- [ ] 6.5 删除普通地面目标对旧最近Surface、5厘米／1度查询门、Capsule Ground Path和Convex Hull的依赖；验证剩余公共世界服务只有实际消费者，没有Foot旧算法旁路

## 7. 预测记录、步幅和LockFoot

- [ ] 7.1 完整翻译CalculatePredictFootTarget和PredictFoot，使用原区域脚点、remainTime、Owner运动、步幅与平台输入；验证目标、记录与enablePIK的写入顺序及无旧KCC未来轨迹替代
- [ ] 7.2 复刻CrossCheck、get_EnableLockFootReal、LockFoot和PreprocessAnimPos；验证FootLockInfo七字段、开启门、XZ结果及Foot／Toe后继输入与原函数一致
- [ ] 7.3 复刻PredictIkHitGround的非isMoving接触分支、XZ距离、enterGroundedZone乘3阈值和重写条件；验证current／next位置、法线、两高度及平台标志的写入顺序
- [ ] 7.4 复刻未重写时进入事件的current承接旧next与随后next=current；验证不使用leaveGroundedZone替代、不额外创建世界残差或纪元状态机
- [ ] 7.5 完整翻译PredictIkHitGround剩余高度历史、distFraction、组件空间和预测启停分支；验证两份高度按原PIKSmoothSpeed与dt分别推进，关闭预测时仍执行原记录更新
- [ ] 7.6 将普通／预测支撑按pIkWeight组合到唯一后继目标输入；验证位置／法线混合的操作数、归一化位置以及PIK→PIK区域交接，禁止第二次动画Pose混合

## 8. 原脚目标、方向与输出语义

- [ ] 8.1 完整翻译DoCalculateTarget前段坐标变换、FootNormalForwardAngleThreshold及后段FootRotVelocityLimit；验证两个角约束的含义和每次／每秒单位不混用
- [ ] 8.2 复刻原高度区间、带符号标量、实例isRaising选速率和enablePIK／disableDamping旁路；验证Owner局部坐标输出及所有参数消费与原操作数一致
- [ ] 8.3 复刻CalculateFootTarget的位置／quaternion基准混合和历史回归；验证k=clamp01((1-pelvisIkWeight)×IKWeight)只在原层应用一次
- [ ] 8.4 复刻SetFootControlParam的目标、FootDisableIkHeight分支及InScale，核对原生实际消费者；验证附加标量不能未经证明直接映射为FinalIK权重
- [ ] 8.5 将结果通过显式Rig转换编码为唯一Pending脚控制量；验证原Foot点、项目Sole／Ankle、加权目标和实际骨骼分型且没有旧Landing完成门
- [ ] 8.6 删除旧五态响应、PlantWorldResidual、ContactWorldResidual域切换与无消费者字段；验证已复刻的原历史是唯一目标来源

## 9. 原骨盆与完整Foot结果

- [ ] 9.1 完整翻译HipHeightLiftingDelta两种函数、Min／MaxHipsDelta及两种原骨盆候选模式；验证候选选择、腿距条件和PelvisAdjustmentAdvance分支均已覆盖
- [ ] 9.2 完整翻译CalculatePelvisTarget的上下速率、PD、历史差量、查询、旋转与权重；验证不调用旧3C Spring、Primary Support目标或同层下沉限速
- [ ] 9.3 对齐disableDamping先供双脚消费、再由骨盆清零的顺序；验证两个骨盆模式都执行逐脚Finalize且根Bank尚未提前提交
- [ ] 9.4 将原脚与骨盆输出组成唯一Foot Result，迁移Resolved合同、Goal编码和原控制量所需字段；验证无重复加权、无旧Reach准入或第二最终结果
- [ ] 9.5 删除失去消费者的旧Primary Support、Pelvis响应、Reach夹取和配置；验证只保留实际Solver／诊断观察，Gameplay KCC与其他未修改模块保持原职责

## 10. 唯一求解与正式内容切换

- [ ] 10.1 完成原控制口／InScale／Rig／FBIKSetting到项目唯一Goal／Solver合同的映射；验证目标空间与附加标量的实际消费一致，确需改Solver实现时先明确原证据及现行spec冲突
- [ ] 10.2 成套迁移Corin的原区域输入、Profile、Rig Calibration、Pose输入绑定与参数来源；验证旧配置不再被读取，缺失原数据必须明确失败
- [ ] 10.3 显式重建Corin Projection、Float32与Fixed Program及所需绑定；验证身份、容量和原模式配置一致，不修改TrainingEnemy资产或建立兼容开关
- [ ] 10.4 在唯一编译计划中完成相依模块切换；验证Foot、Goal Assembler、Solver、Final Publication各执行一次，没有原生组件回调或图外骨骼写入
- [ ] 10.5 用已对齐的原目标和实际物理骨骼定位剩余差异；验证NodeTaskFBIK与当前backend未证实等价的部分不被当作完成，不能反调正确目标掩盖Solver差异

## 11. 诊断、原证据与同输入回放

- [ ] 11.1 将区域／状态／时间、运动、查询、两路支撑、各层历史、脚控制量、骨盆、Goal、Solver与Physical接入现有Committed事实和Generated DiagnosticEvent链；验证无Snapshot拼接、第二Writer或旧CaptureViewLease依赖
- [ ] 11.2 按E3现有字段映射对标准37份、快速重入窗口和扩展录制分别对账；验证只对输入完整片段复算公式，缺局部量、异步样本和未覆盖分支明确保留
- [ ] 11.3 对每个已迁移原函数保存输入／输出／历史和分支覆盖记录；验证离散结果与顺序一致、连续误差满足design容差，未翻译函数不能用总分替代
- [ ] 11.4 使用2.3锁定的同输入／同世界／同表现时钟回放Corin完整链；保存逐脚穿透、距离、位置／旋转跳变、膝弯、伸展和骨盆速度，验证目标与实际骨骼分别可追踪
- [ ] 11.5 对原开关分支保存可用片段与覆盖状态，基准仍采用Corin真实参数；验证未采到开启场景不虚构通过，原效果与3C环境差异不通过修改评分抹平
- [ ] 11.6 为每个实施、失败和恢复切片保存中文提交与精确产物索引；验证只撤销本切片确认引入的错误，原数据和用户其它改动不被覆盖

## 12. 清理与完成

- [ ] 12.1 清理被替换的Foot源数据、旧命名、旧配置和全部无消费者诊断列；用rg核对只剩同一原算法链，历史研究与失败证据保留
- [ ] 12.2 对账82方法、区域生产者、Corin状态／参数／Rig资源、查询分支和原生控制消费者；验证design第8节所有影响目标范围的缺项已解决，完整复刻没有未声明替代计算
- [ ] 12.3 执行规定参数的必要Runtime／Editor编译并立即关闭dotnet build-server；验证不运行Unity batchmode、不新增测试且编译结果不代替效果证据
- [ ] 12.4 执行git diff --check与本change严格OpenSpec校验，并逐项检查current specs／旧change冲突表；验证全部所需delta和旧任务清理完成，不能以CLI通过代替语义对账
- [ ] 12.5 汇总原算法等价、正式输入完整性、目标编码及Corin实际骨骼四类结果；验证全部完成条件有精确来源，不把只完成文档或局部公式称为完整运行实现
