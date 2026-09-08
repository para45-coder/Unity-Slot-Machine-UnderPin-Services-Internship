using UnityEngine;

[CreateAssetMenu(fileName = "New Symbol", menuName = "SlotGame/Symbol")]
public class SymbolData : ScriptableObject
{
    public string symbolName;
    public Sprite symbolSprite;
    public int multiplier = 1;
}