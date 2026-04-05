using System;
using System.Collections.Generic;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic.AddVein
{
    internal static partial class ModifyPlanetTheme
    {
        internal static readonly Dictionary<int, ThemeData> ThemeDatas = new Dictionary<int, ThemeData>
        {
            //0 铁
            //1 铜
            //2 硅
            //3 钛
            //4 石
            //5 煤
            //6 油
            //7 可燃冰
            //8 金伯利
            //9 莫桑石
            //10 有机
            //11 光栅 12
            //12 刺笋
            //13 单极磁石
            //14 辉银矿 15
            //15 深层岩浆 16
            //16 铀 17
            //17 黄铁 18
            //18 冰 19
            {
                1, // 地中海
                new ThemeData(new[] { 1, 14 }, new[] { 5, 6 }, new[] { 0.5f, 0.3f }, new[] { 0.4f, 0.1f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 18 }, new[]
                {
                    1.0f, 1.0f, 0.3f, 0.15f
                })
            },
            {
                6, // 干旱荒漠
                new ThemeData(new[] { 0, 2, 5, }, new[] { 8, 14, 0, }, new[] { 0.7f, 0.5f, 0, }, new[] { 0.3f, 0.7f, 0,},
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 12, 15, 17, 18, 19 }, new[]
                {
                    0.0f, 0.3f, 0.3f, 0.15f,
                    0.0f, 0.2f, 0.4f, 0.4f,
                    0.0f, 0.15f, 0.88f, 0.1f,
                    0.0f, 0.2f, 0.75f, 0.3f,
                    0.0f, 0.1f, 0.75f, 0.4f, //
                })
            },
            {
                7, // 灰烬冻土
                new ThemeData(new[] { 1, 2, 5 }, new[] { 6, 8, 0, 4 }, new[] { 0.4f, 0.5f, 0, 0.3f }, new[] { 0.6f, 0.3f, 0, 0.1f },
                    new[] { 0 }, new[]
                {
                    1.0f, 0.5f, 0.7f, 0.5f,
                }, new[] { 15, 17, 18, 19 }, new[]
                {
                    0.0f, 0.3f, 0.6f, 0.2f,
                    1.0f, 0.4f, 0.8f, 0.2f,
                    1.0f, 0.3f, 0.83f, 0.2f, //
                    1.0f, 0.6f, 0.75f, 0.7f, //
                })
            },
            {
                8, // 海洋丛林
                new ThemeData(new[] { 0, 1, 2, 3, 4 }, new[] { 6, 6, 5, 6, 8 }, new[] { 0.3f, 0.5f, 0.2f, 0.3f, 0.6f }, new[] { 0.3f, 0.3f, 0.3f, 0.2f, 0.5f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 15, 17, 18, }, new[]
                {
                    0.0f, 0.2f, 0.5f, 0.3f, //
                    0.0f, 0.2f, 0.8f, 0.1f,
                    0.0f, 0.8f, 0.6f, 0.4f, //
                })
            },
            {
                9, // 熔岩
                new ThemeData(new[] { 0, 1, 2, 3, 5, 15 }, new[] { 7, 5, 4, 6, 0, 22 }, new[] { 0.6f, 0.5f, 0.6f, 0.9f, 0, 1.0f }, new[] { 0.6f, 0.6f, 0.8f, 0.9f, 0, 1.0f },
                    new[] { 0, 1, }, new[]
                {
                    0.0f, 0.5f, 0.8f, 0.3f,
                    0.0f, 0.8f, 0.6f, 0.7f,
                }, new[] { 15, 17, 18, }, new[]
                {
                    1.0f, 0.3f, 0.7f, 0.2f,
                    1.0f, 1.0f, 1.0f, 0.2f, //
                    1.0f, 0.8f, 0.5f, 1.0f, //
                })
            },
            {
                10, // 冰原冻土
                new ThemeData(new[] { 0, 1, 4, 5, 14 }, new[] { 7, 6, 4, 0, 4 }, new[] { 1f, 0.5f, 0.6f, 0, 0.4f }, new[] { 1f, 0.5f, 1f, 0, 0.3f },
                    new[] { 0 }, new[]
                {
                    1.0f, 1f, 0.9f, 0.3f,
                }, new[] { 9, 17, 18, 19 }, new[]
                {
                    0.0f, 0.2f, 0.3f, 0.1f, //
                    1.0f, 0.6f, 0.75f, 0.2f, //
                    1.0f, 0.6f, 0.83f, 0.2f, //
                    1.0f, 0.8f, 0.85f, 0.85f, //
                })
            },
            {
                11, // 贫瘠荒漠
                new ThemeData(new[] { 2, 3 }, new[] { 6, 4 }, new[] { 0.25f, 0.2f }, new[] { 0.4f, 0.7f },
                    new[] { 1 }, new[]
                {
                    0.0f, 0.0f, 0.0f, 0.0f,
                }, new[] { 18, 19}, new[]
                {
                    0.0f, 0.05f, 0.75f, 0.1f, //
                    0.0f, 0.05f, 0.6f, 0.3f, //
                })
            },
            {
                12, // 戈壁
                new ThemeData(new[] { 0, 3, 5, 14, 15 }, new[] { 7, 4, 0, 6, 12 }, new[] { 0.4f, 0.4f, 0, 0.6f, 1.0f }, new[] { 0.4f, 0.6f, 0, 0.6f, 0.9f },
                    new[] { 0 }, new[]
                {
                    0.0f, 0.8f, 0.6f, 0.6f,
                },  new[] { 17, 18, 19 }, new[]
                {
                    0.0f, 0.1f, 0.6f, 0.2f, //
                    0.0f, 0.1f, 0.75f, 0.5f, //
                    0.0f, 0.05f, 0.6f, 0.4f, //
                })
            },
            {
                13, // 火山灰
                new ThemeData(new[] { 2, 3, 5, 14, 15 }, new[] { 10, 5, 0, 8, 18 }, new[] { 0.6f, 0.5f, 0, 0.5f, 1.0f }, new[] { 0.6f, 0.6f, 0, 0.7f, 1.0f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 9, 17, 18}, new[]
                {
                    0.0f, 1.0f, 0.6f, 0.4f, //
                    0.0f, 1.0f, 1.0f, 0.2f, //
                    0.0f, 1.0f, 0.8f, 0.4f, //
                })
            },
            {
                14, // 红石
                new ThemeData(new[] { 0, 1, 2, 3, 14 }, new[] { 8, 5, 6, 4, 4 }, new[] { 0.75f, 0.2f, 0.1f, 0.2f, 0.5f }, new[] { 0.5f, 0.6f, 0.3f, 0.6f, 0.3f },
                    new[] { 0 }, new[]
                {
                    0.0f, 0.3f, 0.6f, 0.1f,
                },  new[] { 17, 18, }, new[]
                {
                    0.0f, 0.25f, 0.8f, 0.1f, //
                    0.0f, 0.35f, 0.8f, 0.3f, //
                })
            },
            {
                15, // 草原
                new ThemeData(new[] { 1, 2, 3, 4, 14 }, new[] { 6, 4, 2, 4, 5 }, new[] { 0.15f, 0.3f, 0.6f, 0.5f, 0.3f }, new[] { 0.5f, 0.6f, 0.5f, 0.7f, 0.3f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18, }, new[]
                {
                    0.0f, 0.25f, 0.75f, 0.5f, //
                    0.0f, 0.2f, 0.75f, 0.1f, //
                })
            },
            {
                16, // 水世界
                new ThemeData(new[] { 0, 1, 2, 3, 4 }, new[] { 6, 5, 2, 3, 5 }, new[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f }, new[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 8, }, new[]
                {
                    0.0f, 1.0f, 0.85f, 0.8f, //
                })
            },
            {
                17, // 黑石盐滩
                new ThemeData(new[] { 3, 5}, new[] { 2, 3 }, new[] { 0.2f, 0.1f }, new[] { 0.3f, 0.3f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 15, 17, }, new[]
                {
                    0.0f, 0.2f, 0.75f, 0.2f, //
                    0.0f, 0.2f, 0.75f, 0.2f, //
                })
            },
            {
                18, // 樱林海
                new ThemeData(new[] { 1, 2, 3, 4, 14 }, new[] { 6, 3, 5, 5, 3 }, new[] { 0.1f, 0.3f, 0.5f, 0.8f, 0.3f }, new[] { 0.6f, 0.6f, 0.6f, 0.5f, 0.2f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18 }, new[]
                {
                    0.0f, 0.1f, 0.6f, 0.2f, //
                    0.0f, 0.3f, 0.75f, 0.2f, //
                })
            },
            {
                19, // 飓风石林
                new ThemeData(new[] { 5, 14 }, new[] { 5, 3 }, new[] { 0.1f, 0.3f }, new[] { 0.3f, 0.1f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18, 19 }, new[]
                {
                    0.0f, 0.3f, 0.85f, 0.1f, //
                    0.0f, 0.4f, 0.93f, 0.2f, //
                    0.0f, 0.2f, 0.8f, 1.0f, //
                })
            },
            {
                20, // 猩红冰湖
                new ThemeData(new[] { 0, 1, 3 }, new[] { 11, 5, 8 }, new[] { 1f, 0.8f, 0.3f }, new[] { 1f, 1f, 1f },
                    new[] { 0, 1 }, new[]
                {
                    1.0f, 1f, 0.6f, 0.7f,
                    0.0f, 0.0f, 0.0f, 0.0f,
                },  new[] { 15, 17, 18, 19 }, new[]
                {
                    1.0f, 0.2f, 0.5f, 0.2f,
                    1.0f, 0.5f, 0.8f, 0.3f, //
                    1.0f, 0.7f, 0.8f, 0.4f, //
                    1.0f, 0.6f, 1.0f, 1.0f, //
                })
            },
            {
                22, // 热带草原
                new ThemeData(new[] { 1, 2, 3, 4, 14 }, new[] { 5, 4, 3, 4, 4 }, new[] { 0.2f, 0.5f, 0.6f, 0.7f, 0.4f }, new[] { 0.6f, 0.8f, 0.7f, 1.0f, 0.2f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18, }, new[]
                {
                    0.0f, 0.2f, 0.6f, 0.2f, //
                    0.0f, 0.25f, 0.75f, 0.2f, //
                })
            },
            {
                23, // 橙晶荒漠
                new ThemeData(new[] { 0, 1, 2, 3, 4, 5, 14 }, new[] { 4, 7, 6, 4, 4, 0, 3 }, new[] { 0.4f, 0.6f, 0.6f, 0.7f, 0.5f, 0, 0.5f }, new[] { 0.4f, 0.8f, 0.6f, 0.5f, 0.6f, 0, 0.5f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18, 19 }, new[]
                {
                    0.0f, 0.8f, 0.83f, 1.0f, //
                    0.0f, 1.0f, 1.0f, 1.0f, //
                    0.0f, 0.4f, 0.93f, 0.8f, //
                })
            },
            {
                24, // 极寒冻土
                new ThemeData(new[] { 1, 4, 5, }, new[] { 6, 4, 0, }, new[] { 0.4f, 0.4f, 0, }, new[] { 0.8f, 0.4f, 0, },
                    new[] { 0, 1 }, new[]

                {
                    1.0f, 1.0f, 0.8f, 1.0f,
                    0.0f, 0.0f, 0.0f, 0.0f,
                },  new[] { 17, 18, 19 }, new[]
                {
                    1.0f, 0.2f, 0.75f, 0.2f, //
                    1.0f, 0.1f, 0.6f, 0.2f, //
                    1.0f, 0.8f, 1.0f, 1.0f, //
                })
            },
            {
                25, // 潘多拉沼泽
                new ThemeData(new[] { 0, 2, 3, 14 }, new[] { 5, 4, 3, 4 }, new[] { 0.3f, 0.1f, 0.3f, 0.3f }, new[] { 0.7f, 0.8f, 1f, 0.4f },
                    Array.Empty<int>(), Array.Empty<float>(), new[] { 17, 18, 19 }, new[]
                {
                    0.0f, 0.2f, 0.85f, 0.1f, //
                    0.0f, 0.3f, 0.8f, 0.1f, //
                    0.0f, 1.0f, 1.0f, 1.0f, //
                })
            },
        };

        public struct ThemeData
        {
            public readonly int[] VeinId;
            public readonly int[] VeinSpot;
            public readonly float[] VeinCount;
            public readonly float[] VeinOpacity;
            public readonly int[] VanillaRareVeins;
            public readonly float[] VanillaRareSettings;
            public readonly int[] RareVeins;
            // 第一参数初始星该矿脉出现概率，第二参数其他星该矿脉出现概率，随机0到1小于第三参数则矿点加1，最多12个矿点，第四参数，储量和误差都是1f - Mathf.Pow(1f - 四参数, p);p由恒星决定，规律不明
            public readonly float[] RareSettings;

            public ThemeData(int[] veinId, int[] veinSpot, float[] veinCount, float[] veinOpacity, int[] vanillaRareVeins, float[] vanillaRareSettings, int[] rareVeins, float[] rareSettings)
            {
                VeinId = veinId;
                VeinSpot = veinSpot;
                VeinCount = veinCount;
                VeinOpacity = veinOpacity;
                VanillaRareVeins = vanillaRareVeins;
                VanillaRareSettings = vanillaRareSettings;
                RareVeins = rareVeins;
                RareSettings = rareSettings;
            }
        }
    }
}
