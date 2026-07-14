## MODIFIED Requirements

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST 区分 logic root 和 visual root。`CharacterController` 或等价 logic root MUST 继续表达碰撞、判定、网络预测和 motion correction 的逻辑真值。表现插值 MUST 只应用到显式配置的 visual root / model root。PresentationFrame MUST NOT 调用 `CharacterController.Move`，MUST NOT 反写 logic root position/rotation，MUST NOT 修改 `MotionResult` 或 `MotionCorrectionApplicationResult`。Presentation MUST 从正式 correction application result 获取 application extent，MUST NOT 从 motion debug snapshot 获取运行决策。

#### Scenario: 本地 motion 插值

- **WHEN** previous logic sample 和 current logic sample 都有有效 logic pose
- **THEN** PresentationFrame MUST 使用 interpolation alpha 计算 visual position 和 visual rotation
- **AND** 计算结果 MUST 应用到 visual root
- **AND** logic root MUST 保持 MotionStage 在 logic tick 中结算出的 Transform

#### Scenario: 网络校正后表现贴合

- **WHEN** logic tick 收到 motion correction 并产生 MotionCorrectionApplicationResult
- **THEN** correction MUST 仍由 MotionStage 的 correction phase 处理
- **AND** Presentation MAY 对部分应用使用普通 logic sample interpolation，对完整应用维持当前立即贴合行为
- **AND** Presentation MUST NOT 把 correction 当作新的 motion contribution
- **AND** diagnostics 开关 MUST NOT 改变表现结果
