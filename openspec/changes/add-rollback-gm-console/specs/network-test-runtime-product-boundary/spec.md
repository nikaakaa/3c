## MODIFIED Requirements

### Requirement: Network Test Product必须由显式Runtime Artifact列表组成

Network Test Product manifest MUST使用 schema v2 记录 NetworkModelIdentity、RuntimeTopologyIdentity 与全部 runtime artifacts。每个 artifact MUST声明唯一 RoleId、Kind、ProductId、受约束相对 root、entry point、configuration identity 及所属 manifest path/hash。公共系统 MUST不使用固定 Player/Server 字段或按目录猜测闭包。

#### Scenario: Rollback 开发产品包含独立 GM

- **WHEN** Build 生成 Rollback 产品 manifest
- **THEN** artifacts MUST精确包含 `unity-client-player`、`deterministic-relay-server`、`development-gm-server`
- **AND** GM MUST是独立 ManagedExecutable，不能藏进 Relay 命令分支或 Player Scene

#### Scenario: Artifact 路径逃逸

- **WHEN** artifact 路径规范化后离开 ProductRoot
- **THEN** Build 或 Run MUST在启动前失败，不修复或搜索路径

### Requirement: 三个产品必须拥有精确且隔离的Artifact闭包

Unity Authority 与 DotRecast Authority 产品 MUST保持各自现有 artifact 闭包。Rollback Development 产品 MUST包含 Unity Client Player、portable Relay Server 与独立 GM Server。不同产品 MUST使用不重叠输出目录；修改 Rollback 工具配置不得改变其它产品。公共 workflow MUST通过既有附加 artifact 合同处理 GM，不增加产品类型分支。

#### Scenario: 构建 Rollback GM 服务

- **WHEN** Rollback adapter 发布独立 GM artifact
- **THEN** 公共 workflow MUST照常执行文件集合、hash、候选验证及原子替换
- **AND** MUST不修改 Authority 产品输出或默认附加 GM
