using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public enum Rarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public static class Helpers
{

    public static T GetRandomElement<T>(this List<T> list, int seed = 0)
    {
        if (list == null || list.Count == 0)
            return default(T);
        System.Random rng = new System.Random(ResolveSeed(seed));
        int index = rng.Next(list.Count);
        return list[index];
    }

    public static List<T> GetRandomItems<T>(List<T> sourceList, int itemCount, int seed = 0)
    {
        List<T> selectedItems = new List<T>();
        if (sourceList == null || sourceList.Count == 0 || itemCount <= 0)
            return selectedItems;

        List<T> tempList = new List<T>(sourceList);
        System.Random rng = new System.Random(ResolveSeed(seed));
        for (int i = 0; i < itemCount && tempList.Count > 0; i++)
        {
            int index = rng.Next(tempList.Count);
            selectedItems.Add(tempList[index]);
            tempList.RemoveAt(index);
        }        
        return selectedItems;
    }

    public static List<T> GetRandomItemsAllowRepeats<T>(List<T> sourceList, int itemCount, int seed = 0)
    {
        var selectedItems = new List<T>();
        if (sourceList == null || sourceList.Count == 0 || itemCount <= 0)
            return selectedItems;

        System.Random rng = new System.Random(ResolveSeed(seed));
        for (int i = 0; i < itemCount; i++)
        {
            int index = rng.Next(sourceList.Count);
            selectedItems.Add(sourceList[index]);
        }

        return selectedItems;
    }

    public static List<T> GetWeightedRandomItems<T>(List<T> sourceList, int itemCount, Func<T, float> weightSelector, int seed = 0)
    {
        var selectedItems = new List<T>();
        if (sourceList == null || sourceList.Count == 0 || itemCount <= 0)
            return selectedItems;

        var tempList = new List<T>(sourceList);
        System.Random rng = new System.Random(ResolveSeed(seed));

        for (int i = 0; i < itemCount && tempList.Count > 0; i++)
        {
            int index = GetWeightedRandomIndex(tempList, weightSelector, rng);
            if (index < 0 || index >= tempList.Count)
                break;

            selectedItems.Add(tempList[index]);
            tempList.RemoveAt(index);
        }

        return selectedItems;
    }

    private static int GetWeightedRandomIndex<T>(List<T> sourceList, Func<T, float> weightSelector, System.Random rng)
    {
        if (sourceList == null || sourceList.Count == 0)
            return -1;

        float totalWeight = 0f;
        int fallbackIndex = -1;

        for (int i = 0; i < sourceList.Count; i++)
        {
            T item = sourceList[i];
            if (!ReferenceEquals(item, null) && fallbackIndex < 0)
                fallbackIndex = i;

            if (ReferenceEquals(item, null))
                continue;

            float weight = Mathf.Max(0f, weightSelector != null ? weightSelector(item) : 1f);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return fallbackIndex;

        double roll = rng.NextDouble() * totalWeight;
        for (int i = 0; i < sourceList.Count; i++)
        {
            T item = sourceList[i];
            if (ReferenceEquals(item, null))
                continue;

            float weight = Mathf.Max(0f, weightSelector != null ? weightSelector(item) : 1f);
            if (weight <= 0f)
                continue;

            if (roll < weight)
                return i;

            roll -= weight;
        }

        return fallbackIndex;
    }

    private static int ResolveSeed(int seed)
    {
        if (seed != 0)
            return seed;

        unchecked
        {
            return Environment.TickCount ^ (Time.frameCount * 397);
        }
    }

    public static float CalculateFinalDamage(
        float baseDamage, 
        float defense, 
        float critChance, 
        float critMultiplier,
        int levelDifference = 0,
        float damageMultiplier = 1f)
    {
        return CalculateFinalDamage(
            baseDamage,
            defense,
            critChance,
            critMultiplier,
            levelDifference,
            damageMultiplier,
            out _);
    }

    public static float CalculateFinalDamage(
        float baseDamage,
        float defense,
        float critChance,
        float critMultiplier,
        int levelDifference,
        float damageMultiplier,
        out bool didCrit)
    {
        float mitigatedDamage = baseDamage * ((1-(defense / (defense + 25 + 3 * (levelDifference)))));
        float finalDamage = mitigatedDamage * damageMultiplier;
        didCrit = Random.value <= critChance;
        if (didCrit)
        {
            finalDamage *= critMultiplier;
        }
        // Debug.Log($"[Damage Calculation] Base: {baseDamage}, Defense: {defense}, LevelDiff: {levelDifference}, Mitigated: {mitigatedDamage}, Multiplier: {damageMultiplier}, CritChance: {critChance}, CritMultiplier: {critMultiplier}, Final: {finalDamage}");
        return finalDamage;
    }

    public static Color GetColorForRarity(Rarity rarity)
    {
        GlobalLibrary library = Library.Instance;
        if (library == null || library.rarityColors == null || library.rarityColors.Count < 4)
        {
            Debug.LogWarning("GlobalLibrary or rarityColors not properly set up.");
            return Color.white;
        }
        switch (rarity)
    {
            case Rarity.Common:
                return library.rarityColors[0];
            case Rarity.Rare:
                return library.rarityColors[1];
            case Rarity.Epic:
                return library.rarityColors[2];
            case Rarity.Legendary:
                return library.rarityColors[3];
            default:
                return Color.white;
        }
    }

    public static string GetRarityNameFromColor(Color color)
    {
        GlobalLibrary library = Library.Instance;
        if (library == null || library.rarityColors == null || library.rarityColors.Count < 4)
        {
            Debug.LogWarning("GlobalLibrary or rarityColors not properly set up.");
            return "Unknown";
        }

        if (color == library.rarityColors[0])
            return "Common";
        else if (color == library.rarityColors[1])
            return "Rare";
        else if (color == library.rarityColors[2])
            return "Epic";
        else if (color == library.rarityColors[3])
            return "Legendary";
        else
            return "Unknown";
    }
}
