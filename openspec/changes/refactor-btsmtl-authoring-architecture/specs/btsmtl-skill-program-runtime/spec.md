## Purpose

定义重度技能内容的作者数据、编译产物与实例执行边界，支持Tree、Timeline、局部状态机和参数化子图嵌套，并使共享模板、并发释放、完整停止和状态恢复复用唯一动作事务与模拟管线。

## ADDED Requirements

### Requirement: 技能定义必须拥有明确执行内容与策略引用

技能定义 MUST具有稳定身份、版本、唯一ActionProfile引用、入口图、参数签名与显式依赖。Tree、Timeline、TreeClip、局部状态机和嵌套子图 MUST是正式可编辑内容。作者数据 MUST不保存运行对象、释放身份或网络模型策略。

#### Scenario: 创建完整技能

- **WHEN** 作者创建技能并设置入口Tree与ActionProfile
- **THEN** MUST能在同一作者闭包编辑Timeline和嵌套子图
- **AND** MUST不要求先创建角色控制状态机

#### Scenario: 没有Timeline的技能

- **WHEN** 技能只包含等待、条件与局部Tree流程
- **THEN** MUST使用相同的定义、构建、ActionInstance和生命周期


### Requirement: 技能必须编译为只读数据并由唯一解释器执行

技能作者数据 MUST经唯一语义发射、校验和Numeric Target构建形成只读Skill Program，包含操作、时间数据、参数绑定、局部状态布局、引用及来源。正式Runtime MUST只解释已验证产物，不读取作者图、不克隆节点对象，不并存原始图解释器。

#### Scenario: 运行已发布技能

- **WHEN** Session启动一个有效Skill Program
- **THEN** MUST复用已装配解释器与只读运行索引
- **AND** MUST不现场构建IR或补查Unity作者资产

#### Scenario: 技能子图缺失

- **WHEN** 构建发现缺失子图或参数类型不匹配
- **THEN** MUST在发布前失败并定位定义与调用点


### Requirement: 技能释放必须以ActionInstance为唯一owner

每次接受的技能启动 MUST建立唯一ActionInstance，并绑定精确Skill Program。节点游标、Timeline进度、子图frame、局部变量、等待、generation和停止进度 MUST归属该实例的typed执行状态。MUST不创建第二个SkillInstance生命周期、action context镜像或模板级当前状态。

#### Scenario: 同一技能并发释放

- **WHEN** 准入策略允许同Actor同时启动同一个技能两次
- **THEN** 两个ActionInstance MUST共享只读Program并持有独立执行状态
- **AND** 任一实例停止 MUST不停止另一实例

#### Scenario: 策略禁止并发

- **WHEN** ActionProfile拒绝第二次释放
- **THEN** MUST按正式准入结果拒绝，且不得覆盖正在执行的实例状态


### Requirement: 子图调用必须具有签名和独立调用状态

子图 MUST声明输入、输出及局部变量；调用点 MUST具有稳定身份。调用开始时按值绑定输入，完成时按签名返回结果，中止时不得提交未完成输出。每个实例、调用点和activation generation MUST隔离状态；共享定义不得导致调用状态共用。

#### Scenario: 同一子图被并行调用

- **WHEN** Parallel的两个分支引用相同共享子图
- **THEN** 两个调用 MUST保有各自参数、游标、局部变量和停止状态

#### Scenario: 恢复嵌套等待

- **WHEN** 快照捕获时子图正在等待
- **THEN** 恢复后 MUST保留调用层级、参数、等待进度和返回位置

#### Scenario: 发现子图递归

- **WHEN** 子图依赖形成直接或间接调用环
- **THEN** Build MUST拒绝并报告完整引用链
- **AND** 显式Loop和跨技能组合引用 MUST按各自语义校验


### Requirement: 树和时间轴必须共享模拟推进与阶段边界

Tree、Timeline及TreeClip MUST在所属ActionInstance内使用同一SimulationTick。Decision TreeClip MUST在角色控制决策前仅生成当前Frame合法候选，Commit内容在正式技能执行阶段推进。MUST不产生Timeline私有时钟、独立scheduler或动画回调驱动的Gameplay推进。

#### Scenario: 决策窗口参与同Tick取消

- **WHEN** 当前Tick的Decision TreeClip开放闪避取消窗口
- **THEN** 角色控制 MUST能在同Tick正式决策中读取该候选
- **AND** MUST不等待下一渲染帧或读取动画播放头


### Requirement: 父级停止必须关闭完整技能执行范围

Complete、Cancel、Interrupt、Reject、Abort和teardown MUST沿唯一动作生命周期处理本实例的子图、Timeline、等待、作用域及临时资源。graceful停止中 MUST保存可恢复进度；terminal后只允许必要清理，不得产生新的正常技能输出。重复停止 MUST不重复释放或影响其他实例。

#### Scenario: 取消嵌套技能

- **WHEN** 技能内多个子图与Timeline仍为Running时父级被取消
- **THEN** MUST对本实例全部活动内容发出停止并完成其清理
- **AND** 新实例 MUST不继承旧调用frame或临时效果

#### Scenario: 回滚到停止中

- **WHEN** snapshot保存的是尚未完成的graceful stop
- **THEN** 重算 MUST从该停止进度继续，MUST不把它当作已完成或重新激活


### Requirement: 技能输出必须通过正式领域与管线入口

技能 MUST通过typed上下文读取角色事实，并通过正式请求、贡献或输出合同使用Motion、GE、Equipment和Presentation。技能不得直接修改代码控制状态、World Body或Unity对象。未来弹道等能力 MUST通过已安装的模拟模块与状态合同扩展，未安装能力必须被构建拒绝。

#### Scenario: 技能产生位移

- **WHEN** 技能Timeline或Tree计算运动内容
- **THEN** MUST进入唯一Motion合成、WorldResolve与Finalize
- **AND** MUST不直接修改Transform或调用具体运动组件

#### Scenario: 使用未安装弹道节点

- **WHEN** 作者内容引用尚不存在的弹道能力
- **THEN** 构建 MUST明确拒绝
- **AND** MUST不发布占位Program或添加空Projectile Pass


### Requirement: 技能数据更新必须锁定完整依赖与状态版本

Skill Program、子图依赖、ActionProfile、数值Target、状态布局及所需代码语义 MUST形成完整可验证发布闭包。新数据 MUST经正式Build与资源发布采用；运行中的释放与Session不得原地换成另一份Program或旧版状态。

#### Scenario: 共享子图更新

- **WHEN** 共享子图语义变化影响多个技能
- **THEN** 构建 MUST更新受影响闭包与身份并原子发布
- **AND** 仍使用旧目录的Session MUST保持其版本，不能只替换某个子图


### Requirement: 技能Program必须使用独立定义索引与显式外部绑定

Skill Program内部的operation、常量、调用点和局部状态位置 MUST以技能定义为索引域。角色事实、输入、GE／Equipment与其他外部依赖 MUST通过明确typed绑定访问，不能嵌入某个Actor实例或角色包的全局可变状态地址。角色组合只链接已验证技能与本实例存储；相同技能定义不得因两个释放而复制可变模板。

#### Scenario: 多个角色组合复用技能

- **WHEN** 两个兼容角色组合使用同一Skill Program
- **THEN** 技能内部定义索引 MUST保持自身一致，各组合显式解析外部binding
- **AND** ActionInstance与调用状态 MUST分别属于各自角色事务
