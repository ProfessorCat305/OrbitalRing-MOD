![img.png](https://assets.awbugl.top/dsp-genesisbook/icon.png)

构建星环，探索宇宙。

Constructing the Orbital Ring, Exploring the Universe.

## 意见和反馈 | Feedback and suggestions

如果您对本Mod有宝贵的意见或建议，
请加QQ群（如下）向管理员反馈，欢迎加群交流。

- 星环MOD内测群 1035445959
- 群问题答案：有机晶体

If you have any problems, please open a issue via [github](https://github.com/ProfessorCat305/Cosmogenesis-MOD).

## 如何开始

1. 在开始之前，请务必确认您对以下内容以及它们的用法都有一定了解：

* [C#语言](https://www.runoob.com/csharp/csharp-tutorial.html)
* [Harmony](https://harmony.pardeike.net/articles/intro.html)，修改游戏运行逻辑
* [BepInEx](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/BepInEx/)
  ，5.4.18无法正确处理Harmony的部分Transpiler，由于创世大量使用Transpiler，强烈建议使用5.4.17
* [LDBTool](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/LDBTool/)，向戴森球计划添加物品、配方、科技等
* [CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/)，向戴森球计划添加物品、配方、科技等

2. 安装 Windows .NET SDK，项目的目标框架仍为 .NET Framework 4.7.2。
3. 将 `DefaultPath.props.example` 复制为 `DefaultPath.props`，填写 `ProfileDir` 和 `DSPGameDir`。共享配置会推导 BepInEx 和游戏程序集目录；已有的 `BepinExPath`、`DSPAssemblyPath` 显式配置仍然优先。
4. 在对应的 R2 Profile 中启用星环，并把 `BepInEx.cfg` 的 `Preloader-DumpAssemblies` 设为 `true`。
5. 通过该 Profile 启动游戏后，会生成带有星环 Preloader 字段的 `BepInEx\DumpedAssemblies\DSPGAME\Assembly-CSharp.dll`。后续构建会按需公开化到 `lib\Assembly-CSharp-publicized.dll`。
6. 按下面的命令还原和构建。新源码由 SDK 自动收集，无需逐个加入项目文件。

## How to start

1. Before you begin, make sure you have some understanding of the following and how they are used:

* [C# Language](https://www.runoob.com/csharp/csharp-tutorial.html)
* [Harmony](https://harmony.pardeike.net/articles/intro.html), Modify the game's runtime logic
* [BepInEx](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/BepInEx/), 5.4.18 can't handle some of Harmony's
  Transpiler correctly, so 5.4.17 is highly recommended due to lots of Transpilers in this project.
* [LDBTool](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/LDBTool/), Adds items, recipes, technologies, etc.
  to the Dyson Sphere Program.
* [CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/), Add items, recipes, technologies,
  etc. to the Dyson Sphere Program.

2. Install the Windows .NET SDK. The projects still target .NET Framework 4.7.2.
3. Copy `DefaultPath.props.example` to `DefaultPath.props` and set `ProfileDir` and `DSPGameDir`. Shared settings derive the BepInEx and game assembly paths; explicit `BepinExPath` and `DSPAssemblyPath` values still take precedence.
4. Enable Orbital Ring in the matching R2 profile and set `Preloader-DumpAssemblies` to `true` in `BepInEx.cfg`.
5. Starting the game through that profile produces `BepInEx\DumpedAssemblies\DSPGAME\Assembly-CSharp.dll` with the fields added by the Orbital Ring preloader. Builds publicize it to `lib\Assembly-CSharp-publicized.dll` when needed.
6. Restore and build with the commands below. The SDK includes new source files automatically.

## 编译注意事项

主项目、Preloader 和 Packer 都使用 `Microsoft.NET.Sdk`，公共设置在 `Directory.Build.props`。主项目排除了两个子项目的源码；Packer 保留对 Preloader 的构建依赖，打包时先生成 patcher。

.NET Framework 4.7.2、C# 7.3、现有程序集版本来源和 Debug/Release 符号格式保持原样。`GenerateAssemblyInfo=false` 避免 SDK 重复生成版本信息；`AppendTargetFrameworkToOutputPath=false` 保持 `bin/Debug`、`bin/Release` 层级，供 Packer 的相对路径使用。

在 Windows 终端中从仓库根目录执行：

```powershell
dotnet restore ProjectOrbitalRing.sln
dotnet build ProjectOrbitalRing.sln --no-restore -c Debug -p:DeployOnBuild=false
dotnet build ProjectOrbitalRing.sln --no-restore -c Release -p:DeployOnBuild=false
```

`DeployOnBuild=false` 只生成各项目的编译产物，不复制到游戏或发布目录。不指定该参数时，`DeployMod` 和 `DeployPreloader` 会在构建完成后复制插件、patcher、资源与发布说明，发布目录默认为仓库下的 `release`。构建前应确认本地 Profile 和发布路径。构建 Packer 不会自动运行打包程序。

游戏更新后，`DumpedAssemblies` 与公开化参考库必须来自当前游戏版本，并包含本仓库 `preloader/Preloader.cs` 添加的字段。可通过 `-p:GameAssemblyPath="完整 DLL 路径"` 使用已准备好的参考库，通过 `DumpedAssembliesPath` 指定待公开化的输入。旧参考库缺少新游戏接口时，应更新参考库，不应为了编译而把当前源码改回旧接口。

主项目的 `System` 和 `mscorlib` 必须引用 `DSPAssemblyPath` 中的游戏版本；游戏提供的 `Array.Fill`、`Dictionary.TryAdd` 等 API 超出了标准 net472 参考库的范围，这两项不能作为普通的冗余系统引用删除。其他系统引用由 SDK 或目标框架提供。

使用完整 `dotnet build` 会自动准备和嵌入资源，不再需要手动串联 `ResolveReferences`、`PrepareResources`、`Compile`。最终主 DLL 应包含唯一的 `ProjectOrbitalRing.data.strings.json` 词表和轨道模型资源，新增资源时应保持运行代码使用的完整名称。

开始修改联机或译文前，阅读 [联机与多语言维护](docs/multiplayer-and-localization.md)，遵循其中的同步范围、词表增删和验证要求。
