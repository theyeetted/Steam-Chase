using SerializeReferenceEditor;
using UnityEngine;
[System.Serializable]

public class AutoTargetEffect
{
    [field: SerializeReference, SR] public TargetMode TargetMode {  get; private set; }
    [field: SerializeReference, SR] public Effect Effect { get; private set; }
    [field: SerializeField, Min(1)] public int RepeatCount { get; private set; } = 1;
    [field: SerializeField, Range(0, 100)] public int ChancePercent { get; private set; } = 100;
}
    
