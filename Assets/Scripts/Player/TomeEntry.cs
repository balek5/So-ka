using UnityEngine;

[System.Serializable]
public sealed class TomeEntry
{
    public Tome tome;

    [Min(1)]
    public int stacks = 1;
}
