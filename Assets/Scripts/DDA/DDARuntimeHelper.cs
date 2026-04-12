using DDAMAPEKitFramework;

public static class DDARuntimeHelper
{
    public static bool IsDDAActive()
    {
        if (!DDAIntegration.IsTrackingEnabled)
            return false;

        DDAMAPEKit dda = DDAMAPEKit.TryGetExistingInstance();
        return dda != null && dda.IsInitialized;
    }

    public static PlayerModel TryGetActivePlayerModel()
    {
        if (!IsDDAActive())
            return null;

        return DDAMAPEKit.TryGetExistingInstance()?.GetPlayerModel();
    }
}
