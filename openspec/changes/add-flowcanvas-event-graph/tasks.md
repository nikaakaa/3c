本清单按用户确认的UE式作者分工执行，替代上一版以输入清理/取消Corin事件图为主的方向。保留已正确的原生runtime、C#作者能力、Pose算法及旧协议删除结果。不新增测试或验证任务，不安排反复Build或回放。

## 1. 原始数据合同与所有权

- [ ] 1.1 按design D2/D3/D7固定原始观测、七项派生量、旧生产/消费位置与文件Owner，保留MovementMode和原动作规则。
- [ ] 1.2 对接Pose/Presentation提供的Velocity、Rotation、Grounded、DesiredPlanarVelocity、DesiredFacing、HasMotion等原始Fact及身份时钟，事件宿主从同一schema读取，不再接收待迁移的派生结果。
- [ ] 1.3 保留正式时间对齐、输入快照和Reset代际，只把动画派生公式与专用历史迁出；不在事件图重建Gameplay状态或时钟。

## 2. 原生类型、节点与唯一帧

- [ ] 2.1 在同一EventGraph类型合同、声明/配置和C#薄适配中补齐本次所需Vector2/Vector3、Quaternion只读输入及MotionPhase枚举，保持Float/Int32/Bool精确值。
- [ ] 2.2 接通正式原始Fact输入节点及原生数学、比较、分支、Get/Set和Macro所需能力，业务阈值和分支保留在图中，不新建“计算全部动画数据”的C#大节点。
- [ ] 2.3 将新类型纳入唯一Contract/Layout/Frame和静态消费绑定，保留实例/采样/tick/Reset/版本及只读租约，不以Float/object或第二变量表中转。

## 3. Corin真实动画更新图

- [ ] 3.1 保留并重写CorinAnimationEventGraphAuthoringCode，用正式API生成原始输入、计算节点、变量、执行线和数据连接，替换仅Start/Update的内容；不删除Corin事件图接入目标。
- [ ] 3.2 将HorizontalSpeed、VerticalSpeed、MovementDirection和DesiredDirection的原公式与零值分支生成到图中，保持XZ平面和原阈值。
- [ ] 3.3 将HorizontalAcceleration及hasPreviousSample/previousPlanarVelocity历史生成到图中，保持首次更新、delta和Reset语义。
- [ ] 3.4 将FacingError的原旋转/前向量/角度计算生成到图中，保持原数学，不修改Warp算法或增加新转身策略。
- [ ] 3.5 将MotionPhase的落地、hasMotion、速度和垂直速度判断生成到图中，保持原四个枚举值，不替换MovementMode状态条件。
- [ ] 3.6 将初始化、公开变量写入和历史更新按原先后关系连接，继续复用EnsureRoot和明确Profile绑定；已移除的占位资产沿同一正式生成入口恢复真实内容。

## 4. 原Pose消费者接入

- [ ] 4.1 向Pose任务提供D3的稳定变量ID、精确类型和唯一输出布局；Pose侧改原参数/Fact条件和节点绑定，不在本任务跨文件接管其实现。
- [ ] 4.2 对接现有PoseStateMachine预测选择的MotionPhase变量读取，及原RootOrientationWarp朝向误差读取；使用正式静态句柄，不在运行器硬编码变量名。
- [ ] 4.3 对接原方向/速度/加速度/阶段的其它正式消费者及MM chooser合同，保留原算法和可用能力；不为显示Get增加新BlendSpace、播放倍率或平滑用途。
- [ ] 4.4 保持Pose状态机、节点相关性、播放器/状态时间、曲线、混合、Foot/IK与最终Writer的既有职责，实例更新结果只作为输入。
- [ ] 4.5 Pose侧按同一原始Fact/动画变量分类更新Blackboard、完整Preview及Compiler绑定，本任务提供生产接口；每帧不重新发现全部消费者。

## 5. 旧派生链删除与C#作者收口

- [ ] 5.1 在对应图生产和真实消费者接通后，由所属Pose/Presentation任务删除旧Projector派生公式、字段、getter及专用历史；本任务删除旧Host派生Fact输入映射，不回填Fact作为兼容。
- [ ] 5.2 保持已退役Document/motor桥删除，清理本次确认无消费者的旧派生配置和引用，不恢复Action/Foot重复变量，不保留两个生产者。
- [ ] 5.3 让公共代码输出器的事件图薄适配完整表达新类型、真实公式/分支、历史、Macro、连接、布局和稳定身份，不省略不支持内容或输出占位。
- [ ] 5.4 通过原直接API和两个公共作者入口保存明确范围及Profile根引用，内部使用本次生成对象，保留范围外资源；作者生成不自动Build。

## 6. 文档与实施记录

- [ ] 6.1 按当前实际生产/消费对账本任务规范，明确旧派生Fact与新动画变量的唯一来源及UE参考差异，不覆盖其它规划窗口文件。
- [ ] 6.2 在原execution.md记录实际迁移、删除和未完成依赖，区分真实图内容与基础框架；不以空图、Profile绑定或产品发布替代业务接入，不新增测试或验证任务。
