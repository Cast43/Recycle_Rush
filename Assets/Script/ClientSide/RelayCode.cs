using UnityEngine;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using TMPro;
public class RelayCode : MonoBehaviour
{
    public static RelayCode instance;
    public string tempCode;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // opcional: destrói duplicata criada via cena
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void SetCode(string code)
    {
        tempCode = code;
    }
}
