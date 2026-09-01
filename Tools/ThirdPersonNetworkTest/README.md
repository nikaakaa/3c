# Network Test Orchestrator

这是 Network Test Candidate 自带的普通 .NET 8 Windows 会话进程。Launcher 只负责启动它；它不进入 Unity Player，也不处理 Gameplay。

## 命令

```text
ThirdPerson.NetworkTest.Orchestrator.exe validate --candidate <NetworkTestProduct.json>
ThirdPerson.NetworkTest.Orchestrator.exe start --candidate <NetworkTestProduct.json> --slot <SlotId>
ThirdPerson.NetworkTest.Orchestrator.exe stop --run <RunRoot>
```

`validate` 校验 Candidate schema、Git 身份、精确文件闭包、Tool Bundle、Session Plan 和当前 Candidate 自己的 Orchestrator 身份。

`start` 先独占正式 Slot lease并检查声明端口，再创建 `Build/Network/RunLogs/<Product>/<RunId>`。Candidate-owned adapter只接受生成的 `RunManifest.json`，把本 Run 配置写入 `Config`，把日志写入 `Logs`，把启动进程身份写入 `Processes.json`。Orchestrator通过Windows Job Object持有这批子进程；停止或异常只回收本 Run。

`stop` 只向目标 Run 写入停止请求。它不按程序名、可执行文件目录或端口扫描并终止其它会话。

## 进程数量

Deterministic Rollback每个运行会话是五个进程：Orchestrator、Relay、GM、Client A、Client B。Unity Authority是Orchestrator、Fantasy Server、Authority Worker、Client A、Client B。DotRecast Authority是Orchestrator、Fantasy Server、Client A、Client B。

Orchestrator是额外的控制进程，代价是一份很小的常驻.NET进程；收益是Candidate、工具、端口、Run目录和子进程所有权都有唯一归属，可以安全并行并保留可追溯证据。
