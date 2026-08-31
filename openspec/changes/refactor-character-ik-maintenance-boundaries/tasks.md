## 1. 固定合同与保护范围

- [x] 1.1 以用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`固定源码与行为基线，绑定233436运行证据及205014交叉对照；核对后续接口差异，不使用当前HEAD替代，不回退后续正确改动
- [x] 1.2 建立Foot/Pelvis/Bend持久字段的写入Owner、消费者、初始化与Reset清单，区分运行历史、帧结果和过程证据
- [x] 1.3 对齐current、stabilize及project中的初步/最终结果和过期硬Reach条款，保留当前Reach观察与Landing资格；失败候选不作为前置，直接重叠行为工作不得并行

- [x] 1.4 按数据流和修改权限表，将目标、权重、Support/Reach决定及阶段输出落实到具体方法，列出非权威读取和重复决定的迁移位置

## 2. 权威数据流与控制权收口

- [x] 2.1 将既有初步结果迁为模块内部typed脚请求和Pair，明确未加权/有效目标、Support与Reach观察视图，删除同义副本，不增建硬裁决DTO
- [x] 2.2 将Support事实和Reach观察准入收进唯一Foot请求生产者，删除Module从过程Motion.State/Step/Resolved重判的路径，保持当前条件和阈值
- [x] 2.3 将权重与当前实际消费的加权脚几何统一在请求生产者，移除临时Goal编码再反解Pelvis输入的往返，保留原空间变换和数值顺序
- [x] 2.4 将Stride/Pelvis所需事实投影为最小typed输入，删除下游对原始Landing、完整Path或过程Context的混用，不复制业务判断
- [x] 2.5 将唯一Primary选择、Pelvis目标和一次Response适配到请求和准备结果，保留各自状态Owner、可达观察及配置
- [x] 2.6 将完成凭据限制在Foot内部，保留本腿可达观察到原Landing完成的唯一反馈，不增加迭代、不夹脚、不改权重
- [x] 2.7 Encoder只读最终Resolved与Pelvis结果，Assembler只汇聚，Solver只处理正式Goal和Pending Pose，Writer唯一写骨骼；删除旁路数据、阶段误名及未消费参数
- [x] 2.8 通过现有生产/消费边界与只读视图落实权限，不逐层重复重检；Root只拥有根Bank事务，不接管业务数学

## 3. 运行历史与过程证据分责

- [x] 3.1 把CorrectionResponseFact中被下一帧读取的方向迁入最小typed Interpolation历史，保留现有方向限制、分域、残差和Reset语义
- [x] 3.2 将响应、接触和可达过程Fact改为本帧不可变证据，删除同义状态副本及对公开Diagnostics的运行依赖
- [x] 3.3 保持Pending开放状态与Committed结果有效性分责，不在本change启用或重设计stabilize拥有的Goal Sole历史接管
- [x] 3.4 用既有正式录制对233436核对状态搬移前后Goal、Pelvis、Foot连续输出、已保留膝向与实际骨骼，保存独立差异和版本证据

## 4. 独立修正Solver重置后的方向所有权

- [ ] 4.1 从现有Rig参考姿态与Profile准备过程取得精确typed方向初值及身份，非法准备显式失败，不新增默认轴或近似初始化
- [ ] 4.2 在明确清空Solver历史的初始化、Reset与调参路径统一重建正式方向，每帧仅由正式输入设置Vendor工作字段
- [ ] 4.3 删除空BendHistory时读取旧Vendor方向的路径，保护a40b71f可靠动画腿轴运输、Stable/Applied含义与既有退化分支，不恢复半球强翻或SmoothKnee
- [ ] 4.4 通过既有正式初始化/Reset入口保存新建与完全重置的对照，以及普通历史帧回归证据；缺少覆盖时保持本项未完成

## 5. 配套诊断记录与采样映射统一

- [ ] 5.1 在已完成的紧凑诊断链上按业务分组响应、Contact、Support、Reach/Pelvis证据，删除同义平铺拷贝，保留固定容量和同帧冻结
- [ ] 5.2 建立唯一Editor typed列绑定，在现有初始化入口一次检查名称、类型、单位、有效性和读写覆盖，不增加逐层重复校验
- [ ] 5.3 由同一绑定驱动Header、CSV写行、Analyzer读取和必需列校验，删除旧手工映射，保留单次解析、几何独立表和原始帧字节索引
- [ ] 5.4 集中当前格式identity，纯映射整理保持原ABI；真实语义变化才升级，删除被实际替换的旧reader/别名，不恢复展开facts.json
- [ ] 5.5 在独立输出目录对账字段、全部42项诊断、评分、紧凑明细及查询索引，保持规则与事件完整性，保留全部历史原包

## 6. 清理与收尾

- [ ] 6.1 按权威读取迁移清单清除旧类型、非权威过程读取、重复决定、未消费字段与映射，确认没有第二生命周期、第二采样器或图外Goal后处理
- [ ] 6.2 按规定参数编译受影响Runtime与Editor并立即关闭build server；仅在正式依赖确实变化时通过精确Corin Build发布匹配产物
- [ ] 6.3 对账正常帧行为保持与Reset边界行为修正，分别记录结果、已知限制和未覆盖输入，不以同一总分替代
- [ ] 6.4 更新本change真实任务状态与最终合同，核对current和active条款不存在旧Resolved/Pelvis含义覆盖，完成严格校验和差异检查
