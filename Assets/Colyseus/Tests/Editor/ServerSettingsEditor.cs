using System;
using Colyseus;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(ColyseusSettings))]
public class ServerSettingsEditor : Editor
{
    private SerializedProperty url;
    private SerializedProperty port;
    private SerializedProperty secureProto;
    private SerializedProperty requestHeaders;
    bool serverInfoExpanded = false;

    //private Texture colyseusIcon;
    private float buttonWidth = 250;
    private float sectionSpacer = 20;

    void OnEnable()
    {
        url = serializedObject.FindProperty("colyseusServerAddress");
        port = serializedObject.FindProperty("colyseusServerPort");
        secureProto= serializedObject.FindProperty("useSecureProtocol");
        requestHeaders = serializedObject.FindProperty("_requestHeaders");

        //
        // TODO: Why loading icon never works?
		// I've spent more than 2 hours here!
        //
        //colyseusIcon = EditorGUIUtility.IconContent("ColyseusSettings").image;
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
        //if (GUILayout.Button("Colyseus Cloud Dashboard", GUILayout.MaxWidth(buttonWidth)))
        //{
        //    Application.OpenURL("https://cloud.colyseus.io/");
        //}
        if (GUILayout.Button("Documentation", GUILayout.MaxWidth(buttonWidth)))
        {
            Application.OpenURL("https://docs.colyseus.io/?utm_source=unity-editor");
        }
        serializedObject.ApplyModifiedProperties();
    }
}
