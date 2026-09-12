## Purpose

定义通用事件图的首个动画应用：按照正式表现更新时机读取角色事实，执行作者计算与变量读写，再向同一动画实例的姿势消费者发布完整的只读输入。它不替代角色事实、姿势采样、动画曲线或最终骨骼写入。

## ADDED Requirements

### Requirement: 动画宿主必须提供初始化与每帧更新

动画宿主 MUST在首次有效输入到达后执行一次初始化，再执行本次更新。后续更新 MUST沿正式表现采样事件及其 delta 发生，不使用额外窗口时钟或自动组件 Update。暂停或没有正表现 delta 时 MUST不推进更新逻辑。

#### Scenario: 首次动画更新

- **WHEN** 一个新动画实例收到首次有效表现输入
- **THEN** 初始化设置的变量 MUST对随后同次更新可见，更新结束后再发布输出

#### Scenario: 两个逻辑 Tick 之间多次表现更新

- **WHEN** 表现系统以多个正 delta 更新同一角色
- **THEN** 图 MUST分别计算各次动画变量，且 MUST不重复产生 Gameplay 操作或推进其逻辑时钟

### Requirement: 动画图必须区分事实与作者状态

角色速度、落地等数据 MUST来自正式同次只读表现事实。作者动画变量 MUST由图自行声明和更新；动画速度等派生表现状态 MUST不替代真实移动速度。素材曲线、Foot 权重和 BlendShape MUST继续来自原有姿势采样与混合，不作为动画更新图的隐含可写状态。

#### Scenario: 作者平滑速度

- **WHEN** 角色速度为 4，旧动画速度为 0，响应速率为 2，本次 delta 为 0.1，作者按限速趋近计算
- **THEN** 图 MUST能够得到动画速度 0.2，角色速度 MUST仍为 4

#### Scenario: 修改 Foot 曲线的尝试

- **WHEN** 更新图试图以共享变量 Set 改写素材采样得到的脚权重
- **THEN** 合同校验 MUST拒绝该写入，不改变原曲线与求解行为

### Requirement: 动画变量必须按精确类型交接

动画变量输出 MUST按稳定图身份、变量身份、精确类型和实例来源引用同一原生声明。首个 Pose 交接 MUST支持 Float、Int32、Bool；方向或位置 MUST能在事件图内部使用原生 Vector2/Vector3 并以所需分量输出。未支持的对象、集合或直接 Pose 向量输出 MUST明确拒绝，不能经 object 或 float 冒充合法类型。

#### Scenario: 自建变量提供动画输入

- **WHEN** 作者新建合法 Float 变量并将其提供给 Pose 消费
- **THEN** 输出合同 MUST可引用该变量，不受三个固定 motor 名称限制

#### Scenario: 整数经过交接

- **WHEN** 作者输出合法 Int32 计数
- **THEN** 消费者 MUST收到同一整数，不能先转换 float 导致精度丢失

#### Scenario: 生成后恢复变量Get身份

- **WHEN** 显式执行已编译创建代码重建事件图并恢复指定动画根引用
- **THEN** 图与原生Variable.ID MUST按代码恢复，Pose Get MUST引用同一声明与唯一布局
- **AND** MUST不新增第二变量表或依赖已删除生成对象的物理GUID

### Requirement: 输出必须是一次完整更新的只读值

事件图成功后 MUST冻结本次消费者需要的值，携带实例、表现采样、图/合同版本和重置代际。Pose 消费者 MUST在同次推进中只读该输出，不读取正在修改的原生 Blackboard。缺变量、失配类型、跨实例或过期帧 MUST失败，不补固定 motor 值或作者默认值。

#### Scenario: 同次 Set 后 Pose 消费

- **WHEN** 更新事件将变量从 1 写为 2 并成功结束
- **THEN** 随后同次 Pose 消费 MUST读到 2，而不是前一次的 1

#### Scenario: 所需变量未绑定

- **WHEN** Pose 需要某变量但动画宿主没有完整输出绑定
- **THEN** 发布或接入 MUST明确失败，不能自动创建变量或调用旧固定参数桥

### Requirement: 动画更新完成必须与姿势提交完成区分

成功执行的原生更新 MUST按表现时间保留变量和节点状态，即使随后姿势因资源未准备好而未发布。系统 MUST不重跑或回退该次更新来伪装与 Pose 原子提交。更新结果只证明输入更新完成；姿势显示完成 MUST以原有最终提交结果为准。

#### Scenario: 姿势资源尚未准备好

- **WHEN** 更新图成功，但随后 Pose source 尚未准备好
- **THEN** 更新图状态 MUST保留，Pose MUST沿原规则结束本次 Pending
- **AND** 下一次有效更新 MUST从其真实状态继续，不使用旧参数或重跑上次事件

### Requirement: 动画实例重置和故障必须完整隔离

Actor/Body discontinuity、明确 Reset 或图 Replacement MUST结束旧事件图实例及其订阅和跨次状态，并在下一次有效输入上重新初始化。普通 PoseState 转移 MUST不重置整个事件图。更新图失败或 Actor 动画运行已 Faulted 时 MUST停止后续输出，直到正式 Reset/Replacement。

#### Scenario: 重置包含原生节点历史

- **WHEN** 动画实例被重置
- **THEN** 变量初值与原生节点历史 MUST共同重建，不能只清理对外值页

### Requirement: 动画宿主必须保持一次同步更新

首个动画合同 MUST只开放本次能完整结束的事件逻辑。涉及时间的作者计算 MUST消费宿主提供的 delta；绕过该 delta 的全局时间模式、协程等待、延时执行及使调用跨帧悬挂的断点 MUST不能进入该同步更新。

#### Scenario: 节点使用全局每秒赋值

- **WHEN** 节点配置为直接读取 Unity 全局 delta 的每秒赋值模式
- **THEN** 校验 MUST拒绝并指出应显式连接宿主 delta，不悄悄替换该原生模式含义
