# 开发与发布 / Development and releases

[中文 README](../README.zh-CN.md) · [English README](../README.md) · [开发约束 / Agent guide](../AGENTS.md)

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
git diff --check
```

CI 在 Windows/macOS/Linux 的 Debug、Release 配置中运行。真实硬件编码需另用目标设备验证。 / CI runs Debug and Release on Windows, macOS, and Linux. Hardware encoding requires separate device testing.

## 实际界面与千文件检查 / Desktop UI and 1,000-file checks

在有桌面的会话中运行，需要系统 FFmpeg： / Run in a desktop session with system FFmpeg:

```powershell
dotnet run --project scripts/UiChecks/UiChecks.csproj -p:OutputPath=bin/Debug/layout-check/
```

工具会打开真实 Avalonia 窗口，生成 1,000 个 64×64 PNG，检查独立/共享参数、预设与应用设置隔离、搜索防抖、排序、失败筛选、扫描取消及重扫。导出中英文/浅深主题的 760×560 截图、视频编码器布局截图和 `metrics.json`。

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

同步项目版本、程序集版本、`app.manifest`、安装器默认版本、中英文 README 示例及 CHANGELOG。通过测试和元数据检查后，将发布提交合入 main，再推送对应的 `v<version>` 标签。标签会触发 GitHub Actions 构建、打包并上传 Release 附件；推送成功不代表构建已完成。

Update the project/assembly versions, manifest, installer fallback version, README examples, and changelog together. After validation, merge into main and push the matching `v<version>` tag. The tag triggers package builds and Release asset uploads; a successful push does not mean those jobs have finished.
