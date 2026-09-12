## Purpose

定义采用UE式组织的角色动画作者体验：AnimGraph组合姿势，动画层封装状态机、Slot及骨骼混合，现有Action Timeline承担Montage职责，Control Rig表达身体修正。作者节点经唯一编译链生成既有Native运行计划，并在普通Unity Play中观察精确角色与调用的已完成结果。

## ADDED Requirements

### Requirement: 作者必须按图职责组织动画

系统 MUST提供AnimGraph、Animation Layer、State Machine、State Pose、Transition Rule和Control Rig作者表面。根图 MUST通过明确接口引用这些图，不展开所有内部执行步骤；目录、双击、详情与运行定位 MUST共享完整调用路径和唯一选择。不同图角色 MUST有相应节点目录、端口及输出约束，MUST不把Rig执行线、状态转换线与普通数据线混为一类。

#### Scenario: 进入上半身状态
- **WHEN** 作者从AnimGraph进入UpperBody Layer再进入其状态机与Aim状态
- **THEN** 工作区 MUST显示相应图和调用路径，返回时定位正确父调用
- **AND** MUST不默认跳到同一共享层的另一处调用

#### Scenario: 查看Corin根图
- **WHEN** 作者打开迁移后的Corin AnimGraph
- **THEN** 作者 MUST能从状态机、Slot、实际需要的惯性化与控制图引用理解最终姿势组合
- **AND** MUST不要求手接Action Playback Input、参数汇总或Goal Assembler

### Requirement: 动画层可以组合状态机Slot和骨骼混合

Animation Layer MUST声明Pose、参数和资源接口及唯一Pose返回，允许在内部组合状态机、Slot与Layered Blend Per Bone。层 MAY只产生姿势，由调用者混合；也 MAY显式接收Base Pose并在内部完成混合。层名称 MUST不隐式决定骨骼范围或生成Slot。共享Implementation与独立调用状态 MUST有明确绑定，不得在既有角色执行链之外启动第二执行器或私有时钟。层实例共享组与Slot Group MUST是不同身份和职责，不能按同名自动关联。

#### Scenario: 层内状态机被动作覆盖
- **WHEN** UpperBody Layer的状态机输出姿势并连接UpperBody Slot
- **THEN** Slot MUST能在当前层的Pose上下文中消费对应动作播放结果
- **AND** 骨骼范围 MUST来自该层内或调用者明确配置的按骨骼混合

#### Scenario: 两个调用使用同一层
- **WHEN** 同一Implementation在两个不同调用位置使用
- **THEN** 各调用 MUST按声明的共享组和调用identity管理状态及输入
- **AND** 运行观察 MUST区分调用，不将两次输出合并

### Requirement: Player必须直接表达资源和播放策略

Sequence Player MUST直接选择原生AnimationClip或明确的typed资源参数，Blend Space等Player MUST使用其正式资源。Player MUST提供有意义的速率、起始位置、Loop及进入行为设置；运行source binding与dense index MUST由编译生成。作者 MUST不为使用一个Clip先建立Source Slot再到Profile重复指定资源，MUST不恢复Animation Sequence包装资产或曲线副本。

#### Scenario: 同一Clip用于不同播放方式
- **WHEN** 两个Player引用同一Clip，一个配置循环，一个配置有限播放
- **THEN** 各source usage MUST按自己的正式播放策略编译，资源正文不复制
- **AND** Phase／同步约束 MUST使用该usage的有效时间语义

### Requirement: Slot路由与骨骼作用范围必须分离

Slot MUST是动作播放进入Pose组合的明确位置，可位于AnimGraph、Animation Layer或State Pose图。Slot MUST引用角色Rig目录的稳定Slot定义并提供Source Pose与源更新策略，MUST不保存Bone Mask或重新仲裁技能。Slot Group MUST只表达合法动作播放的互斥关系；Slot、Group、内部AnimationChannel与骨骼Mask MUST具有不同身份和职责。

#### Scenario: 多个动作共用上半身入口
- **WHEN** 攻击和换弹Timeline的动画轨道都引用UpperBody Slot
- **THEN** 两者 MUST进入同一Slot路由，并使用AnimGraph实际配置的上半身混合范围
- **AND** 各动作 MUST仍能拥有不同淡入淡出设置

#### Scenario: 子图中放置Slot
- **WHEN** 作者把Slot放入某层或某状态的Pose图
- **THEN** Slot MUST保持所属角色实例的播放路由，不自动获得私有命名空间或新播放实例
- **AND** 不相关的Slot求值位置不得令动作时间重复推进

### Requirement: 现有Action Timeline必须承担Montage职责

具有角色动画轨道的有限Action Timeline MUST继续拥有动画片段、事件／窗口及唯一时间与生命周期，并在原资产内提供Slot轨道、Sections、Blend In／Out、Blend Profile In／Out和自动退出设置。动画片段 MUST直接引用原生AnimationClip。系统 MUST不另建保存相同内容的Montage资产、独立播放游标或第二事件真相；通用Timeline不因名称类比而获得角色动画语义。

#### Scenario: 修改换弹动作的混合
- **WHEN** 作者修改换弹Timeline的动画Blend In时间和Profile
- **THEN** 修改 MUST只落到该Timeline的正式动画设置，其他共用Slot的动作设置保持独立
- **AND** MUST不在Slot或另一个Montage资源中复制该设置

#### Scenario: 跳到下一个Section
- **WHEN** 正式动作指令要求跳转或设置后续Section
- **THEN** 原Timeline MUST统一处理Section选择、动画采样及窗口退出／进入规则
- **AND** MUST不出现动画已跳段而伤害窗口继续旧时间的双时钟结果

#### Scenario: 同一动作使用多个Slot轨道
- **WHEN** 一个有限Action Timeline配置多个动画Slot轨道
- **THEN** 轨道 MUST遵守同一Montage式Group合同，并由同一动作实例推进
- **AND** Group互斥 MUST不替代Gameplay动作准入

### Requirement: 骨骼混合必须使用明确Mask层权重和过渡设置

Layered Blend Per Bone MUST支持Branch Filter或Rig关联Blend Mask、明确层顺序和Alpha，以及相应空间和Curve混合设置。Mask MUST表达各骨骼的基础混合范围，Alpha MUST表达该层当前贡献，Blend Profile MUST表达转换或动作进入／退出的逐骨骼混合行为。状态机转换与各Action Timeline MUST分别引用自己的过渡设置，Slot名称或层名称不得生成隐含Mask。

#### Scenario: 边跑边换弹
- **WHEN** 基础移动姿势和UpperBody Slot结果按上半身Mask组合
- **THEN** 未纳入该层的骨骼 MUST采用Base Pose对应数据，上身按动作及层权重混合
- **AND** 两条分支 MUST复用同帧源结果，不重复采样同一usage

#### Scenario: Mask引用其他Rig骨骼
- **WHEN** Mask、Branch Filter或Blend Profile与当前Rig不匹配
- **THEN** 作者提交或编译 MUST定位错误骨骼／owner并拒绝对应更新
- **AND** MUST不按名称猜测或补出默认骨骼映射

### Requirement: 惯性化必须保持显式节点和局部请求范围

Inertialization MUST是作者可见的Pose节点，处理由其上游Pose分支产生的明确惯性请求。每个请求 MUST拥有唯一时间设置来源；一个节点收到多个请求时 MUST使用有界集合与确定的最短duration选择，不能依赖消息到达顺序。节点 MUST保持局部history、完成帧rebase和Reset／Unavailable语义，MUST不自动改变Standard Blend或在Output前隐式创建全局惯性化。

#### Scenario: 状态与Slot都发出请求
- **WHEN** 同一处理节点的上游在同帧产生多个合法惯性请求
- **THEN** 节点 MUST按最短duration及稳定owner顺序选择对应设置并保留来源
- **AND** MUST不把其他未连接分支纳入同一残差

### Requirement: Control Rig必须表达控制目标而非内部组装流程

Control Rig MUST按Forwards Solve和明确的数据依赖组织已有骨骼控制、Foot Placement目标与FBIK Effectors。输入Pose、Rig、目标空间、骨骼、位置／旋转权重和实际支持的求解设置 MUST明确。目标打包、Goal Assembler和固定工作区属于编译展开，MUST不成为作者必须手接的节点。系统 MUST继续使用既有Foot／Goal／FBBIK算法与唯一Final Writer，不另建RigVM或第二求解器。

#### Scenario: 双脚贴地与手部目标共同作用
- **WHEN** 控制图给FBIK提供合法的双脚与手部目标
- **THEN** 编译产物 MUST形成同Frame／Rig的一组目标，并进入本计划唯一身体求解位置
- **AND** 作者 MUST不需要接内部Goal Set组装器

#### Scenario: 两个来源写同一Effector
- **WHEN** 两个目标来源竞争同一Effector而没有明确混合或选择
- **THEN** 编译 MUST指出两个来源与目标并拒绝发布
- **AND** MUST不按接线顺序覆盖或让多个求解器分别写骨骼

#### Scenario: 请求后端没有的参数
- **WHEN** 某UE PBIK专有设置没有当前FBBIK的实际对应实现
- **THEN** 作者面板 MUST不提供假装有效的可编辑字段，并明确后端支持范围

### Requirement: 原生交互必须完整进入领域事务

创建、连接、改接、复制、删除、名称、字段、接口和布局修改 MUST进入同一Capability与typed Mutation owner。批量失败 MUST不留下部分写入，详情 MUST恢复正式值并显示原因。状态和Alias MUST具有自己的可编辑详情；转换线 MUST可选中编辑策略并下钻Rule。普通重绘、选中与悬停 MUST不修改作者数据或执行重操作。

#### Scenario: 编辑状态别名
- **WHEN** 作者修改Alias名称或成员
- **THEN** 原状态机owner MUST提交完整成员集合，循环引用或未知成员不能部分保存

#### Scenario: 从端口创建节点
- **WHEN** 目标节点有多个同类型合法输入
- **THEN** 作者 MUST明确选择目标输入，不能默选第一个端口

### Requirement: 普通Play观察必须绑定版本实例和调用

窗口 MUST自动发现普通Unity Play的合法真实Actor，唯一匹配可绑定，多目标需明确选择。结果 MUST匹配Session／Actor、generation、Program／Projection、Rig、作者图版本、层Implementation及call-site；失配 MUST清空叠加并显示原因。窗口 MUST不创建场景、角色、作者图执行器或私有时钟，不自动Build。

#### Scenario: 播放中打开层图
- **WHEN** 已有匹配Actor且作者进入一个层调用
- **THEN** 窗口 MUST显示该调用的已完成结果，而不另启动层或状态机

#### Scenario: 修改子图
- **WHEN** 当前子图或Linked Implementation版本与运行产物不同
- **THEN** 窗口 MUST停止旧值叠加，不停止或热换真实角色

### Requirement: 观察必须显示已采集事实并释放生命周期

节点、端口、状态、曲线、Mask有效权重、Slot播放与Rig目标 MUST来自有界的已完成结果；一个作者节点展开的内部操作只按需显示。条件边 MUST使用实际读取，未采集、未读取、覆盖和0 MUST区分。关闭、退出Play、目标销毁／替换及脚本重载 MUST释放兴趣与失效结果，Unity暂停保留最后完成帧。

#### Scenario: 观察短路条件
- **WHEN** And／Or短路导致一个输入未被读取
- **THEN** 对应边 MUST不高亮为已读取，并与结果False／0分开显示

#### Scenario: 关闭窗口
- **WHEN** 作者关闭观察窗口
- **THEN** 该窗口的兴趣和租约 MUST释放，角色执行次数与时钟不受影响

### Requirement: 作者组织迁移必须在代码完成后一次收敛

新组织涉及真实作者结构变化，系统 MUST在代码及正式读取/配置API完整后，通过显式C#导出/生成迁移精确Corin闭包，保留可保留的identity、资源、动作时间、过渡及IK设置。Action Timeline正文原位扩展，旧Source Slot、内部作者节点和废弃配置在成功转换后删除。失败 MUST按既有资产服务处理，不换绑未完整的新根；不得新增整包同步事务。物理对象可重建，图/节点/变量业务ID MUST保持，内部引用使用新对象并恢复明确Profile/Definition根挂接，不依赖旧生成子资产GUID。产物只能通过独立显式Build更新，生成不得自动Build。

#### Scenario: 旧混合规则无法表示
- **WHEN** 某个旧exact pair策略无法无损迁入目标Timeline或转换设置
- **THEN** 迁移 MUST报告具体owner与规则并停止，不能猜默认值或保留隐藏兼容策略

#### Scenario: 迁移成功
- **WHEN** 正式事务保存了新图、Timeline设置与Rig控制引用
- **THEN** 本次生成 MUST恢复明确根挂接并清理指定范围内的退役对象，旧作者类型不再可创建或编辑；人工编辑不自动写源码，生成不自动合并未导出修改

### Requirement: Pose必须独立编译并由角色层完成接口装配

完整动画根、Rig、资源、可达图及Animation Input Contract MUST足以进入唯一Pose Compiler，无需合法SkillGraphs、Character Program、Numeric Target或场景Actor。输入合同 MUST保留Fact/Slot/World，共享变量部分引用事件图唯一Contract/Layout，公开子图入参与source-local曲线保留原来源；该合同只声明依赖，MUST不伪造运行值、读取过期角色产物或遍历技能实现来填充。

独立结果 MUST包含不可变Pose程序、资源及依赖清单、来源映射和动画输入接口，能够以自身输入identity／hash保存和复用。角色总Build MUST调用同一Compiler或复用精确匹配结果，在装配时绑定Gameplay producer、参数、Timeline播放消息、Rig与World能力，并原子发布角色产物。角色装配失败不能否定已成功的独立Pose编译，也不能发布未绑定的角色Projection。

#### Scenario: 技能入口缺失但动画输入完整
- **WHEN** Character的技能入口不完整，但动画图、Rig、资源和动画输入声明合法
- **THEN** 独立Pose编译 MUST成功生成自己的正式结果
- **AND** 角色组装／发布 MUST单独报告缺失技能输入，不把该错误当成Pose错误

#### Scenario: 动画自身的输入声明缺失
- **WHEN** 动画节点引用了未声明的参数、Slot消息或所需Rig资源
- **THEN** Pose编译 MUST失败并定位动画owner及输入
- **AND** MUST不生成假输入、默认角色或旧Program补足声明

#### Scenario: 没有角色上下文
- **WHEN** 作者仅提供完整动画根与其依赖并点击编译动画
- **THEN** 系统 MUST能执行唯一Pose编译，不要求先打开角色或配置SkillGraphs
- **AND** 普通Play观察仍需要真实运行实例，编译本身不创建预览角色

#### Scenario: 总Build复用动画结果
- **WHEN** 动画输入hash与已生成Pose结果一致且角色输入合法
- **THEN** 总Build MUST复用同一动画结果并生成正式接口绑定
- **AND** MUST不维护另一份仅供角色使用的Pose编译器或旧图中转

#### Scenario: 技能实现改变
- **WHEN** 技能内部逻辑改变但公开动画输入合同与动画作者数据未改变
- **THEN** Pose编译结果的失效判定 MUST只依据自身真实依赖
- **AND** 角色总Build仍 MUST重新处理受影响的Gameplay结果及绑定，不擅自放行错误接口

### Requirement: 完整动画预览必须沿同一公共变量输入

完整Pose/角色预览 MUST使用运行相同的动画宿主和事件图唯一Contract/Layout/Frame。Get、条件与BlendSpace MUST读取同次成功发布的Float/Int32/Bool精确类型帧，匹配实例、表现采样、Simulation tick、Reset和版本；消费者结束前输出不得重写。Source Pending不得回退已成功更新的事件状态，输入发布不得标记为最终Pose提交。窗口仍不拥有时钟或第二执行器。

单资源查看 MUST使用原正式资源调参合同。全部CharacterPresentationProgramParameterFrame消费签名迁移后，事件图任务才删除旧类型和生产方法，MUST不以默认motor值补齐缺失角色输入。

#### Scenario: 完整预览消费作者变量

- **WHEN** 正式角色预览使用事件图输出驱动条件和BlendSpace
- **THEN** 两者 MUST读取同一实例、同次成功发布的typed帧，并保持原Pose编译和提交边界
- **AND** 单资源查看不得借用该角色帧或恢复旧固定参数桥
