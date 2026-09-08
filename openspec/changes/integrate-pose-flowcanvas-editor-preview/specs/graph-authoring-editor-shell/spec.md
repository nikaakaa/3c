## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

工作区 MUST提供目录、唯一图区域、Details、工具栏及可折叠运行观察。Pose使用原生图编辑器，按AnimGraph、Layer、状态／Rule和Rig角色组合内容；各区域不得保存第二份图或选择。有限Action Timeline沿用原时间轴作者表面，通过明确上下文打开，不把所有动画与事件轨道摊到AnimGraph。

#### Scenario: 查看角色动画总装
- **WHEN** 作者打开角色AnimGraph
- **THEN** 根图 MUST显示状态机、Slot、层和控制图等作者功能，并能进入其内容
- **AND** 默认Details MUST服务当前资源与参数编辑

#### Scenario: 打开Action Timeline
- **WHEN** 作者从Slot或播放引用导航到有限动作
- **THEN** 系统 MUST打开原Timeline资产及其Slot轨道／Sections／混合设置，不创建第二Montage编辑副本

#### Scenario: 打开BTSMTL RootTree
- **WHEN** 作者打开技能正式根入口
- **THEN** 工作区 MUST装配技能当前正式表面及字段
- **AND** 不强制替换画布或加入Pose／Rig字段

#### Scenario: 打开Character Pose Graph
- **WHEN** 作者从精确Profile打开动画工作区
- **THEN** 原生图编辑器 MUST承载唯一图、目录、Details和观察区域
- **AND** 不构造旧GraphView镜像

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

平移、缩放、页签、折叠、选择、观察目标和诊断显隐 MUST属于窗口状态；作者节点位置及明确布局属于相应图的正式layout。面包屑 MUST基于稳定调用关系恢复，重载不能依赖旧对象引用。打开、返回、居中和悬停不得改作者拓扑、布局或产物。

#### Scenario: 返回父调用
- **WHEN** 作者从共享层或状态图通过面包屑返回
- **THEN** 窗口 MUST回到进入时的父图和调用对象，不跳到另一个同名图

#### Scenario: 折叠Bottom Dock

- **WHEN** 作者折叠Preview与Diagnostics区域
- **THEN** Graph Canvas MUST扩展使用可用空间
- **AND** 当前Graph asset MUST不变脏

#### Scenario: domain reload恢复窗口

- **WHEN** Editor domain reload后恢复Graph窗口
- **THEN** Shell MAY恢复editor-only布局状态
- **AND** document与runtime target仍 MUST按各自稳定identity重新绑定，不得恢复旧对象实例

### Requirement: Shell必须保持重操作的显式触发边界

Build、资源生成、作者迁移与正式资产发布 MUST由明确命令触发。普通Inspector、节点选中、目录打开、窗口恢复和AssetDatabase刷新不得自动Build、解码大型产物或修复作者数据。

#### Scenario: 选择一个节点
- **WHEN** 作者反复选择Slot或Rig节点
- **THEN** 详情 MUST读取已准备的作者数据及缓存观察，不重新编译或等待Job

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** Projection Build MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

## ADDED Requirements

### Requirement: 图窗口的运行观察不得拥有预览生命周期

图窗口 MUST观察普通Play中的真实Actor，绑定或释放相应兴趣，不加载场景、创建角色、运行作者图或管理私有时钟。Timeline、Layer与Rig观察都读取同一正式执行链；Unity自身暂停时保留最后完成帧，窗口不提供独立播放／单步／seek。

#### Scenario: 未播放时打开控制图
- **WHEN** Unity不在Play且作者打开Control Rig
- **THEN** 图 MUST保持正常作者编辑并显示未播放，不启动隐藏求解或预览角色
