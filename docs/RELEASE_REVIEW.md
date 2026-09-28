# 发布前审查 / Release readiness review

日期：2026-09-28。范围：相对 `468b36f`（v1.10.0）的本地未提交改动及新增文件。本轮保留原有改动，版本维持 `1.10.0`，新增说明仍归入 Unreleased。

## 新增功能核对

| 改动 | 核对重点 |
| --- | --- |
| 后台工作区保存 | 快照隔离、请求合并、单写入者、失败后重试、退出等待、旧队列与主动清空的区别 |
| 进度与执行 | 100 ms 合并、终态顺序、增量汇总、执行前冻结参数、历史使用冻结参数 |
| 大队列与媒体缓存 | 范围更新、任务身份与选择保持、搜索防抖、失败筛选、512 条 LRU 与元数据失效 |
| 编辑模块拆分 | 独立文件与共享文件夹参数、v1 JSON 兼容、预设、恢复、应用到全部、语言切换 |
| 布局和结果区 | 1180 DIP 双列、窄窗口单列、常显 CLI、主题与双语、结果入口和历史 |

## 发现并修复

1. **恢复可能覆盖已保存参数（P1）**：工作区中的 `IsSelected` 已恢复，但编辑器尚未加载；选择任务前的保存会用初始值覆盖旧参数。恢复期间禁止反向保存，并优先恢复仍存在的原选中任务。检查使用两个不同质量参数的文件和一个缺失文件，确认选中项与非选中项均保留参数。
2. **共享文件夹恢复丢失比例（P1）**：图片质量模式的 `ImageTargetSizeKB` 为零，将其作为编辑值恢复会丢失原来的百分比，且控件未配置成 1–100。现在保存共享编辑状态；兼容旧文档时从 `RelativeTargetPercentage` 恢复比例。覆盖旧文档和再次保存、恢复的往返过程。
3. **图片质量目标显示错误（P2）**：图片不限制大小且不使用 CRF 时，恢复逻辑误选“码率”。现在恢复队列、预设和 CRF 联动均选择质量目标。
4. **立即应用到全部复制旧滑块值（P2）**：100 ms 合并尚未应用时执行批量复制会漏掉最后一次拖动。复制前刷新待应用值。
5. **语言切换重置状态和参数（P2）**：重建本地化选项对象触发了正常参数变更事件，可能重新应用预设、重算质量并把完成状态改成待处理。重建期间抑制参数联动，随后刷新文案和预览。
6. **缺失文件状态不一致（P2）**：恢复后的缺失项使用灰色；批量复制参数还会清除失败状态。现在使用失败色，并在参数修改/复制时保留已排除项的失败状态。

前五项均通过扩展真实 Avalonia runner 复现失败后修复；第六项由状态赋值审查发现并补充界面运行断言。原有 62 项单元/媒体测试未覆盖这些完整操作顺序，不能仅凭原回归通过认定无缺漏。

## 本轮验证

- 使用 WSL 调用本机 Windows .NET SDK 10.0.400，项目目标仍为 .NET 8；执行前已重新 restore。
- 修复后 `FFGUITOOL_REQUIRE_FFMPEG=1`：Debug、Release 各 **62 通过、0 失败、0 跳过**。
- `scripts/release-check.ps1` 通过，验证的是现有 **1.10.0** 元数据一致性，不代表下一版本已准备好。
- 针对恢复的真实 Avalonia 检查通过，包含参数、目标、立即复制滑块、语言切换及共享比例；最终完整界面复跑结果见下方记录。
- 普通 `git diff --check` 仍会报告工作区既有 CRLF/LF 差异；`git -c core.autocrlf=true diff --check` 通过。未批量改写行尾。
- 全部合成媒体、配置、日志、输出和截图位于忽略的 `TestResults` 内；未使用或删除真实用户媒体及配置。

最终完整界面复跑 **通过**，结果见本地 [metrics.json](../TestResults/release-review-final-20260928/metrics.json)。运行当前仓库 Release 构建的真实窗口，实际 `RenderScaling=1.25`：

- 760×560、960×800、1360×900，中英文 × 浅深主题；检查并查看窗口渲染截图。
- 千文件独立导入 158.85 ms、文件夹导入 79.72 ms；列表实际只创建 5–6 个可见容器，Reset 通知为 0。
- 10,000 项集合逆序 2.23 ms；513 文件缓存场景保留热点；11,000 条进度事件 121.74 ms，终态先于汇总完成。
- 32 张合成图片实际编码 1,294.23 ms；队列操作最大 UI 心跳间隔 73.43 ms。
- 扫描取消/重扫、独立/共享参数、搜索/排序/失败筛选、预设与应用配置隔离、退出保存、原选中项恢复、缺失文件、清空后重新读盘、语言切换及共享文件夹二次恢复断言均通过。

上述计时为单次样本，导入/筛选等包含 runner 的帧等待，不能作为跨机器性能承诺。恢复测试是重新创建 ViewModel 并读盘，不等于整个应用进程崩溃恢复测试。

截图：[英文宽窗口深色](../TestResults/release-review-final-20260928/en-US-dark-1360x900.png)、[中文小窗口浅色](../TestResults/release-review-final-20260928/zh-CN-light-760x560-settings.png)、[结果区](../TestResults/release-review-final-20260928/results-en-US-dark-760x560.png)、[视频编码器](../TestResults/release-review-final-20260928/video-codec-en-US-dark-760x560.png)。这些产物仅保留在本地忽略目录。

## 发布前剩余事项

- 确定下一版本号后，同步项目/程序集、manifest、安装器默认值、README 示例、CHANGELOG 和标签；初次审查未创建提交、标签、安装包或线上 Release；后续按用户要求在 dev 开发并同步分支，版本号与发布标签仍单独处理。
- 本轮实际桌面为 Windows、125% 缩放。macOS/Linux 桌面、100%/150%/200% 与跨屏缩放、真实 GPU 编码、各架构安装包安装/启动仍需对应环境验收；不能以 Windows 回归代替。
- 千文件测量使用合成小图片。大型媒体、真实万文件窗口、长时间内存稳定性、整个进程崩溃/断电恢复未在本轮验证。

## 优先增强落实（2026-09-28）

1. **已实现发布标签校验**：元数据脚本支持 `-Tag` 和 `RELEASE_TAG`，发布工作流显式传入标签，严格要求等于 `v<Version>`。新增 PowerShell 回归覆盖正确标签、空白、分支名、大小写、版本错配、环境变量及兼容入口。
2. **已接入恢复场景 CI**：CI 覆盖 dev/main，Windows Release 增加 `--recovery-only`，10 分钟超时，并核验退出码和指标文件。runner 改为有界等待实际初始化，仅生成两张恢复测试图片；失败时保存诊断截图，CI 保留指标和日志 14 天。常规 `dotnet test` 仍不包含这些桌面断言。
3. **更大范围重构后置**：继续拆分主 ViewModel、固定参数作用范围提示、工具配置与预览缩放，可在发布后逐步推进；本次发布前优先收敛状态正确性和平台验收。

优先增强本地验证：标签校验回归通过（正确标签和 9 个拒绝场景）；Debug/Release 各 62 项通过、无跳过；更新后的恢复 runner 通过（`TestResults/dev-ci-recovery-20260928/metrics.json`，Windows 125%）。CI YAML 解析与行尾归一化差异检查通过。远程 Actions 结果应按对应提交单独核验。

## English summary

The review found and fixed queue-restoration data loss, shared-folder ratio restoration, image-goal mismatches, stale slider values during apply-to-all, language-change side effects, and missing-input status inconsistencies. Both FFmpeg-required test configurations pass (62 each, no skips). Version metadata remains at 1.10.0; selecting and publishing the next version and additional platform/device verification are separate remaining steps.
