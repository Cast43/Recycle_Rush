using UnityEngine;
using Unity.NetCode;
using Unity.Entities;

public struct GoInGameRequestRpc : IRpcCommand
{

}
public struct ConnectionEntity : IComponentData
{
    public Entity Value;
}
