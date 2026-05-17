using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeUIInfo", menuName = "Rougue/Upgrade UI Info")]
public class UpgradeUIInfoSO : ScriptableObject
{
    public string upgradeName; // Nome interno do efeito (mesmo que vai pelo RPC)
    [TextArea]
    public string description;
    public Sprite image;
    public UpgradeType type;
}