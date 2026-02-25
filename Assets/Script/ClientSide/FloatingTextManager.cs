// FloatingDamageText.cs
using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Configurações")]
    public float floatSpeed = 2f;
    public float lifeTime = 1.5f; // Quanto tempo dura na tela
    public TextMeshProUGUI textMesh;

    private Transform mainCameraTransform;
    private float timer;
    private WorldTextManager manager;

    private void Awake()
    {
        // Guarda a câmera principal para o efeito Billboard (olhar para a tela)
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    // Chamado pelo WorldTextManager ao ser ativado
    public void Setup(int amount, WorldTextManager manager)
    {
        this.manager = manager;
        textMesh.text = amount.ToString();
        timer = lifeTime; // Reseta o cronômetro
    }

    private void Update()
    {
        // 1. Cronômetro de vida
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            // Em vez de Destroy(), devolvemos para o Pool!
            manager.ReturnToPool(gameObject);
            return;
        }

        // 2. Faz o texto subir
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 3. Efeito Billboard: Faz olhar para a câmera do jogador local
        if (mainCameraTransform != null)
        {
            Vector3 directionToCamera = transform.position - mainCameraTransform.position;
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
}