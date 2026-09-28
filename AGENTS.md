# FFGUITool 开发指引

适用于整个仓库；交流和交付默认使用中文。

## 工作原则

- 开始先看 `git status --short` 和相关 diff，保留用户及前序代理的未提交改动；不要重置或批量清理工作区。
- 后续开发先切换到 `dev` 分支；如果它落后于主线，先安全同步基准并保留未提交改动，再开始修改。验证通过后按任务要求同步主线与远程，最终回到 `dev`。
- 按当前任务阅读实现和测试，以当前代码和重新验证的结果为准；不要顺带实施无关待办。
- 修改关键行为时同步维护本文件；功能说明维护在中英文 README 和 CHANGELOG，未要求发布时使用 Unreleased，不自行升级版本。
- 交付区分已实现、实际验证和未验证事项；编译通过不代表功能或界面验证完成。

## 项目与入口

.NET 8 / Avalonia 11.0.10 / CommunityToolkit.Mvvm / MSTest。解决方案为 `FFGUIToolAvalonia.sln`，主项目 `FFGUITool`，测试项目 `FFGUITool.Tests`。依赖 FFmpeg，建议提供 ffprobe；ExifTool 可选。不得硬编码本机路径。

以下路径相对于 `FFGUITool/`：

| 职责 | 文件 |
| --- | --- |
| 执行、取消、输出预留、目标大小修正 | `Services/ProcessingExecutor.cs`、`EncodingPolicy.cs` |
| 命令、格式、命名与兼容性 | `Services/CommandBuilder.cs`、`CommandArguments.cs`、`Models/FFmpegCommand.cs` |
| 进程生命周期与安全提交 | `Services/ProcessRunner.cs`、`OutputTransaction.cs` |
| 探测、缓存、文件发现 | `Services/MediaProbe.cs`、`VideoAnalyzer.cs`、`MediaInputService.cs`、`MediaFileSupport.cs`、`MediaImportReport.cs`、`PathIdentity.cs` |
| 任务、参数与工作区 | `Models/ProcessingTask.cs`、`ProcessingWorkspace.cs`、`CompressionSettings.cs`、`ProcessingExecution.cs` |
| 持久化、试压与进度 | `Services/WorkspaceStore.cs`、`PreviewService.cs`、`UiProcessingProgress.cs`、`ProcessingResultFormatter.cs` |
| 主界面、队列与辅助面板 | `Views/MainWindow.axaml`、`QueuePanel.axaml`、`WorkspaceTools.axaml`、`AppPreferencesWindow.axaml`、`PreviewWindow.cs` |
| 启停、主题与文案 | `App.axaml(.cs)`、`Views/MainWindow.axaml.cs`、`Converters/StringToColorConverter.cs`、`Services/LocalizationService.cs`、`ImprovementResources.cs` |

`ViewModels/MainWindowViewModel` 已按 partial 拆分：主文件负责协调，`.Inputs` 导入，`.TaskEditor` 任务切换，`.Parameters` 参数联动，`.Estimates` 估算，`.EncoderOptions` 编码器，`.Processing` 执行，`.Workspace` 持久化/预设/试压，`.AppSettings` 应用设置。新增代码放入对应模块。
组合模块：`ParameterEditorViewModel` 持有编辑值，`TaskEditorState` 保存显示状态（保留 v1 JSON 字段兼容）；`QueuePanelViewModel` 管理筛选、搜索防抖和列表选择；`OutputSettingsViewModel` 管理输出选项。主 ViewModel 的转发属性供现有协调代码使用，禁止再引入第二份字段。`WorkspacePersistence` 负责后台单写入者，`BatchQueueSummary` 和 `ProcessingProgressTotals` 分别负责集合事实缓存与增量进度。媒体相关校验、估算、预设/历史和部分参数控件仍由主 ViewModel 协调，尚未完成全部职责迁移。
测试入口：`FFGUITool.Tests/Program.cs`、`ReliabilityTests.cs`、`QueueAndImportTests.cs`；实际窗口检查：`scripts/UiChecks`（运行方法见 `docs/DEVELOPMENT.md`）。

## 必须保留的约束

### 输出与编码

- 编码写入目标目录内的唯一临时文件，校验后提交；失败或取消保留旧输出。预留整批输出路径，即使选择覆盖也不能覆盖队列输入。
- 编码和探测使用 `ProcessRunner` + `ProcessStartInfo.ArgumentList`，同时读取 stdout/stderr；取消或超时须终止进程树并等待退出，再清理临时文件。
- 修改参数转义须验证空参数、空格、中文、引号及反斜杠往返。执行开始冻结参数，UI 后续变化不能影响已启动任务。
- 目标码率使用裁剪后时长、实际音频预算、十进制 kb/s 和封装余量；修正次数有界，未达目标必须返回警告并区别于完全成功。
- 两遍编码仅用于兼容的软件 libx264/libvpx-vp9 码率模式；硬件质量参数分别处理，保留真实短编码探测及自动模式的软件回退提示。
- WebM/GIF 不得被 H.264 硬件选择覆盖；提前校验音频复制与封装、流复制与缩放/帧率等冲突。
- 图片编码以最终输出格式为准；PNG 压缩级别与有损质量分开。目标大小尝试有界，只有 `AllowImageResize` 启用时才能缩小尺寸。
- 命名支持 `{name}`、`{label}`，处理非法字符并阻止路径逃逸；试压样本不能宣称准确预测完整文件体积。

### 队列与状态

- 独立文件各自保存参数，文件夹任务共享参数。新增设置须贯通克隆、切换、应用到全部、预设、恢复和持久化。恢复期间禁止把尚未恢复的编辑器写回任务；保留有效的原选中项。共享任务保存编辑状态，旧工作区从执行策略恢复百分比及 1–100 控件范围。
- 文件夹扫描与信息收集在后台进行，支持取消并分批更新 UI；跳过重解析点、临时输出和已记录生成文件。
- 媒体缓存按路径、长度和修改时间失效，使用 LRU 逐条淘汰，上限 512 条；视频串行，图片并发限制 1–4。
- 不超过 5 个独立文件使用标签，大批量和文件夹使用虚拟化列表；搜索防抖 200 ms，`QueueViewUpdater` 小改动使用增量更新；大幅重排通过 `QueueTaskCollection` 发出一次范围 Replace，不发 Reset，保留任务对象、状态和无变化时的列表容器。列表更新期间的临时选择事件不能触发参数恢复。扫描分批刷新，失败筛选须响应任务状态变化。
- 滑块更新合并；执行、保存预设、应用到全部、切换任务、切换语言和试压前刷新待应用值。保持 `SelectedGoal`、`UseCrf`、`LimitFileSize` 一致，用 `_syncingGoal` 防止递归。
- `workspace.json` 位于 `AppConfigService.AppDataPath`。UI 线程捕获脱离事件订阅、集合和可变设置的快照；后台单写入者序列化并用临时文件提交，合并尚未开始的旧请求。退出等待导入/执行收尾及最后保存，保存失败留在窗口；历史最多 100 条。恢复是重新执行未完成任务，不是断点续传；缺失源文件显示失败且排除执行。
- 保留 `_workspaceActivated` 对未恢复旧队列和已清空当前队列的区分，防止启动保存抹掉旧队列或清空后任务复活。
- `UiProcessingProgress` 每 100 ms 合并运行进度；终态同步应用并拒绝同任务的迟到运行事件，在汇总和保存前完成。执行前在 UI 线程冻结任务，后台执行事件映射回原任务。

### 界面与用户数据

- 保持编译绑定及明确的数据类型；跨父级绑定的例外限定在局部模板。新增文案提供中英文，状态色使用主题资源。语言切换重建选项时不得重新应用预设、改变参数或重置任务状态；图片不限制大小时恢复为质量目标。批量应用参数保留已排除的缺失文件失败状态。
- 应用设置收纳托盘行为和图片并发数；输出区收纳目录、命名和子目录结构。参数区标明当前文件/共享文件夹作用范围，导入显示数量与跳过原因，中英文切换须同步刷新。
- 宽度达到 1180 DIP 时任务区独立滚动，较窄窗口恢复单列；阈值变更须检查中英文和实际缩放。共享样式位于 `Views/WorkspaceStyles.axaml`；队列、目标和输出子视图绑定对应组合模块。
- CLI Preview 保持单个常显面板及复制按钮，长命令区域限高滚动，不再嵌套同名折叠标题。
- 保留 Ctrl+O 导入、Ctrl+Enter 执行、Escape 取消；退出等待取消收尾。默认关闭窗口退出，托盘行为须由用户设置启用。
- 界面测试启动当前仓库构建产物；使用绝对路径环境变量 `FFGUITOOL_APP_DATA` 隔离配置、工具安装目录与日志；不删除真实媒体、输出、预设、队列或整个配置目录，只清理确认由测试创建的数据。
- 不提交临时媒体、配置、构建产物或日志。用户要求停止界面操作时立即停止。

## 验证

按改动范围运行检查；首次或依赖变化后先 `dotnet restore FFGUIToolAvalonia.sln`。界面改动至少编译，并明确说明是否实际查看窗口。

完整回归在 Debug、Release 下分别运行（PowerShell）：

```powershell
$previousFfmpegRequirement = $env:FFGUITOOL_REQUIRE_FFMPEG
try {
    $env:FFGUITOOL_REQUIRE_FFMPEG = '1'
    foreach ($configuration in @('Debug', 'Release')) {
        dotnet test FFGUIToolAvalonia.sln --configuration $configuration --no-restore --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "$configuration tests failed." }
    }
} finally {
    $env:FFGUITOOL_REQUIRE_FFMPEG = $previousFfmpegRequirement
}
./scripts/release-check.ps1
./scripts/test-release-check.ps1
git diff --check
```

- 发布工作流必须显式传入 `-Tag $env:RELEASE_TAG`，要求标签严格等于 `v<Version>`；本地未提供标签时只检查文件元数据。发布须提供 `docs/releases/v<Version>.md` 中英文说明，上传阶段检出同一标签并将说明作为 Release 正文，缺少说明或附件须失败。Windows Release CI 还须执行 `UiChecks --recovery-only`，检查非零退出码与 `metrics.json`，保留失败诊断，不将超时或缺少 FFmpeg 视为跳过。
- `dotnet test` 包含构建；仅编译可用 `dotnet build FFGUIToolAvalonia.sln --configuration Debug --no-restore`，Release 同理。
- FFmpeg 测试跳过不能算完整回归通过；区分依赖、权限和代码问题，不降低测试要求。CI 配置存在不代表已经运行成功。
- 回归重点：旧输出保护、取消清理、失败继续、输出冲突、参数快照、两遍编码、图片超限警告、流复制、缓存、试压清理和路径转义。
- 界面重点：扫描取消、独立/共享参数、清空后重启、旧队列恢复、缺失文件、预设目标一致性、进度终态、浅深主题、中英文及小窗口。千文件性能须实际测量，硬件编码须用真实设备验证。
- 当前混合图片/视频导入按模式筛选；不要宣称支持混合类型统一队列。上述验证项是检查清单，不是已通过记录。
