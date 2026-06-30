using ApexShift.Runtime.Audio;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Player;
using ApexShift.EditorTools.Data;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Audio
{
    public sealed class AudioProfileQuickAssignWindow : EditorWindow
    {
        [MenuItem("Tools/Apex Shift/Audio/Quick Assign Profiles")]
        public static void Open()
        {
            GetWindow<AudioProfileQuickAssignWindow>("Audio Profiles");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Quick Assign", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assigns default audio profiles to selected objects with matching components.", MessageType.Info);

            if (GUILayout.Button("Assign To Selection"))
            {
                AssignToSelection();
            }

            if (GUILayout.Button("Create Default Profiles"))
            {
                DefaultAudioAssetGenerator.CreateDefaultAudioProfiles();
            }
        }

        private static void AssignToSelection()
        {
            CombatAudioProfile combat = DefaultAudioAssetGenerator.LoadCombatProfile();
            TrapAudioProfile trap = DefaultAudioAssetGenerator.LoadTrapProfile();
            CreatureAudioProfile creature = DefaultAudioAssetGenerator.LoadCreatureProfile();

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null)
                {
                    continue;
                }

                PlayerCombatRuntime playerCombat = go.GetComponent<PlayerCombatRuntime>();
                if (playerCombat != null && combat != null)
                {
                    SerializedObject so = new SerializedObject(playerCombat);
                    so.FindProperty("combatAudioProfile").objectReferenceValue = combat;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(playerCombat);
                }

                TrapDamageRuntime trapRuntime = go.GetComponent<TrapDamageRuntime>();
                if (trapRuntime != null && trap != null)
                {
                    SerializedObject so = new SerializedObject(trapRuntime);
                    so.FindProperty("trapAudioProfile").objectReferenceValue = trap;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(trapRuntime);
                }

                PlaceableStructureRuntime structure = go.GetComponent<PlaceableStructureRuntime>();
                if (structure != null && trap != null)
                {
                    SerializedObject so = new SerializedObject(structure);
                    so.FindProperty("trapAudioProfile").objectReferenceValue = trap;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(structure);
                }

                CreatureHealthRuntime creatureHealth = go.GetComponent<CreatureHealthRuntime>();
                if (creatureHealth != null && creature != null)
                {
                    SerializedObject so = new SerializedObject(creatureHealth);
                    so.FindProperty("creatureAudioProfile").objectReferenceValue = creature;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(creatureHealth);
                }
            }
        }
    }
}
