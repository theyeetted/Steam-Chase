#if UNITY_EDITOR
using System.Linq;
using Steamchasing.Cards;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        CardDatabase database = (CardDatabase)target;

        if (GUILayout.Button("Refresh / Validate Database"))
        {
            database.Initialize();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space(5);

        int total = database.Cards.Count;
        int common = database.Cards.Count(c => c != null && c.Rarity == CardRarity.Common);
        int rare = database.Cards.Count(c => c != null && c.Rarity == CardRarity.Rare);
        int legend = database.Cards.Count(c => c != null && c.Rarity == CardRarity.Legend);
        int unique = database.Cards.Count(c => c != null && c.Rarity == CardRarity.Unique);

        EditorGUILayout.LabelField("Card Count", total.ToString());
        EditorGUILayout.LabelField("Common", common.ToString());
        EditorGUILayout.LabelField("Rare", rare.ToString());
        EditorGUILayout.LabelField("Legend", legend.ToString());
        EditorGUILayout.LabelField("Unique", unique.ToString());
    }
}
#endif
