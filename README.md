# Privacy Metadata Cleaner

Privacy Metadata Cleaner 是一款专为 macOS（Mac Catalyst）打造的 .NET MAUI 应用，用于批量清理 Word、PDF 与图像文件中的隐私元数据，并为图像提供可调节的压缩能力。应用会将清理后的文件存放在原始目录下的 `Cleaned` 子文件夹，避免覆盖原文件。

## 主要功能
- 拖放或拖入文件夹以批量导入 `.docx`、`.pdf`、`.jpg`、`.jpeg` 与 `.png` 文件。
- 基于 GroupDocs.Metadata 清除作者、标题、创建时间、GPS、相机参数等敏感元数据。
- 基于 SixLabors.ImageSharp 对图像执行去元数据与压缩处理，支持 30%、50%、70%、90% 等质量档位。
- 处理过程中显示进度条，并在列表中列出每个文件的处理结果与大小对比。
- 自动在源文件夹下创建 `Cleaned` 目录存放清理后的输出。

## 环境要求
- macOS 13 或更高版本（用于运行 Mac Catalyst 应用）。
- .NET 8 SDK 及以上版本。
- GroupDocs.Metadata 与 SixLabors.ImageSharp NuGet 包（在 `dotnet restore` 时自动获取）。

## 开发与运行
1. 在项目根目录执行 `dotnet workload install maui`（如尚未安装 MAUI 工作负载）。
2. 运行 `dotnet restore` 安装依赖。
3. 使用以下命令以调试模式运行：
   ```bash
   dotnet build PrivacyMetadataCleaner/PrivacyMetadataCleaner.csproj -t:Run -f net8.0-maccatalyst
   ```
4. 在运行中的应用中，将文件或文件夹拖放至界面中央区域即可开始清理。列表将展示每个文件的成功或失败状态以及体积变化。

## 使用说明
- 拖放前请确保文件未被其他程序占用。
- 压缩质量设置仅对图像文件生效，文档类文件会直接使用 GroupDocs.Metadata 清理元数据。
- 如果某些文件因权限或格式受限而无法处理，界面会给出失败原因，可根据提示手动处理。
- 清理结果位于原目录的 `Cleaned` 文件夹中，您可以在 Finder 中快速比对清理前后的差异。

## 安装与打包
1. 执行常规构建：
   ```bash
   dotnet publish PrivacyMetadataCleaner/PrivacyMetadataCleaner.csproj -f net8.0-maccatalyst -c Release
   ```
2. 使用 `scripts/create_dmg.sh` 将生成的 `.app` 打包为 `.dmg` 安装镜像（需在 macOS 上运行脚本，并准备签名证书）。
3. 按提示完成签名与压缩后，即可将 `.dmg` 分发给用户，用户只需将应用拖入 `/Applications` 即可完成安装。

有关详细的安装步骤与 DMG 自定义流程，请参阅 [`docs/打包与安装指南.md`](docs/%E6%89%93%E5%8C%85%E4%B8%8E%E5%AE%89%E8%A3%85%E6%8C%87%E5%8D%97.md)。

## 许可证
本项目示例代码以 MIT 许可证发布。请确保在商业环境中遵守 GroupDocs.Metadata 等第三方库的许可条款。
