#if false
//UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EditorExtension;

[CanEditMultipleObjects]
[CustomEditor(typeof(AudioSource), true)]
public class AudioSourceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ComponentHeaderUtility.DrawHeaderButtons((Component)target);
    }
}
#endif