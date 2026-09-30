## ADDED Requirements

### Requirement: 共享工作区必须通过领域操作承载场景预览

共享工作区 MUST通过领域提供的操作和状态合同承载预览场景选择、运行控制、精确作者/运行目标绑定及生效状态。角色领域提供 Actor/SkillDefinition/ActionInstance 与调用路径，独立 Timeline 领域提供正式内容根、业务 owner/播放 identity、调用点和 generation；外壳 MUST不判断 Gameplay、Pose、Action、独立内容或相机字段，也不强制所有领域具有角色。已有 Toolbar、Navigator、Canvas、Details 和 Bottom Dock MUST继续复用同一交互和数据来源，不得复制为新的预览工作台。

#### Scenario: 从Graph进入场景预览

- **WHEN** 当前 Graph 领域提供合法的预览操作
- **THEN** 共享 Toolbar MUST显示其场景和运行状态
- **AND** Canvas 的 selection、clipboard、Undo 和导航 MUST继续使用原有实现

#### Scenario: 独立Timeline领域连接场景

- **WHEN** 领域适配器提供合法的非 Skill 内容与调用方观察合同
- **THEN** 共享操作 MUST复用同一场景启停和状态，显示领域提供的准确目标
- **AND** 外壳 MUST不添加 Character/Action 校验或持有内容播放状态

### Requirement: 窗口生命周期必须只管理本地预览视图

窗口 MUST只拥有本地技能、独立 Timeline、Pose 及其它仍保留的领域文档、选择、运行观察绑定和 interest；C#控制只提供正式配置与代码来源观察，不提供可编辑角色总控RootTree。切换文档及关闭普通作者页面 MUST 保持现有预览；关闭最后一个承载预览的窗口或明确关闭预览区域 MUST 由唯一隐藏宿主结束并释放预览。实际游戏观察窗口关闭 MUST NOT 终止游戏 Session。重载后 MUST按稳定 identity 重新绑定，不能恢复旧运行对象或因旧外壳描述恢复已退役领域。

#### Scenario: 关闭最后一个预览承载窗口

- **WHEN** 受控预览运行期间最后一个承载预览的窗口关闭
- **THEN** 隐藏场景宿主 MUST 通过对应正式业务 owner 停止驱动、结束 Session 并释放预览资源
- **AND** 窗口 interest MUST 全部释放，编辑器后台 MUST NOT 继续推进已经关闭的预览，不要求 Unity Stop

#### Scenario: 从技能页返回控制配置

- **WHEN** 作者从技能 Root/Timeline 返回 Character 控制配置
- **THEN** 窗口 MUST显示已登记 C# 模块、可配置字段及代码来源，不创建角色图
- **AND** 场景运行和其它窗口的准确实例绑定 MUST保持原归属

### Requirement: Ability 图必须联动实际执行时间线

打开 Ability MUST 能查看原 FlowCanvas 节点图，并联动独立可停靠 Preview 中的执行投影与角色视口，不要求先打开一个被调用的作者 Timeline。选择执行片段 MUST 能定位记录时的来源节点、Loop 迭代或 Timeline 调用；来源导航 MUST 不改变实际运行。纯 Timeline 基础编排 MUST 保持直接入口，不强制依赖 Ability 图。

#### Scenario: 打开 Ability 并选择循环记录

- **WHEN** 用户打开 Ability 并选择其第二次 Loop 中的节点执行片段
- **THEN** 图面 MUST 定位该片段对应的正式来源，并显示第二次迭代的运行身份
- **AND** MUST NOT 因来源节点相同而改选第一次记录或重建 Session
