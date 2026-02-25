// WorldTextManager.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class WorldTextManager : MonoBehaviour
{
    public static WorldTextManager Instance { get; private set; }

    [Header("Configurações")]
    [SerializeField] private GameObject damageTextPrefab; // Prefab do seu Texto
    [SerializeField] private int poolSize = 30; // Quantos textos criar de antemão

    // A nossa "piscina" de textos desativados prontos para uso
    private Queue<GameObject> textPool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        // Cria vários textos ocultos no início da partida
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(damageTextPrefab, transform); // transform = Canvas World
            obj.SetActive(false);
            textPool.Enqueue(obj);
        }
    }

    // O ECS chama este método
    public void ShowDamage(float3 worldPosition, int amount)
    {
        if (textPool.Count == 0) return; // Se acabaram os textos na piscina, ignora (ou você pode instanciar mais)

        // Pega um texto desativado da fila
        GameObject textObj = textPool.Dequeue();
        textObj.SetActive(true);

        // Define a posição 3D
        textObj.transform.position = new Vector3(worldPosition.x, worldPosition.y + 1f, worldPosition.z);

        // Configura o texto e avisa ele quem é o Manager para ele poder voltar depois
        FloatingDamageText floatingScript = textObj.GetComponent<FloatingDamageText>();
        floatingScript.Setup(amount, this);
    }

    // O Texto chama este método quando termina a animação para se "reciclar"
    public void ReturnToPool(GameObject textObj)
    {
        textObj.SetActive(false);
        textPool.Enqueue(textObj);
    }
}