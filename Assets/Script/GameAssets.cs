using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public const int UNIT_LAYER = 3;
    public const int EXPERIENCE_LAYER = 7;

    public static GameAssets Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }


}
