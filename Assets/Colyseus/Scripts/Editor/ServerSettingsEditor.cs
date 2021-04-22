using System.Collections;
using System.Collections.Generic;
using Colyseus;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ColyseusSettings))]
public class ServerSettingsEditor : Editor
{
    private SerializedProperty url;
    private SerializedProperty port;
    private SerializedProperty secureProto;
    private SerializedProperty requestHeaders;
    bool serverInfoExpanded = false;

    private Texture colyseusIcon;
    private float buttonWidth = 250;
    private float sectionSpacer = 20;
    void OnEnable()
    {
        url = serializedObject.FindProperty("colyseusServerAddress");
        port = serializedObject.FindProperty("colyseusServerPort");
        secureProto= serializedObject.FindProperty("useSecureProtocol");
        requestHeaders = serializedObject.FindProperty("_requestHeaders");
        GUIContent content = EditorGUIUtility.IconContent("ColyseusSettings");
        if (content != null)
        {
            colyseusIcon = content.image;
        }
        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Colyseus Server Settings", EditorStyles.boldLabel);
        serverInfoExpanded = EditorGUILayout.Foldout(serverInfoExpanded, "Server Information");
        if (serverInfoExpanded)
        {
            EditorGUILayout.PropertyField(url);
            EditorGUILayout.PropertyField(port);
            EditorGUILayout.PropertyField(secureProto);
        }

        EditorGUILayout.PropertyField(requestHeaders);

        EditorGUILayout.Space(sectionSpacer);
        EditorGUILayout.LabelField("Additional Resources", EditorStyles.boldLabel);
        if (GUILayout.Button("Colyseus Arena Dashboard", GUILayout.MaxWidth(buttonWidth)))
        {
            Application.OpenURL("https://console.colyseus.io/");
        }
        if (GUILayout.Button("Documentation", GUILayout.MaxWidth(buttonWidth)))
        {
            Application.OpenURL("https://docs.colyseus.io/");
        }
        serializedObject.ApplyModifiedProperties();
    }
}
