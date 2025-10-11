# Privacy Metadata Cleaner

该仓库包含一个基于 WPF（.NET 6）的桌面工具，用于批量清理 Word、PDF 以及图片文件的隐私元数据。

## 如何打包为可在无 .NET SDK 环境运行的 .exe

如果需要在未安装 .NET SDK（甚至未安装 .NET Runtime）的 Windows 机器上运行本程序，可以借助 `dotnet publish` 生成自包含（Self-contained）的可执行文件。以下步骤假设您已在打包机器上安装了 .NET 6 SDK 或更高版本。

1. **还原依赖并以 Release 模式构建项目**

   ```powershell
   dotnet restore PrivacyMetadataCleaner/PrivacyMetadataCleaner.csproj
   dotnet build PrivacyMetadataCleaner/PrivacyMetadataCleaner.csproj -c Release
   ```

2. **发布自包含的单文件可执行程序**

   以 64 位 Windows 为例，执行以下命令：

   ```powershell
   dotnet publish PrivacyMetadataCleaner/PrivacyMetadataCleaner.csproj \
       -c Release \
       -r win-x64 \
       --self-contained true \
       -p:PublishSingleFile=true \
       -p:IncludeNativeLibrariesForSelfExtract=true \
       -p:DebugType=None \
       -p:DebugSymbols=false
   ```

   关键参数说明：

   - `-r win-x64`：指定目标运行时标识符（RID），此处为 64 位 Windows。若需 32 位可改为 `win-x86`，ARM 设备可使用 `win-arm64`。
   - `--self-contained true`：将 .NET 运行时与应用一起打包，目标机器无需安装额外运行时。
   - `-p:PublishSingleFile=true`：将输出整合为单个 `.exe` 文件，便于分发。
   - `-p:IncludeNativeLibrariesForSelfExtract=true`：确保像 iText、Magick.NET 等原生依赖在启动时能够正确释放。
   - `-p:DebugType=None` 与 `-p:DebugSymbols=false`：移除调试信息，减小包体积。

3. **获取发布产物并分发**

   发布完成后，可在 `PrivacyMetadataCleaner/bin/Release/net6.0-windows/win-x64/publish/` 目录下找到生成的文件。其中 `PrivacyMetadataCleaner.exe` 即为可直接分发的可执行文件。您可以选择：

   - 直接复制整个 `publish` 目录到目标机器；
   - 或者将该目录打包为 ZIP 压缩包后再分发。

4. **在干净环境验证**

   将 `publish` 目录拷贝到一台未安装 .NET SDK 的 Windows 电脑上，双击运行 `PrivacyMetadataCleaner.exe`，确认应用可以正常启动并执行清理操作。

### 可选优化与常见问题

- 若希望进一步减小体积，可尝试添加 `-p:PublishTrimmed=true` 开启裁剪；但由于本项目依赖反射较多的第三方库，开启裁剪后需充分测试。
- 如果不想生成单文件形式，可去掉 `-p:PublishSingleFile=true`，保留默认的文件夹结构，这样可以降低应用启动时的解压开销。
- 如果需要一次发布多个平台，可以使用 `-r win-x86`、`-r win-x64` 等参数分别发布，每个平台都会生成独立的 `publish` 目录。
