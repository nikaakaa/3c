## Context
项目已经存在 URP 后处理、二次元角色 shader 参考和 `Assets/Shader/Scene` 目录，但还没有专门的场景植被能力。高草丛应先作为场景预览能力实现，服务于视觉风格和交互读数选择，不应绕过当前角色控制器或动作系统。

## Goals / Non-Goals
- Goals: 提供一块可快速放进 Sandbox 的高草丛预览区域。
- Goals: 草丛能被玩家或指定 Transform 推开/压弯，反馈足够明显。
- Goals: 草丛外观可在二次元色块和较自然草色之间调参，不先锁死最终风格。
- Goals: 生成结果可复现，方便测试和对比。
- Non-Goals: 本变更不做大世界植被流式加载。
- Non-Goals: 本变更不接入潜行、隐身、敌人感知或战斗逻辑。
- Non-Goals: 本变更不新增独立角色控制器、摄像机路径或网络同步路径。
- Non-Goals: 本变更不引入第三方植被系统。

## Decisions
- Decision: 第一版使用项目内置 mesh/card 草片和 URP shader，而不是 Terrain Grass。
- Alternatives considered: 使用 Unity Terrain Grass。该方案快速，但交互和二次元风格控制较弱，且不适合当前小范围 3C 预览。

- Decision: 使用 `ScriptableObject` 配置草丛生成参数，生成器只消费归一化配置。
- Alternatives considered: 把参数直接写在 MonoBehaviour。该方案短期更快，但不利于测试、复用和后续场景多块草丛对比。

- Decision: 交互源以 Transform 位置和半径表达，shader 根据交互点压弯草片。
- Alternatives considered: 为每根草维护 CPU 物理状态。该方案更真实，但第一版成本高，且不利于稳定测试。

- Decision: 先支持单交互源，预留后续多交互源扩展点。
- Alternatives considered: 一开始支持多个角色/敌人同时交互。该方案更完整，但会扩大 shader 参数和测试面。

## Risks / Trade-offs
- 草片数量过多会增加透明排序和 overdraw：第一版限制预览范围和密度，并提供安全钳制。
- 二次元草如果色块过硬可能显假：提供颜色梯度、边缘强化和风摆参数用于比较。
- 透明草片可能和角色/后处理排序产生问题：第一版优先使用 alpha clip，减少半透明排序风险。
- 交互只基于单点半径，不模拟真实碰撞：先满足角色穿过高草丛的读数，后续再评估是否需要更复杂弯曲。

## Migration Plan
1. 新增能力不会修改现有角色控制器和动作系统。
2. 新增默认关闭或独立摆放的高草丛预览 prefab。
3. 在 Sandbox 中只放置可手动启用的预览对象，避免默认污染画面。
4. 用户确认草丛视觉方向后，再另起变更接入关卡布局、潜行或战斗逻辑。

## Open Questions
- 最终草丛偏二次元色块，还是偏自然风格化，需要通过预览对比决定。
- 是否需要角色进入草丛时驱动音效、粒子、隐身或敌人感知，等视觉预览通过后另起变更。
