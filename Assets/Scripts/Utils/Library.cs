using UnityEngine;

public static class Library
{
    private static GlobalLibrary _instance;
    public static GlobalLibrary Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GlobalLibrary>("GlobalLibrary");
            }
            return _instance;
        }
    }
}
