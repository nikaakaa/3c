# Change: 增加角色运行时黑板

## Why
当前角色链路已经有输入快照、移动意图、状态机 context、动画进度 facts、动作 runtime facts 等纯数据事实，但这些事实分散在不同帧结构和控制器记忆里。后续接入脚步相位、方向起步、原地转身和跑动转角时，如果继续把跨帧事实散落到 Presenter 或局部控制器字段，会削弱统一状态机和动画表现层分离的边界。

本变更引入受控的 typed runtime blackboard，用来集中承载可被状态机、移动、动作、动画事实采样共享的纯数据事实，同时避免复制 BBB 风格的全局可变 `PlayerRuntimeData`。

## What Changes
- 新增 `character-runtime-blackboard` 能力，定义角色运行时黑板的职责、生命周期和边界。
- 黑板只保存纯数据 facts，不保存 Animancer、UnityEngine.Object、Transform、InputAction、Camera 或场景实例引用。
- 每类 facts 必须有明确写入权威，跨模块消费者只读，避免任意模块偷写。
- 黑板必须能参与 snapshot/restore，为后续本地预测、回放、同步测试和诊断保留确定性边界。
- 统一状态机 context 可以读取黑板快照，但状态机 runner 不成为黑板维护器。
- Presenter 只能通过只读进度或 facts adapter 报告动画事实，不允许通过黑板直接切状态或移动角色。

## Impact
- Affected specs:
  - `character-runtime-blackboard`
  - 后续可能修改 `locomotion-state-graph-config`
  - 后续可能修改 `basic-locomotion-animation`
  - 后续可能修改 `animation-phase-timeline-facts`
  - 后续可能修改 `simulation-tick-system`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/StateMachine/Model`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Model`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Model`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor`
