## Purpose

规定 Timeline 秒制作者时间、逻辑更新与表现采样各自的职责。同一内容适配不同逻辑 tick 率，同一配置内保证确定性推进；同一动作的动画、表现 Marker 与相机共享正式采样结果，作者编辑、图执行、事件调和与停止遵守一致合同。

## ADDED Requirements

### Requirement: Timeline 作者时间与逻辑调度必须区分

Timeline MUST以秒作为唯一作者时间单位，起点、时长、Marker、Section、ClipIn、循环边界和 Timeline 自有时间坐标 MUST使用同一正式时间表示。归一化参数与源坐标 MUST通过正式映射派生或解释，不得保留可写作者帧副本或生成独立 tick 版资产。SimulationTickRate MUST来自正式 pipeline 配置。既有逻辑播放管理者 MUST在 SimulationTick 内计算动作推进，Timeline MUST被动遍历提供的秒制区间；tick 编号 MUST保留为模拟身份。表现位置 MUST NOT写回作者内容或逻辑游标。

#### Scenario: 同一秒制内容适配不同tick率

- **WHEN** 同一段 1 秒内容从 0 开始、无暂停且以正常速率在 30Hz 或 120Hz SimulationTick 下播放
- **THEN** 累计进度 MUST分别在 30 或 120 个 tick 后到达 1 秒，作者内容 MUST不改变
- **AND** 0.25 秒事件 MUST分别在第 8 或第 30 tick 被经过；MUST NOT据此承诺不同 tick 率下碰撞和输入响应完全相同

### Requirement: 秒制逻辑推进必须保留确定性状态

正式秒制表示 MUST明确精度、范围、舍入、速率换算和边界比较规则，不得默认用表现浮点 delta 累加决定逻辑事件。精确动作时间、倍率、暂停及换算余数 MUST归既有逻辑播放管理者；Timeline MUST保留必要求值状态，两者 MUST沿同一 Step 和 Capture / Restore 链一致提交恢复，不得有两个独立推进的逻辑游标。正常前进 MUST按 `(previous, current]` 遍历点事件，起点由开始经过处理，循环按正式分段与稳定排序处理。一次调用 MUST处理所有经过边界；短窗口边界交付 MUST NOT被宣称为已完成窗口内碰撞采样。

#### Scenario: 非整除步长与回滚

- **WHEN** 固定 tick 步长或播放速率不能整除所选时间精度，并从快照恢复后重放相同配置与输入
- **THEN** 精确动作进度、余数和事件顺序 MUST与原执行一致，不得丢弃余数造成累计漂移

#### Scenario: 暂停与变速不修改内容

- **WHEN** 正式播放控制暂停或改变动作速率
- **THEN** Logic MUST按控制改变动作时间推进，事件作者秒数 MUST保持不变
- **AND** MUST NOT维护一份需要随暂停或变速重写的作者事件 tick 表

### Requirement: 秒制迁移必须删除旧时间存储路径

迁移 MUST统一更新正式作者字段、资产、生成代码、闭包、指纹、派生格式、编辑器 mutation、导出重建和运行消费者。旧作者帧 MUST按已知旧时间基准转换，源素材帧 MUST按其来源映射处理。迁移后 MUST删除旧帧存储、兼容读取和双写，MUST NOT保留按不同资产版本选择两套时间推进的路径。

#### Scenario: 带来源帧身份的旧资产迁移

- **WHEN** 一个事件同时含旧作者帧位置和素材 LocalFrame 身份
- **THEN** 作者位置 MUST转换为唯一正式秒数，内容身份与引用 MUST保持
- **AND** LocalFrame MAY保留为来源标识，但 MUST NOT成为另一份可写调度时间

### Requirement: Timeline必须被动消费正式播放进度

既有播放管理者 MUST提供播放身份、执行域、前后动作秒数、完整循环／分段经过、推进原因和事件资格。Timeline MUST据此遍历边界、映射源采样并产生生命周期及领域候选，MUST NOT自行读取 Unity 时间、注册独立更新、累计另一份动作进度或再次应用动作倍率。被动求值 MAY保留活动 Clip、边界和去重状态；MUST NOT新增并列播放器、Registry 或影子运行链。推进和求值热路径 MUST复用既有预分配存储并保持 0 GC。

#### Scenario: 相同区间具有不同推进原因

- **WHEN** 两次调用的前后秒数相同，但一次是正常前进、另一次是 Seek 或修正
- **THEN** Timeline MUST根据原因处理事件资格，MUST NOT仅凭位置差补发修正区间的 Marker

#### Scenario: Decision截停或调用被丢弃

- **WHEN** 请求区间被内容 Decision／阻塞边界截停，或所属 Step 被 Discard
- **THEN** 播放管理者 MUST与 Timeline 求值状态在同一事务接受实际位置或共同丢弃候选
- **AND** MUST NOT把请求终点直接提交而遗漏中途截停，也不得先推进时间再单独丢弃事件

### Requirement: 业务慢动作必须通过正式播放控制改变进度

子弹时间、hitstop 与动作变速 MUST由正式播放管理者解释为进度变化，不得写 Unity Time.timeScale、Time.fixedDeltaTime 或为此改变 SimulationTickRate。有效倍率 MUST在动作推进处应用一次；跟随 committed sample 的表现 MUST不重复缩放。逻辑控制的生效 Step、组合和恢复 MUST进入原确定性控制／快照链。暂停解除 MUST由业务控制的正式来源决定，不得等待已冻结动作游标自行抵达解除点。Clip 源倍率 MUST只影响源映射，不得隐式修改动作进度和战斗窗口。

#### Scenario: 子弹时间减速

- **WHEN** 正常动作增量为 0.02 秒且正式有效倍率变为 0.1
- **THEN** 播放管理者 MUST提供 0.002 秒推进，Timeline MUST按该区间求值
- **AND** 动画、随动作 Marker 与 Camera MUST消费同一动作位置，不再重复乘 0.1，作者秒数和编辑网格 MUST保持不变

#### Scenario: 只调整动画素材速度

- **WHEN** 作者只修改某个 Clip 的源采样倍率
- **THEN** MUST只改变动作秒数到素材时间的映射
- **AND** 逻辑窗口和动作进度 MUST不被隐式加速

### Requirement: 表现进度策略必须由正式业务装配选择

表现进度来源与修正方式 MUST由正式业务装配明确给出；执行域、游戏类型、网络模型名称和单个 Clip MUST NOT成为消费者中的隐式策略开关。策略 MUST遵守该业务现行的 sample、horizon、终态与连续性合同，不得提供缺失配置后的自由播放 fallback。逻辑 Motion、Warp、Window 等 MUST只消费逻辑域数据，不得读取表现私有进度。

当前有限 Action MUST继续基于 committed raw sample 投影；locomotion MUST继续使用自身正式 prepared binding 的 FreeRun 或 CommittedMovement。CommittedMovement MUST视为逻辑派生事实，不构成新增调度域。对正式合同允许的独立表现播放，装配 MAY选择表现 delta 自由推进；未来追赶策略也 MUST遵守相同输入输出和生命周期边界。

#### Scenario: 有限Action与locomotion同时存在

- **WHEN** 有限 Action 和 locomotion 在同一角色表现帧内更新
- **THEN** Action MUST消费其 committed sample 投影，locomotion MUST消费自己的 prepared binding
- **AND** 两者 MUST NOT因为共用角色或 Timeline 名称而被强制合并为一个时钟

#### Scenario: 普通业务中断动作

- **WHEN** 没有网络修正的本地业务接受一个动作中断
- **THEN** 动作 MUST按正式终态停止生产新事件
- **AND** 可见姿态如何过渡 MUST由原表现业务决定，不得以“单机无需同步”为理由继续触发旧动作事件

#### Scenario: 缺少策略所需样本

- **WHEN** 已装配的跟随策略缺少合同必需的样本或身份不匹配
- **THEN** 系统 MUST给出该合同规定的保持或失败结果
- **AND** MUST NOT自动切成另一策略或自行读取网络私有历史

### Requirement: 同一动作的表现消费者必须共享一次采样结果

同一动作 playback identity / generation 在一个 PresentationFrame 内 MUST只确定一次表现采样结果。动作动画、该动作 Timeline 的表现 Marker 和 Camera 内容 MUST消费相同的前后动作位置、循环经过、变化原因及事件资格。各 Clip 的起点、ClipIn 和源速率映射 MUST保留；source phase、混合过渡、locomotion 和独立生成效果仍 MUST由原 owner 管理。共享结果 MUST NOT成为第二份作者内容、逻辑权威或全角色统一游标。

#### Scenario: 动画和Marker消费同一动作

- **WHEN** 动作本帧从 0.24 秒正常采样到 0.26 秒
- **THEN** 动画与该动作 Camera MUST从该结果映射到各自源采样
- **AND** 0.25 秒 Marker MUST按同一经过和事件资格判定，不得用另一份 delta 累加结果

#### Scenario: 动作内Clip起点不同

- **WHEN** 两个 Clip 属于同一动作但起点或 ClipIn 不同
- **THEN** 两个源采样时间 MUST通过各自正式映射计算
- **AND** MUST NOT因为共享动作位置而强制使用相同源动画时间

### Requirement: 执行域必须限定内容的更新者与输出能力

Track MUST是执行域的唯一声明者且只能声明 Logic 或 Presentation。Clip 与 Marker MUST继承所在 Track，MUST NOT保留单独域覆盖或 DualProjection。内容类型 MUST校验轨道域是否受支持。Logic 输出 MUST由 SimulationTick 经 Advance / Commit 产生；Presentation 输出 MUST由表现帧产生且不得写 Gameplay fact、canonical input 或 SimulationState。两类轨道 MUST消费正式播放管理者提供的动作进度；逻辑结果传给表现 MUST走已有提交链，不得重复执行逻辑图。

Runtime MUST继续直接读取同一正式只读 Timeline 内容；每次 evaluation 只是当前调用的结果，不得生成第二 Timeline 操作表或常驻执行语言。

#### Scenario: 表现内容需要跟随已提交动作

- **WHEN** Presentation Track 使用跟随 committed sample 的动作采样
- **THEN** Track MUST仍在 PresentationFrame 运行
- **AND** 使用逻辑提交的进度来源 MUST NOT使它变成 Logic Track，也不得要求再调用一次逻辑 evaluator

#### Scenario: 轨道域不支持已有内容

- **WHEN** 作者将包含 Logic TimelineBody Clip 的轨道改为 Presentation
- **THEN** 作者提交 MUST原子拒绝修改，preparation MUST指出该内容的域能力错误
- **AND** MUST NOT在 PresentationFrame 复制执行 Logic 图

### Requirement: Marker必须与Clip同级且只提供点触发

Marker MUST由 Track 直接持有稳定 MarkerId、秒制触发位置和正式触发图引用，图 MUST只有 OnEnable 触发入口。Marker MUST NOT携带持续区间、Update / Exit 生命周期或成为 Clip 子列表。Logic Marker MUST经正式逻辑事务触发；Presentation Marker MUST使用该 playback 的表现采样结果触发，两域的执行职责分开但 MAY共享正式采样来源。

#### Scenario: 表现帧跨过Marker

- **WHEN** 一个已接受的正常表现经过跨过启用的 Presentation Marker 且当前实例具有事件资格
- **THEN** Marker MUST交付一次 OnEnable 事件
- **AND** MUST NOT为了触发而额外推进 SimulationTick

#### Scenario: Logic候选被丢弃

- **WHEN** Logic Marker 已形成候选但所属 Step 被 Discard
- **THEN** 其输出与私有候选 MUST一起丢弃，不留下已提交事件或业务状态

#### Scenario: 起点与静音轨道

- **WHEN** 新 playback 正式开始于含 0 秒 Marker 的轨道
- **THEN** 启用轨道的起点 Marker MUST按开始经过触发一次
- **AND** 静音轨道 MUST不产生该事件，起点重复采样 MUST不再次触发

### Requirement: Presentation触发图必须使用表现安全执行上下文

表现 Marker 的图 MUST经正式图编译与服务边界绑定精确 identity / revision、只读表现输入与已装配的 typed 表现输出能力。图 MUST NOT通过临时 Logic 调用栈执行，MUST NOT访问可写 SimulationState、产生 Gameplay fact 或调用 Kernel Evaluate / Finalize。图角色、域能力或下游资源不合法时 MUST报告明确错误，不得以空 invoker、空图或私有 Simulation context 代替。

图候选输出与事件记账 MUST遵守既有表现帧的接受 / 丢弃边界，未接受输出不得提前发布到下游。

#### Scenario: 表现图包含Gameplay节点

- **WHEN** Presentation Marker 图包含修改 Gameplay 状态或结束 Logic 片段的节点
- **THEN** 作者提交或 preparation MUST拒绝并定位该图与节点
- **AND** MUST NOT将它延迟交给 Logic invoker 执行

#### Scenario: 表现帧候选失败

- **WHEN** 图已产生候选，但本次表现帧被 Discard
- **THEN** 对应候选和未交付事件记账 MUST一起丢弃
- **AND** MUST不泄漏下游命令，也不得把未交付事件标成已消费

### Requirement: Marker事件身份必须区分正常经过与修正采样

Presentation Marker EventId MUST由 PlaybackHandle、Generation、MarkerId 与 TraversalIndex 组成。正常循环再次经过 MUST使用新的 TraversalIndex；同一经过多次重采样 MUST只交付一次。Seek、分支修正或位置回退 MUST不自动补发跨过的区间事件，不得仅为躲避去重而生成新的经过身份。需要替换或取消的已发事件 MUST沿原正式身份调和。

#### Scenario: 修正后再次采样相同位置

- **WHEN** 同 generation 的表现位置因修正回退，再次经过已交付的同一循环 Marker
- **THEN** 系统 MUST识别原经过，不重复交付该事件
- **AND** 修正本身 MUST不冒充自然循环

#### Scenario: 正常循环再次触发

- **WHEN** 同一 generation 按正常播放完成一个循环并再次跨过 Marker
- **THEN** 新事件 MUST拥有新的 TraversalIndex，正式消费端 MUST允许该次触发

### Requirement: 已接受终态必须关闭旧播放的新事件资格

正式 Stop / Cancel / generation 替换生效后，旧播放 MUST不再推进并产生新 Marker，已有采样缓存不得恢复其事件资格。终态时点 MUST服从既有 commit / confirmed horizon 合同；预测分支撤销 MUST使用最终分支更新，不得伪造 confirmed Complete / Release。已生成动画、相机和效果的退役或尾部 MUST由原领域结束策略处理，不得通过保持旧 Timeline 活跃来收尾。

#### Scenario: 循环Timeline停止

- **WHEN** 循环播放的 Stop 已由正式 owner 接受
- **THEN** 后续 PresentationFrame MUST不再产生该 playback / generation 的 Marker
- **AND** MUST NOT等到下一次循环末尾才生效

#### Scenario: 已生成表现需要尾部

- **WHEN** playback 已停止且已生成表现声明合法尾部
- **THEN** 尾部 MUST由该表现领域自身完成
- **AND** MUST不延长旧 Timeline 的事件生产资格或 Gameplay 窗口

### Requirement: 表现输出必须交给正式下游域

动画输出 MUST继续通过原 Action / Pose 生命周期；Camera 输出 MUST通过原 Camera domain 使用稳定身份调和。没有正式下游 domain 的能力 MUST在准备或调用边界明确报告不可用，不得用 payload 字符串、空实现或第二套事件系统宣称已经消费。

#### Scenario: Camera离开生效范围

- **WHEN** Camera 内容离开生效范围或所属播放正式结束
- **THEN** 原 Camera domain MUST收到对应稳定身份的退役结果

#### Scenario: 特效领域未装配

- **WHEN** 表现图请求尚未装配正式 domain 的特效能力
- **THEN** 准备或调用 MUST明确失败
- **AND** trace 或事件输出本身 MUST NOT被宣称为特效已经播放

### Requirement: Domain编辑必须保持整个作者内容一致

现有 Track 的 Domain MUST能够通过正式 Timeline 作者入口修改。Marker MUST显示继承域，域修改 MUST校验同 owner 内受影响的 Clip 显式域与图能力，并作为一次正式 mutation / Undo 提交。失败 MUST保留原内容并定位不兼容项，MUST NOT只改 Track enum 或静默删除节点。Domain MUST只表达执行与输出权限，不改变业务时钟策略。

#### Scenario: 将含Gameplay图的轨道改为Presentation

- **WHEN** 作者修改该 Track 的 Domain
- **THEN** 系统 MUST拒绝不合法转换并显示具体内容原因
- **AND** Track、Clip 与图 MUST保持修改前的一致状态

#### Scenario: 修改合法轨道域

- **WHEN** 受影响内容均具备目标域能力
- **THEN** 域声明、闭包与保存结果 MUST在同一正式 mutation 内一致更新
- **AND** Undo MUST恢复同一 owner 的完整改动

### Requirement: 作者吸附必须与可保存精度一致

拖动、秒输入、Marker 位置和 Clip 边界 MUST使用统一秒制时间表示与保存精度。作者 MUST能选择逻辑 tick、素材帧或关闭吸附。逻辑网格 MUST读取当前绑定 pipeline 的 SimulationTickRate，以 n/R 秒定位；素材帧网格 MUST使用正式素材帧率和映射。缺少绑定时逻辑吸附 MUST不可用，不得猜测默认频率。网格 MUST不限制底层保存精度，配置变化 MUST NOT重新量化作者内容。逻辑生效观察 MUST说明速率、暂停及起点前提。

#### Scenario: Presentation轨道拖动

- **WHEN** 作者关闭帧吸附，将 Presentation Marker 拖到两个显示帧之间
- **THEN** 交互落点与最终保存 MUST使用同一正式秒制精度
- **AND** MUST NOT在提交时重新量化回整数显示帧

#### Scenario: 调整运行tick率

- **WHEN** pipeline SimulationTickRate 改变而 Timeline 内容不变
- **THEN** 作者秒数 MUST保持不变
- **AND** 逻辑吸附网格 MUST跟随新配置更新，运行观察 MUST区分作者秒数与实际逻辑生效 tick

#### Scenario: 作者显式重新对齐

- **WHEN** 作者选择内容并执行按当前逻辑网格重新对齐
- **THEN** 系统 MUST通过正式 mutation 修改选中范围的秒数并提供一次完整 Undo
- **AND** 未选内容 MUST不被量化，共享 Timeline MUST明确显示当前参考 pipeline

### Requirement: Marker私有图必须随正式作者闭包重建

Marker 私有触发图 MUST随所属 Timeline / Graph 的正式 owner 闭包参与复制、删除、导出与生成。C# authoring 重建 MUST保留图角色、节点内容、稳定身份与引用关系，不得仅引用原私有子资产的路径 / localFileId 或用空图替代原内容。

#### Scenario: 重建包含私有Marker图的Timeline

- **WHEN** 作者通过既有 export_code / generate_assets 重建该 owner 内容
- **THEN** 生成结果 MUST包含该 Marker 及其完整私有触发图
- **AND** MUST不依赖旧 owner 下的私有图子资产仍然存在

### Requirement: TimelineData不得保留无语义的全局Scale

TimelineData MUST NOT持有无运行时语义的全局 Scale 字段；时间速率 MUST由正式播放控制或 Clip 源映射显式表达，MUST NOT保留旧字段、兼容读取或 fallback 配置。

#### Scenario: 请求变速播放

- **WHEN** 业务通过正式播放控制改变速率
- **THEN** Logic 换算与动作表现策略 MUST消费各自合同允许的同一控制来源
- **AND** MUST不读取 TimelineData.Scale 或编辑器私有缩放来改变运行进度
