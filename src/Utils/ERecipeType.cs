namespace ProjectOrbitalRing.Utils
{
    public enum ERecipeType
    {
        None = 0,
        Smelt = 1,
        Chemical = 2,
        Refine = 3,
        Assemble = 4,
        Particle = 5,
        Exchange = 6,
        PhotonStore = 7,
        Fractionate = 8,
        太空船坞 = 9,
        粒子打印 = 10, // 4 + 10
        等离子熔炼 = 11, // 1 + 11
        物质重组 = 12, // 4 + 10 + 12
        生物化工 = 14,
        Research = 15,
        高分子化工 = 16,
        所有化工 = 17, // 2 + 3 + 16 
        黑盒 = 18, // 4 + 9
        所有熔炉 = 19, // 1 + 11
    }
}
