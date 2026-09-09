## Purpose

定义编译执行数据到原生图编辑器的只读观测合同，覆盖版本、角色和技能实例、子图调用位置、节点状态、连线和值显示，使预览与正式运行都能准确定位执行且不产生第二条运行链。

## ADDED Requirements

### Requirement: 调试映射必须与运行产物同版本

编译 MUST保存运行操作、值及状态与作者节点、端口、边和调用路径的来源关系，支持一个节点产生多条操作及优化消除。映射 MUST绑定精确产物和作者版本并随产物组发布。映射不匹配或缺失时，编辑器 MUST明确停止对应状态叠加，不得猜测节点或触发自动构建。

#### Scenario: 运行后修改作者图
- **WHEN** 当前作者图与正在执行的编译版本不同
- **THEN** 窗口 MUST显示版本不匹配及来源信息，不把旧执行索引投射到新节点

#### Scenario: 节点被常量折叠
- **WHEN** 编译没有为作者节点保留运行操作
- **THEN** 观察 MUST显示优化状态，不捏造运行经过事件

### Requirement: 运行观察必须选择精确实例和调用位置

观察 MUST绑定Session、角色、技能释放实例、generation、产物及调用路径。共享子图的不同调用 MUST分别定位；父图显示调用状态，进入子图 MUST继承所选调用实例。观察绑定和页面历史 MUST保持editor-only。

#### Scenario: 同一共享子图被调用两次
- **WHEN** 作者从父图的第二个调用节点进入子图观察
- **THEN** 高亮和值 MUST仅来自第二次调用，面包屑 MUST保留对应调用位置

#### Scenario: 所观察技能已经结束
- **WHEN** 所选释放实例结束且新一次释放启动
- **THEN** 窗口 MUST标记旧实例已结束，不自动把新实例状态混入旧页面

### Requirement: 节点与连线必须显示真实执行事实

瞬时经过、持续运行、等待、完成、取消和中断 MUST可区分。边高亮 MUST有实际分支或读取证据；值 MUST来自执行时采集结果。UI刷新、鼠标悬停和打开子图 MUST不重新执行节点、读取有副作用的getter或修改作者状态。

#### Scenario: Timeline仍在等待结束
- **WHEN** 其启动调用已返回但Timeline仍运行
- **THEN** 节点 MUST保持持续状态及可用进度，不能仅闪烁一次后表示完成

#### Scenario: 显示技能条件值
- **WHEN** 作者悬停技能条件连接
- **THEN** 窗口 MUST显示最近已采集结果及帧信息，不推进技能执行

### Requirement: 原生运行外观必须支持只读外部观测源

编辑器 MUST复用已有节点、端口、连接和子图显示能力消费编译运行诊断，MUST不为激活高亮而启动第二份作者图、绑定执行委托或伪造运行状态。运行模块 MUST不依赖编辑器图对象，观测适配 MUST不保存第二份可编辑拓扑。

#### Scenario: 打开运行可视化
- **WHEN** 作者开启编译技能图的运行观察
- **THEN** 技能执行次数 MUST不增加，原有正式执行器仍为唯一业务推进者

### Requirement: 诊断采集必须有界且在完成边界发布

观测 MUST沿已有诊断通道按兴趣采集并限制存储；跨线程结果只在正式完成边界发布只读快照。丢失、覆盖或未采集数据 MUST明确显示。无观察订阅时 MUST不因编辑器维护无界逐帧记录；关闭观察 MUST释放订阅。

#### Scenario: 高频执行超过可保留范围
- **WHEN** 部分观测事件被覆盖
- **THEN** UI MUST标记数据缺口，不将缺失解释为节点未执行

### Requirement: 技能预览必须只是普通运行观察

Unity Play中的技能 MUST通过原Session和ActionInstance普通执行，窗口只观察某次真实释放的已完成结果。窗口 MUST不创建预览角色、技能实例、时钟或独立场景，也不要求专用场景协调器作为前提。多个实例时 MUST明确选择；释放结束后不得自动混入下一次释放。若提供释放按钮，MUST只提交正式技能请求并遵循原准入规则。窗口不得提供私有播放、单步、seek或原生执行断点。

#### Scenario: 普通输入释放技能
- **WHEN** Play中角色通过正常输入启动技能
- **THEN** 窗口 MUST能绑定此次真实释放，无需另启动预览运行

#### Scenario: 停止Play或暂停Unity
- **WHEN** Unity暂停或退出Play
- **THEN** 窗口 MUST分别保留最后完成结果或释放运行绑定，不自行推进技能

#### Scenario: 单独子图缺少输入上下文
- **WHEN** 子图没有可用的正式角色及参数上下文
- **THEN** 系统 MUST等待真实角色与释放实例，并沿父调用定位，不伪造默认输入启动预览

### Requirement: 运行观察必须绑定正式Simulation Session

技能观察 MUST绑定Actor、Simulation Session、Program identity、Pipeline/authority identity、SkillId、ActionInstanceId、generation和调用路径。Rollback重演、Server Authority纠正或Snapshot恢复后，观察 MUST按正式运行身份重新确认目标；MUST不把网络传输对象、作者变量名或最终Pose当作运行身份。

#### Scenario: 网络纠正后继续观察Skill
- **WHEN** Server Authority或Rollback对已选择Skill释放执行纠正
- **THEN** 观察窗口 MUST显示目标版本变化或重新绑定正式实例
- **AND** MUST不把纠正前的作者Graph状态伪装成当前运行状态

