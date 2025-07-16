using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class CameraSingleton : MonoBehaviour
{
    public static CameraSingleton Instance;

    public float3 offset = new float3(0, 5, -10);
    public float smoothSpeed = 5f;
    public float minDist = 3f;
    public float maxDist = 5f;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
