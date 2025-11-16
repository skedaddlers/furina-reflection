using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public static class Helpers
{
    public static T GetRandomElement<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
            return default(T);
        int index = Random.Range(0, list.Count);
        return list[index];
    }

    public static List<T> GetRandomItems<T>(List<T> sourceList, int itemCount)
    {
        List<T> selectedItems = new List<T>();
        if (sourceList == null || sourceList.Count == 0 || itemCount <= 0)
            return selectedItems;

        List<T> tempList = new List<T>(sourceList);
        for (int i = 0; i < itemCount && tempList.Count > 0; i++)
        {
            int index = Random.Range(0, tempList.Count);
            selectedItems.Add(tempList[index]);
            tempList.RemoveAt(index);
        }        
        return selectedItems;
    }
}