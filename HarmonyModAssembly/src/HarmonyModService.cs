using UnityEngine;
using HarmonyModAssembly;
using HarmonyLib;

public class HarmonyModService : MonoBehaviour
{
    public Texture HarmonyTexture;
    
    public void Start()
    {
		if(!Harmony.HasAnyPatches("samfundev.tweaks.Harmony"))
		{
			Patcher.Patch(HarmonyTexture);
			GetComponent<KMGameInfo>().OnStateChange += state =>
			{
				if (state == KMGameInfo.State.Setup)
					ReloadPatch.ResetDict();
			};
		}
    }
}
