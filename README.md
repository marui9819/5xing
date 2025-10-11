# PrivacyMetadataCleaner 使用说明

本项目提供一个基于 .NET 8 的 Windows Forms 图形化工具，用于批量清理常见文档与图片文件中的隐私元数据。以下内容将帮助你在 Windows 环境中编译、运行和正确使用该工具。

## 环境要求

1. **操作系统**：Windows 10/11。
2. **.NET SDK**：请安装 [Microsoft .NET 8 SDK](https://dotnet.microsoft.com/)。
3. **IDE（可选）**：推荐使用 Visual Studio 2022（17.8 及以上）或 Visual Studio Code（配合 C# 扩展）。
4. **第三方依赖**：项目通过 NuGet 自动引用 `DocumentFormat.OpenXml`、`itext7`、`Magick.NET-Q8-AnyCPU`，无需手动下载。

## 获取源代码

```bash
git clone <仓库地址>
cd 5xing
```

> 如已获取压缩包，直接解压并进入 `5xing` 目录即可。

## 编译与运行

### 使用命令行

```bash
cd PrivacyMetadataCleaner
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

### 使用 Visual Studio

1. 打开 `PrivacyMetadataCleaner.sln`。
2. 确保解决方案的目标框架为 `.NET 8.0 (Windows)`。
3. 在“生成”菜单选择“生成解决方案”。
4. 按 `F5` 或点击“启动”运行应用。

> 若需发布单文件可执行程序，可在命令行执行：
>
> ```bash
> dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
> ```
>
> 生成的 `.exe` 位于 `PrivacyMetadataCleaner/bin/Release/net8.0-windows/win-x64/publish/`。

## 界面与操作流程

1. **选择文件**：点击“选择文件”按钮，可一次性添加多个受支持格式的文件（`*.docx`、`*.pdf`、`*.jpg`/`*.jpeg`、`*.png`、`*.tiff`/`*.tif`、`*.bmp`）。
2. **选择文件夹**：点击“选择文件夹”可递归扫描文件夹中所有受支持文件。
3. **任务列表**：待处理文件将显示在列表中，包含文件路径、处理状态和已清除的元数据字段。
4. **保存日志**：点击“保存日志”可将处理过程中产生的日志保存为文本文件。
5. **开始处理**：点击“开始处理”后，程序会在后台线程逐个处理文件，同时更新进度条与日志。
6. **结果查看**：处理完成后会弹出统计提示框，并在列表中显示“成功”“失败”“跳过”状态与清除详情。

## 元数据处理规则

- **Word (`.docx`)**：移除作者、公司、分类、自定义属性、创建时间、修改时间等常见属性。
- **PDF (`.pdf`)**：清理作者、标题、主题、关键字、自定义键值以及 XMP 元数据。
- **图片（JPEG/PNG/TIFF/BMP）**：删除 EXIF、IPTC、XMP 等个人信息，同时保留颜色 ICC 配置文件以避免色彩失真。
- 无法写入或不支持的文件会被跳过，并在日志中给出原因。

## 日志与统计

- 日志窗口实时显示处理进度、每个文件的处理结果以及遇到的错误。
- 所有日志内容可复制或通过“保存日志”导出。
- 批处理结束后会展示总数、成功、失败和跳过的数量，便于审查处理结果。

## 常见问题

| 问题 | 可能原因 | 解决方案 |
| --- | --- | --- |
| 无法编译 | 未安装 .NET 8 SDK 或 SDK 版本过旧 | 安装最新的 .NET 8 SDK 并重启 IDE |
| 运行时报错“缺少依赖” | NuGet 包未正确还原 | 执行 `dotnet restore` 或在 Visual Studio 中重新生成解决方案 |
| 图片失真 | 手动删除了 `ICC` 配置文件 | 程序默认保留 ICC 配置，若自定义脚本需注意保留颜色配置 |
| 日志为空 | 未点击“开始处理”或没有可处理的文件 | 确认任务列表中存在文件且已启动处理 |

## 反馈与贡献

欢迎提交 Issue 或 Pull Request 以改进功能与体验。贡献代码前请先执行 `dotnet format`（需要安装 `.NET 8`）并确保解决方案可以成功构建。

