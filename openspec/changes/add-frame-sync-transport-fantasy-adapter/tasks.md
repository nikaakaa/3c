## 1. Transport Port
- [ ] 1.1 定义 transport port 所在模块
- [ ] 1.2 定义 send input 方法
- [ ] 1.3 定义 send checksum 方法
- [ ] 1.4 定义 send handshake 方法
- [ ] 1.5 定义 handshake result 事件
- [ ] 1.6 定义 input ack 事件
- [ ] 1.7 定义 confirmed input set 事件
- [ ] 1.8 定义 correction 事件
- [ ] 1.9 定义 disconnect 事件
- [ ] 1.10 定义 diagnostic 事件

## 2. Fake Transport
- [ ] 2.1 定义 fake client
- [ ] 2.2 定义 fake room
- [ ] 2.3 定义 fake submit queue
- [ ] 2.4 定义 fake confirmed broadcast
- [ ] 2.5 定义 fake duplicate 注入
- [ ] 2.6 定义 fake missing 注入
- [ ] 2.7 定义 fake late 注入
- [ ] 2.8 定义 fake reorder 注入
- [ ] 2.9 定义 fake correction 注入

## 3. Fantasy Protocol
- [ ] 3.1 确认协议根目录
- [ ] 3.2 确认 Outer 协议目录
- [ ] 3.3 定义 handshake request/response
- [ ] 3.4 定义 input submit message
- [ ] 3.5 定义 input ack push
- [ ] 3.6 定义 confirmed input set push
- [ ] 3.7 定义 checksum message
- [ ] 3.8 定义 correction push
- [ ] 3.9 定义 diagnostic push
- [ ] 3.10 记录 protocol export 命令

## 4. Unity Client Adapter
- [ ] 4.1 定义 Session 持有边界
- [ ] 4.2 定义 handshake 发送映射
- [ ] 4.3 定义 input submit 映射
- [ ] 4.4 定义 checksum submit 映射
- [ ] 4.5 定义 confirmed input push handler 映射
- [ ] 4.6 定义 correction push handler 映射
- [ ] 4.7 定义 disconnect diagnostic 映射
- [ ] 4.8 确认 callback 只入队 transport event

## 5. Server Adapter
- [ ] 5.1 定义 room id 和 session/player 绑定
- [ ] 5.2 定义 room input queue
- [ ] 5.3 定义 tick confirmer
- [ ] 5.4 定义 confirmed broadcaster
- [ ] 5.5 定义 duplicate diagnostic
- [ ] 5.6 定义 missing diagnostic
- [ ] 5.7 定义 late diagnostic
- [ ] 5.8 定义 checksum report collector
- [ ] 5.9 定义 correction broadcaster
- [ ] 5.10 定义 disconnect cleanup

## 6. 自动测试
- [ ] 6.1 添加 transport port 纯数据静态测试
- [ ] 6.2 添加 port 不引用 Fantasy 静态测试
- [ ] 6.3 添加 port 不引用 Unity runtime 静态测试
- [ ] 6.4 添加 fake transport confirmed broadcast 测试
- [ ] 6.5 添加 fake duplicate/missing/late 测试
- [ ] 6.6 添加 adapter callback 不推进 gameplay 静态测试
- [ ] 6.7 添加 Fantasy handler 不引用 Character runtime 静态测试
- [ ] 6.8 添加 `.g.cs` 不手改检查
- [ ] 6.9 添加 protocol export 产物存在性检查

## 7. 验证
- [ ] 7.1 运行相关 EditMode 测试
- [ ] 7.2 运行 protocol export
- [ ] 7.3 运行 dotnet build
- [ ] 7.4 运行 `openspec validate add-frame-sync-transport-fantasy-adapter --strict --no-interactive`
- [ ] 7.5 运行 `openspec validate --all --strict --no-interactive`
