# 开发与发布 / Development and releases

[中文 README](../README.zh-CN.md) · [English README](../README.md) · [开发约束 / Agent guide](../AGENTS.md)

## 开发分支 / Development branch

先检查工作区并切换到 `dev`，保留已有未提交改动；需要时先把主线安全合入 `dev`。在 `dev` 完成开发、验证和提交，再按任务范围同步主线和远程，最后留在 `dev`。历史 release 分支不用于日常开发。 / Start development on `dev`, preserving existing changes and synchronizing the mainline baseline when needed. Validate and commit there, synchronize the requested branches/remotes, then return to `dev`.

## 构建 / Build

需要 .NET 8 SDK、Git；媒体测试需要系统 FFmpeg。 / Requires the .NET 8 SDK, Git, and system FFmpeg for media tests.

```powershell
dotnet restore FFGUIToolAvalonia.sln
dotnet build FFGUIToolAvalonia.sln --configuration Debug --no-restore
dotnet build FFGUIToolAvalonia.sln --configuration Release --no-restore
```

## 完整回归 / Full regression

测试自行生成合成媒体。完整回归要求 FFmpeg，不能把跳过当作通过。 / Tests create synthetic media. Full regression requires FFmpeg and must not skip media tests.

```powershell
$previousRequirement = $env:FFGUITOOL_REQUIRE_FFMPEG
try {
    $env:FFGUITOOL_REQUIRE_FFMPEG = '1'
    foreach ($configuration in @('Debug', 'Release')) {
        dotnet test FFGUIToolAvalonia.sln --configuration $configuration --no-restore --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "$configuration tests failed." }
    }
} finally {
    $env:FFGUITOOL_REQUIRE_FFMPEG = $previousRequirement
}
./scripts/release-check.ps1
./scripts/test-release-check.ps1
git diff --check
```

CI 对 `dev`、`main` 的 push/PR 运行 Windows/macOS/Linux 的 Debug、Release 检查。Windows Release 额外执行真实 Avalonia 恢复场景，10 分钟超时，退出码及 `metrics.json` 必须成功；诊断产物保存 14 天。CI 定义和本地验证不等于远程运行通过。真实硬件编码需另用目标设备验证。 / CI covers dev/main on all three platforms, plus a Windows Release recovery run with a 10-minute timeout and 14-day diagnostic artifacts. Remote results and hardware validation must be checked separately.

## 实际界面与千文件检查 / Desktop UI and 1,000-file checks

在有桌面的会话中运行，需要系统 FFmpeg： / Run in a desktop session with system FFmpeg:

```powershell
dotnet run --project scripts/UiChecks/UiChecks.csproj -p:OutputPath=bin/Debug/layout-check/
```

恢复相关检查可单独运行（产物目录需使用新目录）： / Run only the recovery checks with a fresh artifact directory:

```powershell
dotnet run --project scripts/UiChecks/UiChecks.csproj --configuration Release -p:OutputPath=bin/Release/layout-check/ -- TestResults/recovery-review --recovery-only
```

恢复模式只生成两张合成图片，并有界等待窗口和 ViewModel 完成初始化；失败时保留指标、日志和可用的窗口截图。 / Recovery mode creates two synthetic images, waits for initialization with a timeout, and retains failure diagnostics.

该检查覆盖原选中项参数、图片质量目标、滑块立即应用到全部、缺失文件状态、语言切换，以及旧/新共享文件夹工作区恢复。 / Covers selected-task settings, image goals, immediate slider application, missing inputs, language changes, and legacy/new shared-folder recovery.

本轮发布前检查见 [RELEASE_REVIEW.md](RELEASE_REVIEW.md)。实测记录见 [PERFORMANCE.md](PERFORMANCE.md)。当前 runner 还测量 1,000/10,000 项集合逆序和筛选恢复、513 文件缓存冷热切换、连续调参/保存、11,000 条进度事件及 32 张真实图片执行。内存是场景结束时的托管堆/工作集采样，不是峰值；含固定等待的总耗时不能当作纯操作耗时。

工具会打开真实 Avalonia 窗口，生成 1,000 个 64×64 PNG，检查独立/共享参数、预设与应用设置隔离、搜索防抖、排序、失败筛选、扫描取消及重扫。导出中英文/浅深主题的 760×560 截图、视频编码器布局截图、960×800 和 1360×900 截图、错误/结果截图以及 `metrics.json`；记录实际 `RenderScaling`。

The runner also measures continuous edits/saves, progress storms, short image batches, queue permutations and cache retention. See [PERFORMANCE.md](PERFORMANCE.md) for measured results and limitations. It records the actual render scale.

The runner opens a real Avalonia window, creates 1,000 synthetic PNGs, and checks settings isolation, search debounce, sorting, failure filtering, scan cancellation, and rescanning. It exports bilingual light/dark screenshots and `metrics.json`.

- 产物位于忽略目录 `TestResults/ui-时间戳`。/ Artifacts go to ignored `TestResults/ui-timestamp` directories.
- 配置、工具安装目录和日志通过绝对路径环境变量 `FFGUITOOL_APP_DATA` 隔离；未设置时沿用系统应用数据目录。/ The absolute-path `FFGUITOOL_APP_DATA` variable isolates configuration, installed tools, and logs. Normal runs keep the system app-data default.
- 计时包含布局更新和等待帧，搜索还包含 200 ms 防抖。/ Timings include layout updates and frame waits; search also includes the 200 ms debounce.
- 小型合成媒体不代表大型真实媒体探测耗时或所有设备性能。/ Small synthetic samples do not represent large-media probing time or every device's performance.

## 打包 / Packaging

版本从 `FFGUITool/FFGUITool.csproj` 读取。/ Package versions come from the application project.

```powershell
./publish.ps1 -Windows -Installer
./publish.ps1 -MacOS
./publish.ps1 -Linux
./publish.ps1 -All
```

```bash
bash ./publish.sh -macos --dmg
bash ./publish.sh -linux
bash ./publish.sh -all
```

| 系统 / System | Runtime | Package label |
| --- | --- | --- |
| Windows | `win-x64`, `win-x86`, `win-arm64` | `windows-x64`, `windows-x86`, `windows-arm64` |
| macOS | `osx-x64`, `osx-arm64` | `macos-intel`, `macos-arm64` |
| Linux | `linux-x64`, `linux-arm64` | `linux-x64`, `linux-arm64` |

产物位于 `FFGUITool/bin/publish/`：绿色包在 `archives/`，Windows 安装包在 `installer/`，macOS DMG 在 `dmg/`。/ Output subdirectories are `archives/`, `installer/`, and `dmg/` under `FFGUITool/bin/publish/`.

文件名格式 / Naming: `FFGUITool-v<version>-<platform>-Portable.zip`, `FFGUITool-v<version>-<platform>-Installer.exe`, `FFGUITool-v<version>-<platform>-Installer.dmg`.

Windows 安装包需要 Inno Setup；DMG 依赖 macOS 的 `hdiutil`。/ Windows installers require Inno Setup; DMG creation requires macOS and `hdiutil`.

## 发布 / Release

发布前可执行 `./scripts/release-check.ps1 -Tag v<version>`（替换为实际版本）。发布工作流显式传入 `RELEASE_TAG`；必须与 `v<Version>` 大小写一致，空标签、分支名及错配版本均失败。未指定标签的本地运行仅检查元数据；也可通过 `RELEASE_TAG` 环境变量检查。 / Release jobs explicitly validate the tag against the project version; local metadata-only checks may omit the tag.

同步项目版本、程序集版本、`app.manifest`、安装器默认版本、中英文 README 示例及 CHANGELOG。通过测试和元数据检查后，将发布提交合入 main，再推送对应的 `v<version>` 标签。标签会触发 GitHub Actions 构建、打包并上传 Release 附件；推送成功不代表构建已完成。

Update the project/assembly versions, manifest, installer fallback version, README examples, and changelog together. After validation, merge into main and push the matching `v<version>` tag. The tag triggers package builds and Release asset uploads; a successful push does not mean those jobs have finished.
