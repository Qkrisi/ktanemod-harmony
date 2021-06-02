using UnityEngine;
using HarmonyModAssembly;

public class HarmonyModService : MonoBehaviour
{
    public Texture HarmonyTexture;
    
    public void Start()
    {
        Patcher.Patch(HarmonyTexture);
    }
}
