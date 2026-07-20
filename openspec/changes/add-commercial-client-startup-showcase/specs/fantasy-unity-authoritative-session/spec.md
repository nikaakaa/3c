## ADDED Requirements

### Requirement: Gameplay 控制 Session 必须拥有独立 Fantasy Scene

ServerAuthoritative Unity Client 的 control Session MUST由对应 Network Model session module 独立创建、保存和销毁其 Fantasy Scene 与 KCP Session。公共 Fantasy client bootstrap MUST只负责 runtime 初始化，MUST不保存一个会在新连接前自动断开旧连接的全局可变 SessionFacade。普通产品 Auth WSS Session、其它 Network Model Session 与 control Session MUST互不拥有或销毁对方。

#### Scenario: Auth WSS 已连接后创建 ServerAuthoritative 控制连接

- **WHEN** ProductAuthSessionOwner 已持有有效 Auth Session 且 ServerAuthoritative module 开始 preparation
- **THEN** ServerAuthoritative module MUST创建自己的 Fantasy Scene 与 KCP Session
- **AND** control Connect MUST不调用 Auth Session 的 Disconnect
- **AND** 原有 KCP control 与 UDP Gameplay 数据面语义 MUST保持不变

#### Scenario: Control Session teardown

- **WHEN** ServerAuthoritative Source preparation 失败或 Active Session 结束
- **THEN** control session owner MUST只销毁自己持有的 KCP Session 和 Fantasy Scene
- **AND** MUST不关闭 ProductAuthSessionOwner、切换 Local Pipeline 或重新初始化全局 Fantasy runtime

