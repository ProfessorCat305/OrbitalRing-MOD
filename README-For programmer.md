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

2. 打开本项目，将`DefaultPath.props.example`复制到`DefaultPath.props`。
3. 修改`DefaultPath.props`中的BepinExPath和DSPAssemblyPath，使其与您的实际路径一致，然后关闭/卸载项目。
4. 在R2中打开与上一步一致的Profile，在ConfigEditor中搜索`BepInEx.cfg`，修改Preloader-DumpAssemblies为true。
5. 通过R2启动一次游戏（需要启用星环），将会自动生成`BepInEx\DumpedAssemblies\DSPGAME\Assembly-CSharp.dll`。
6. 重新打开本项目，会自动在项目内生成`lib\Assembly-CSharp-publicized.dll`。至此，准备工作已经完成。现在项目不应有任何报错。

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

2. Open this project, copy `DefaultPath.props.example` to `DefaultPath.props`.
3. Modify the BepinExPath and DSPAssemblyPath in `DefaultPath.props` to match your actual paths, then
   close/uninstall the project.
4. Open a Profile in R2 that matches the previous step, search for `BepInEx.cfg` in the ConfigEditor, and change
   Preloader-DumpAssemblies to true.
5. Launch the game once through R2 (need to enable Orbital Ring), it will automatically generate
   `BepInEx\DumpedAssemblies\DSPGAME\Assembly-CSharp.dll`.
6. Re-open the project, it will automatically generate `lib\Assembly-CSharp-publicized.dll` in the project. So, the
   preparation work has been completed. The project should not report any errors now.

## 编译注意事项

游戏更新后，`DumpedAssemblies` 与 `lib/Assembly-CSharp-publicized.dll` 必须来自当前游戏版本，并包含本仓库 `preloader/Preloader.cs` 添加的字段。旧参考库可能缺少新的游戏接口；此时应重新准备参考库，不应为了通过编译而把当前源码改回旧接口。

只做编译检查时可使用 Windows MSBuild：

```powershell
msbuild ProjectOrbitalRing.csproj /t:ResolveReferences,PrepareResources,Compile /p:Configuration=Debug
```

若此前只调用过 `Compile`，应先把旧的 `obj/Debug/ProjectOrbitalRing.dll` 移到临时目录再执行，避免增量编译复用缺少嵌入资源的产物。最终 DLL 应包含 `ProjectOrbitalRing.data.strings.json`、`ProjectOrbitalRing.data.strings_mod.json` 和 `ProjectOrbitalRing.data.strings_languages.json` 三个词表资源。

该命令不执行项目的 `AfterBuild`。普通 `Build` 会把模组和资源复制到 `DefaultPath.props` 指定的 BepInEx Profile 与发布目录，执行前应确认这些路径。若在临时目录离线准备参考库，应在游戏 DLL 副本上应用本仓库的 `Preloader.Patch` 和 publicizer，不改动已安装游戏；可通过 `DSPAssemblyPath`、`ReferencePath` 和 `BepinExPath` 命令行属性指定所需引用，避免复用旧 Profile 的导出文件。

开始修改联机或译文前，阅读 [联机与多语言维护](docs/multiplayer-and-localization.md)，遵循其中的同步范围、词表增删和验证要求。
