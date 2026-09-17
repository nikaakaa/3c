# character-animation-event-graph Specification

## Purpose
参考UE动画蓝图的作者分工，将项目当前已有的动画派生计算、判断及实例变量更新放入原生事件图，供Pose及原动画模块消费。C#保留原始事实接入和底层执行，迁移保持原公式与表现，不以空图或新动画效果替代实际内容。

## Requirements

### Requirement: 原生更新的Pending和故障语义必须保持

已成功的事件状态 MUST在随后Source Pending时保留；更新失败 MUST不发布部分结果，Pose Actor Faulted时停止其后续更新。Reset/Body discontinuity/Replacement MUST完整重建该实例变量与原生历史。更新 MUST使用宿主delta并保持一次同步执行，不新增时钟、跨帧等待或事件编译器。

#### Scenario: 事件更新成功但Pose未就绪

- **WHEN** 更新成功而Pose source返回Pending
- **THEN** MUST保留原生状态并由Pose按原规则处理Pending，不重放或回退该次事件

### Requirement: 已有动画数学与阶段语义必须保持

迁移 MUST保持Unity原XZ水平平面、速度/方向零值分支、角度计算、加速度历史及原阈值。MotionPhase MUST保持原四个类型化枚举值；地面分类使用原HasMotion或speed>0.0001条件，空中分类使用原VerticalSpeed>0条件。系统 MUST不照抄UE示例阈值，不新增步频、播放倍率、平滑或状态策略。

#### Scenario: 原地面移动分类

- **WHEN** Grounded为true
- **THEN** 图 MUST按原HasMotion或水平速度大于0.0001的规则产出GroundedMoving或GroundedStationary
- **AND** MUST不改变现有动作选择语义

#### Scenario: 原空中分类

- **WHEN** Grounded为false
- **THEN** 图 MUST在VerticalSpeed>0时产出AirborneRising，否则产出AirborneFalling

### Requirement: Pose局部执行与素材曲线必须保留原职责

PoseGraph及原底层模块 MUST继续拥有状态机执行、节点相关性、播放器/状态时间、source采样、混合、曲线传播、Foot/IK与最终输出。EventGraph MUST只提供动画更新结果，不重排这些引擎步骤，不Set Action/Foot/BlendShape曲线或反写Gameplay。

#### Scenario: 原朝向修正读取更新结果

- **WHEN** 已配置的原RootOrientationWarp消费FacingError
- **THEN** MUST从唯一动画变量读取原等价值，再由原模块执行姿势修正
- **AND** MUST不把姿势求解算法移入全局事件图

### Requirement: 原始输入和变量交接必须保持精确类型

原生节点、作者配置、C#输出、唯一变量声明/布局/帧和Pose读取 MUST完整支持本次迁移所需Float、Int32、Bool、Vector2/Vector3、Quaternion只读输入及MotionPhase枚举。枚举 MUST保留类型身份和值，方向与整数 MUST不通过Float或字符串模拟；运行边界 MUST不使用不透明object代替正式typed值。

#### Scenario: Pose读取MotionPhase或方向

- **WHEN** 原消费者读取本次更新的枚举或向量
- **THEN** MUST获得同一声明的精确类型和值
- **AND** MUST不创建第二变量表、浮点别名或旧Fact回填

### Requirement: 原始事实接入与动画变量必须分责

Body/Intent时间对齐、已提交Grounded/MovementMode、原始速度/旋转/期望数据、时钟和身份 MUST由正式只读Fact接入提供。待迁移动画派生值 MUST从EventGraph唯一变量帧读取，不能既保留旧Fact字段生产又增加同名图变量，也不能把更新结果回填Fact作为兼容。

#### Scenario: 原MovementMode规则继续工作

- **WHEN** 既有Pose转移根据已提交MovementMode进行判断
- **THEN** MUST保持原Fact身份和规则
- **AND** MUST不改成新速度阈值策略或第二份Gameplay变量

### Requirement: 实例历史必须随原生更新生命周期维护

加速度所需previousPlanarVelocity和hasPreviousSample MUST属于当前图实例；首次更新及Reset后的首帧加速度 MUST为0，后续采用原速度差长度除以本次delta。公开变量计算 MUST先使用上次历史，最后才写入本次历史。事实接入层专门为已迁移计算保存的同义历史 MUST删除。

#### Scenario: 首次与后续加速度更新

- **WHEN** 一个新实例或重置实例执行首次更新
- **THEN** MUST得到原首帧加速度结果并保存本次历史
- **AND** 下一次更新 MUST从同一实例上次历史计算，另一Actor的历史不能参与

### Requirement: 无需求装配不得成为绕过既有迁移的路径

没有任何动画更新需求的其它根可以使用明确无图合同；存在必需变量或既有待迁移动画计算的根 MUST提供完整生产与绑定。Corin本次已经明确具有上述计算，MUST不能以无需求模式删除其业务接入。显式绑定合法图不能因零输出被隐式跳过，缺生产者 MUST失败而非fallback。

#### Scenario: Corin缺少已确定变量

- **WHEN** 原消费者需要MotionPhase或其它已确定迁移值而图未提供
- **THEN** 接入 MUST明确失败
- **AND** MUST不切换为无图模式、空结果或旧Fact读取

### Requirement: 一次更新输出必须完整且只读

正式顺序 MUST为原始Fact准备、一次原生动画更新、完整typed结果发布、同次Pose消费。帧 MUST携带实例、表现采样、Simulation tick、Reset代际和合同/layout版本；消费完成前 MUST不覆盖，不向Worker暴露可变Blackboard。消费者需求与句柄 MUST由现有编译或实例绑定确定，不能每帧重建另一输入布局。

#### Scenario: 图内Set后同次Pose消费

- **WHEN** 原生事件完成已有动画变量的写入
- **THEN** 同次Pose MUST读取这次完整结果
- **AND** 缺失、错类型或跨实例结果 MUST失败，不能补默认或旧值

### Requirement: 动画派生计算必须由作者图表达

现有HorizontalSpeed、VerticalSpeed、MovementDirection、DesiredDirection、HorizontalAcceleration、FacingError和MotionPhase的派生计算 MUST迁入原生动画事件图。公式、阈值、分支、变量写入与所需历史 MUST可在图或其原生Macro中追踪，不通过一个C#整段预处理节点隐藏全部业务，不继续由旧Projector重复生产。

#### Scenario: 作者查看运动阶段计算

- **WHEN** 作者查看Corin更新图
- **THEN** MUST能沿正式输入找到落地、移动意图、速度阈值和垂直速度判断及MotionPhase写入
- **AND** MUST不只有Start/Update或对某个C#总更新函数的调用

### Requirement: Corin必须拥有实际更新内容和消费

Corin正式事件图 MUST包含原始Fact输入、上述七项现有计算、变量Set/Get和连接；生成入口及明确Profile引用 MUST恢复这些真实内容。原Pose预测状态选择 MUST读取本图MotionPhase；其它原派生值消费者按实际能力绑定同一变量。空图、未消费的变量列表、Profile引用或Build成功 MUST不能代替这一内容要求。

#### Scenario: 原Pose预测选择读取阶段

- **WHEN** 原Pose状态机执行使用运动阶段的预测选择
- **THEN** MUST读取本次图发布的MotionPhase，并保持原预测算法
- **AND** MUST不再读取旧Projector计算的同义阶段

#### Scenario: 重建Corin事件图

- **WHEN** 执行正式Corin生成入口
- **THEN** MUST重建真实输入/计算/分支/变量/连接与逻辑身份，并恢复指定根绑定
- **AND** MUST不生成仅有Start/Update的占位内容或删除接入目标
