# 本地预测回滚 F6/F8 验证

## 目的
验证 F6/F8 hidden rollback/replay 不污染当前角色、表现插值和 Cinemachine 相机状态，并确认 soak 长跑结果低噪声可搜索。

## 前置
- 打开 `3cDemo/Client/3C_Client/Assets/Scenes/Sandbox.unity`
- 确认 Unity Console 当前没有编译错误
- 进入 Play Mode

如果 MCP 或 Console 不方便读取，可以先在仓库根目录运行最近日志扫描：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-UnityEditorCompileLog.ps1 -Tail 5000
```

期望输出：

```text
UNITY_COMPILE_LOG_CHECK result=PASS tail=5000
```

## 静态接线检查
在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-RollbackWiring.ps1
```

期望输出：

```text
ROLLBACK_WIRING_CHECK result=PASS scene=Sandbox f6=True f8=True hidden=True presentation=True camera=True logs=True
```

## HITL 脚本自检
在真实按键前，可以先确认本地验收脚本自身不会误判旧日志、漏掉快速按键日志，且缺失 F8 会失败：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-RollbackHitlScripts.ps1
```

期望输出：

```text
ROLLBACK_HITL_SCRIPT_CHECK result=PASS passSample=True confirmSample=True missingF8Fails=True
```

## MCP 连接诊断
如果 Codex 无法直接跑 Unity Test Runner，先运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-UnityMcpConnection.ps1
```

若输出类似下面这样，说明 MCP server 正常、Unity 进程存在，但 Unity 没注册 instance：

```text
UNITY_MCP_CHECK result=FAIL reason=no-unity-instance server=http://127.0.0.1:8080 health=healthy instances=0 unityProcess=True
```

## 组合 Demo 验收
在仓库根目录先启动组合验收脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Invoke-RollbackDemoHitl.ps1 -TimeoutSec 120
```

然后回到 Unity：

1. 进入 Play Mode。
2. 移动角色几秒。
3. 按 `F6`，等待脚本进入 F8 阶段。
4. 继续移动角色几秒。
5. 按 `F8`。

脚本会先输出 `ROLLBACK_DEMO_HITL step=F6` 和 `ROLLBACK_DEMO_HITL step=F8`，看到对应提示后按键即可。

期望最终输出：

```text
ROLLBACK_DEMO_HITL result=PASS visualConfirmed=False action=confirm-hidden-f6-f8-did-not-shift-character-or-camera
```

如果你已经肉眼确认 F6/F8 后角色画面和相机视角没有永久偏移，可以用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Invoke-RollbackDemoHitl.ps1 -TimeoutSec 120 -ConfirmVisualStable
```

期望最终输出：

```text
ROLLBACK_DEMO_HITL result=PASS visualConfirmed=True
```

## F6 短窗口 synctest
1. 在仓库根目录先启动 HITL 验收脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Invoke-RollbackSynctestHitl.ps1 -TimeoutSec 120
```

2. 回到 Unity，移动角色几秒，让输入和快照历史积累。
3. 按 `F6`。
4. 期望脚本输出：

```text
ROLLBACK_SYNCTEST_HITL result=PASS
```

也可以在按完 F6 后单独运行断言脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-RollbackSynctestResult.ps1 -Tail 5000
```

期望输出包含 `ROLLBACK_SYNCTEST_ASSERT result=PASS`。hidden replay 后画面不应永久跳位，相机不应永久偏移。

## F8 soak 长跑
1. 在仓库根目录先启动 HITL 验收脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Invoke-RollbackSoakHitl.ps1 -TimeoutSec 120
```

2. 回到 Unity，移动角色几秒，让输入和快照历史积累。
3. 按 `F8`。
4. 期望脚本输出：

```text
ROLLBACK_SOAK_HITL result=PASS
```

也可以在按完 F8 后单独运行断言脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Test-RollbackSoakResult.ps1 -Tail 5000
```

期望输出：

```text
ROLLBACK_SOAK_ASSERT result=PASS tail=5000
```

若断言失败，再运行过滤脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Get-RollbackLog.ps1 -Tail 2000
```

过滤出的 `ROLLBACK_SOAK_RESULT` 通过条件：
   - `result=PASS`
   - `applyReplay=False`
   - `sourceRestored=True`
   - `visualRestored=True`
   - `cameraRestored=True`
   - `visualChecked=True`
   - `cameraChecked=True`

## 失败时复制什么
优先复制过滤脚本输出的全部内容。若输出里有 `ROLLBACK_SOAK_FIRST_MISMATCH`，必须一起复制。

如果没有任何输出，复制以下信息：
- 是否已进入 Play Mode
- 是否按了 F8
- Console 是否有编译错误
- `LocalRollbackSoakDebugRunner` 是否挂在 Sandbox 角色对象上

## 长时间观察
可以先运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\3cDemo\Tools\RollbackDiagnostics\Get-RollbackLog.ps1 -Tail 100 -Follow
```

然后回到 Unity 里按 F6 或 F8。脚本只会输出 rollback 关键日志，避免普通时间/动画日志刷屏。
