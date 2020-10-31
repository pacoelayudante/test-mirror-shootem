using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public static class ControlSteam
{
    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        Application.quitting += () => {
            SteamClient.Shutdown();
            Debug.Log("Steam Client Shot down (suppousedly)");
        };

        Coroutinator.Start(RutinaDeSteam());
        try
        {
            SteamClient.Init(480);

        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            // Something went wrong - it's one of these:
            //
            //     Steam is closed?
            //     Can't find steam_api dll?
            //     Don't have permission to play app?
            //
        }
    }

    static IEnumerator RutinaDeSteam()
    {
        while (true)
        {
            if (SteamClient.IsLoggedOn) SteamClient.RunCallbacks();
            yield return null;
        }
    }
}
