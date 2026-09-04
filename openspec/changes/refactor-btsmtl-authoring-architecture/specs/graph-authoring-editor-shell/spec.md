## MODIFIED Requirements

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

Navigator、Details和Bottom Dock的宽度、展开、折叠、选中页签、搜索、分组与Preview面板布局 MUST只保存为window-local或Editor view-state。任何布局变化 MUST不修改Graph、Timeline、Profile、Definition、Rig、Program或Projection revision。窗口尺寸不足时区域折叠 MUST遵循确定规则，MUST不切换到旧Data/Inspector互斥写路径。

作者工作区 MUST在脚本编译期间保持窗口及其可恢复位置，不得仅因开始编译就主动关闭窗口。编译或重载期间无法解析数据时 MUST暂停编辑命令并显示当前不可用状态；重载完成后 MUST按稳定文档身份、正式owner和有效页面路径重新绑定，恢复该窗口已有的布局、页面、可解析选择与画布位置。恢复过程 MUST不保存旧运行对象或生成产物实例，不修改作者资产，不自动Build，也不发出播放、重建或动作输入。未提交手势不得在恢复过程中自动变成资产修改。

#### Scenario: 折叠Bottom Dock

- **WHEN** 作者折叠Preview与Diagnostics区域
- **THEN** Graph Canvas MUST扩展使用可用空间
- **AND** 当前Graph asset MUST不变脏

#### Scenario: domain reload恢复窗口

- **WHEN** Editor domain reload后恢复Graph窗口
- **THEN** 工作区 MUST恢复该窗口的editor-only布局状态以及仍然有效的文档和页面位置
- **AND** document与runtime target MUST按各自稳定identity重新绑定，不得恢复旧对象实例
- **AND** 恢复过程 MUST不自动构建或启动运行

#### Scenario: 脚本编译发生在状态图编辑期间

- **WHEN** 作者位于某个嵌套状态图且Editor开始脚本编译
- **THEN** 窗口 MUST保持打开并暂停不可执行的编辑命令
- **AND** 编译与重载结束后 MUST恢复仍存在的状态图、选择和画布位置
- **AND** MUST不要求作者重新从根资产逐层打开页面

#### Scenario: 恢复的页面已被外部删除

- **WHEN** 重载后原页面或选中实体无法从原owner和稳定身份解析
- **THEN** 窗口 MUST显示具体失效位置，并允许作者显式返回仍有效的上级页面
- **AND** MUST不按名称猜测其他页面、不选择任意角色，也不得创建替代资产

## ADDED Requirements

### Requirement: 工作区只读刷新必须保护正在进行的作者编辑

运行事实、构建状态和诊断数值的刷新 MUST只更新对应显示，不得重置未变化的页面、选择、滚动位置、折叠状态或正在编辑的字段。刷新 MUST不提交、取消或覆盖作者尚未提交的字段输入与拖拽草稿；写入仍由既有正式编辑命令和Undo边界决定。当前owner版本发生外部变化而使编辑草稿失效时，系统 MUST明确报告该变化并按正式冲突或取消规则处理，不能把旧草稿写入新版本。

折叠区域在没有可见内容变化时 MUST不反复重建隐藏控件。展示开销的减少 MUST不改变运行结果、运行目标、诊断采集资格或领域编辑权限。

#### Scenario: 作者输入数值时运行快照更新

- **WHEN** 作者正在输入一个当前允许编辑的数值，运行目标发布了新的只读状态
- **THEN** 字段焦点、未提交文本和当前选择 MUST保持
- **AND** 运行采用值与状态 MUST在各自只读位置更新
- **AND** MUST不额外产生Undo记录或作者数据修改

#### Scenario: 折叠运行观察区域

- **WHEN** 作者折叠运行观察区域而正式角色继续运行
- **THEN** 工作区 MUST保持页面和目标绑定
- **AND** MUST不为每份运行快照重新创建整组隐藏控件
- **AND** 再次展开时 MUST显示当前合法事实

#### Scenario: 外部修改使拖拽草稿失效

- **WHEN** 作者拖拽期间对应作者owner被另一个正式入口修改
- **THEN** 工作区 MUST使旧版本草稿失效并明确显示原因
- **AND** MUST不按过期的实体索引或曲线key索引覆盖新内容

### Requirement: 工作区本地绑定的重建与关闭必须只产生一次相应效果

同一个窗口的本地视图绑定在重建、重新打开页面和重载恢复后，单次作者操作 MUST只提交一次相应编辑或运行命令，同一订阅 MUST不重复消费同一通知。对于视图订阅和观察绑定，关闭、解绑或重复清理 MUST只释放该窗口拥有的部分，不能清空其他窗口的选择、页面或观察目标。关闭通知 MUST只表达实际关闭，不得在窗口创建时发出。

本要求只约束视图的订阅、观察与关闭通知，MUST不改变对应播放规格定义的会话启动、停止和释放行为，也不定义场景运行模式或全局运行所有权。

#### Scenario: 多次重绑后点击一次命令

- **WHEN** 一个窗口经历多次页面切换和重绑，作者点击一次正式命令
- **THEN** 对应命令 MUST只提交一次
- **AND** MUST不因重复订阅产生多次Undo、播放请求或同一回调执行

#### Scenario: 两个窗口观察同一目标后关闭其中一个

- **WHEN** 作者关闭一个观察窗口
- **THEN** 系统 MUST只撤销该窗口的本地绑定和观察兴趣
- **AND** 另一个窗口的页面、选择与合法观察绑定 MUST保持
- **AND** 被关闭窗口 MUST不再接收后续通知

#### Scenario: 创建和关闭窗口

- **WHEN** 工作区先创建并在之后实际关闭
- **THEN** 创建期间 MUST不发出关闭通知
- **AND** 实际关闭 MUST只通知一次，即使清理被重复请求
