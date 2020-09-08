using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static class used for stat calculation and results of in-game interactions. 
/// Helper Class where all of the mathematics (or most of it) are encapsulated
/// </summary>
public static class StatCalculator
{
    // ---------------------------------------------------------------------------------------
    /*                                   STATIC MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    /// This is a list of the thresholds for the agility levels that translate into different tick speeds
    static int[] tickSpeedChart;    

    /// This is the list that directly indicates the tick speeds corresponding to each agility level
    static int[] tickSpeedTranslator;

    /// <summary>
    /// Constructor for the static class, providing fixed values for the attributes above
    /// </summary>
    static StatCalculator() {
        tickSpeedChart = new int[]{ 256, 170, 98, 62, 44, 35, 29, 23, 19, 17, 15, 12, 10, 7, 5, 4, 3, 2, 1, 0 };
        tickSpeedTranslator = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 20, 22, 24, 26, 28 };
    }

    // ---------------------------------------------------------------------------------------
    /*                                   SPEED FORMULAS                                     */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Calculates the "Tick Speed" value, which directly depends on the Agility Stat
    /// </summary>
    /// <param name="agility">Agility Stat from the caller</param>
    /// <returns>
    /// The Tick Speed Value.
    /// MAY ALSO RETURN -1 WHEN AN ATTRIBUTE IS NOT RECEIVED CORRECTLY
    /// </returns>
    public static int CalculateTickSpeed(int agility)
    {
        for (int i = 0; i < tickSpeedChart.Length - 1; i++)
            if (agility < tickSpeedChart[i] && agility >= tickSpeedChart[i + 1])
                return tickSpeedTranslator[i];

        // if value is not between any boundaries
        // make sure to handle the error outside
        return -1;  
    }

    /// <summary>
    /// Calculates the value of the Turn-Clock Counter at a given time, given the unit's parameters.
    /// </summary>
    /// <param name="agility">Agility Stat from the caller</param>
    /// <param name="rank">Used Skill Rank</param>
    /// <param name="hasteStatus">The unit's Status Indicator for "Haste"</param>
    /// <returns>
    /// The Turn-Clock Counter, which directly determines turn order in-game.
    /// MAY ALSO RETURN -1 WHEN AN ATTRIBUTE IS NOT RECEIVED CORRECTLY
    /// </returns>
    public static int CalculateCounter(int tickSpeed, int rank, float hasteStatus)
    {
        // Centralized error handling 
        if (tickSpeed < 0) return -1;   
        else return Mathf.FloorToInt(tickSpeed * rank * hasteStatus);
    }


    // ---------------------------------------------------------------------------------------
    /*                                PHISICAL FORMULAS                                     */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The defensive stat is converted to an integer between 0 and 730 (called "DefNum"), 
    /// which is then used to adjust the damage. 
    /// </summary>
    /// <param name="Def">Defensive Stat (Magical or Phisical)</param>
    /// <returns></returns>
    private static int DefNumCalc(float Def) 
    {
        return Mathf.FloorToInt(Mathf.Pow(Def - 280.4f, 2) / 110 + 16);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="BaseDmg"></param>
    /// <param name="DefNum"></param>
    /// <returns></returns>
    private static int BaseDmgReduction(float BaseDmg, float Def)
    {
        float defNum = DefNumCalc(Def);
        float basedmg2 = BaseDmg * (defNum / 730f);

        return Mathf.FloorToInt(basedmg2 * (730 - (Def * 51 - Mathf.Pow(Def, 2) / 11) / 10) / 730);
    }

    /// <summary>
    /// Base Phisical Damage formula for the Physical Damage Calculation process.
    /// The formula is roughly: [{(Stat ^ 3 % 32) + 32} * (DmgConst % 16)]
    /// </summary>
    /// <param name="Stat">The Strength Stat of the attacking unit</param>
    /// <param name="DmgConst">Damage Constant of the used skill. Every damaging skill in the game has a Damage Constant</param>
    /// <returns>
    /// Base Physical Damage dealt.
    /// </returns>
    private static int PhysicalFormula(float Stat, float DmgConst)
    {
        if (Stat < 0 || Stat > 255) return -1;
        return Mathf.FloorToInt(((Mathf.Pow(Stat, 3) / 32) + 32) * (DmgConst / 16)); 
    }

    /// <summary>
    /// Physical Damage Calculation process.
    /// </summary>
    /// <param name="Stat">The Strength Stat of the attacking unit</param>
    /// <param name="DmgConst">Damage Constant of the used skill. Every damaging skill in the game has a Damage Constant</param>
    /// <param name="Def">Defensive Stat</param>
    /// <returns> Damage Dealt to unit </returns>
    public static int PhysicalDmgCalc(float Stat, float DmgConst, float Def, float braveryFactor, float armorFactor)
    {
        int BaseDmg = PhysicalFormula(Stat, DmgConst);

        if (Def < 0 || Def > 255 || BaseDmg < 0)
            return -1;

        return Mathf.RoundToInt(BaseDmgReduction(BaseDmg, Def) * braveryFactor / armorFactor);
    }


    // ---------------------------------------------------------------------------------------
    /*                                 MAGICAL FORMULAS                                     */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The formula is roughly: [DmgConst * ([Stat ^ 2 ÷ 6] + DmgConst) ÷ 4]
    /// </summary>
    /// <param name="Stat"></param>
    /// <param name="DmgConst"></param>
    /// <returns> </returns>
    private static int MagicalFormula(float Stat, float DmgConst)
    {
        if (Stat < 0 || Stat > 255) return -1;
        return Mathf.FloorToInt(DmgConst * (Mathf.Pow(Stat,2) / 6 + DmgConst) / 4);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Stat"></param>
    /// <param name="DmgConst"></param>
    /// <param name="MagDef"></param>
    /// <returns></returns>
    public static int MagicalDmgCalc(float Stat, float DmgConst, float MagDef)
    {
        int BaseDmg = MagicalFormula(Stat, DmgConst);

        if (MagDef < 0 || MagDef > 255 || BaseDmg < 0)
            return -1;
        
        return BaseDmgReduction(BaseDmg, MagDef);
    }

    // etc

}
