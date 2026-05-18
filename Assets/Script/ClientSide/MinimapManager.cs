using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Header("Map Setup")]
    [SerializeField] private GameObject mapContainer; // O objeto pai que liga/desliga o mapa
    [SerializeField] private RectTransform mapPanel;  // Onde os ícones ficam ancorados
    [SerializeField] private float mapScale = 2f;     // Fator de escala Mundo -> Mapa
    
    [Header("Prefabs de Ícones (UI)")]
    [SerializeField] private GameObject playerIconPrefab;
    [SerializeField] private GameObject enemyIconPrefab;
    [SerializeField] private GameObject garbageIconPrefab;
    [SerializeField] private GameObject eventIconPrefab;
    [SerializeField] private GameObject binIconPrefab;
    
    [Header("Bin Colors")]
    [SerializeField] private Color plasticColor = Color.red;
    [SerializeField] private Color paperColor = Color.blue;
    [SerializeField] private Color glassColor = Color.green;
    [SerializeField] private Color ironColor = Color.gray;
    [SerializeField] private Color organicColor = new Color(0.6f, 0.3f, 0f); // Marrom

    private GameObject playerIcon;
    
    // Pools de objetos de UI (reaproveita os ícones para não instanciar toda hora)
    private List<RectTransform> enemyIcons = new List<RectTransform>();
    private List<RectTransform> garbageIcons = new List<RectTransform>();
    private List<RectTransform> eventIcons = new List<RectTransform>();
    private List<RectTransform> binIcons = new List<RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (playerIconPrefab != null && mapPanel != null)
            playerIcon = Instantiate(playerIconPrefab, mapPanel);
            
        if (mapContainer != null)
            mapContainer.SetActive(true); // Garante que o mapa comece ligado
    }

    public bool IsMapVisible() => mapContainer != null && mapContainer.activeSelf;

    public void UpdateMap(float3 playerPos, Unity.Collections.NativeArray<float3> enemies, Unity.Collections.NativeArray<float3> garbages, Unity.Collections.NativeArray<float3> events, Unity.Collections.NativeArray<float3> bins, Unity.Collections.NativeArray<TrashType> binTypes)
    {
        if (playerIcon != null)
            UpdateIconPos(playerIcon.GetComponent<RectTransform>(), playerPos);

        UpdatePool(enemies, enemyIcons, enemyIconPrefab);
        UpdatePool(garbages, garbageIcons, garbageIconPrefab);
        UpdatePool(events, eventIcons, eventIconPrefab);
        UpdateBinPool(bins, binTypes);
    }

    private void UpdatePool(Unity.Collections.NativeArray<float3> positions, List<RectTransform> pool, GameObject prefab)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            if (i >= pool.Count)
                pool.Add(Instantiate(prefab, mapPanel).GetComponent<RectTransform>());
            
            pool[i].gameObject.SetActive(true);
            UpdateIconPos(pool[i], positions[i]);
        }

        for (int i = positions.Length; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
    }

    private void UpdateBinPool(Unity.Collections.NativeArray<float3> positions, Unity.Collections.NativeArray<TrashType> types)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            if (i >= binIcons.Count)
                binIcons.Add(Instantiate(binIconPrefab, mapPanel).GetComponent<RectTransform>());
            
            binIcons[i].gameObject.SetActive(true);
            UpdateIconPos(binIcons[i], positions[i]);

            var image = binIcons[i].GetComponent<Image>();
            if (image != null)
            {
                switch (types[i])
                {
                    case TrashType.Plastic: image.color = plasticColor; break;
                    case TrashType.Paper: image.color = paperColor; break;
                    case TrashType.Glass: image.color = glassColor; break;
                    case TrashType.Iron: image.color = ironColor; break;
                    case TrashType.Organic: image.color = organicColor; break;
                    default: image.color = Color.white; break;
                }
            }
        }

        for (int i = positions.Length; i < binIcons.Count; i++)
            if (binIcons[i].gameObject.activeSelf) binIcons[i].gameObject.SetActive(false);
    }

    private void UpdateIconPos(RectTransform rect, float3 worldPos)
    {
        // O Eixo X e Z do mundo 3D se tornam os eixos X e Y no plano do Canvas
        rect.anchoredPosition = new Vector2(worldPos.x * mapScale, worldPos.z * mapScale);
    }
}