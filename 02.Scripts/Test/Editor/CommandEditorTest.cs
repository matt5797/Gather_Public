using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using Gather.Interact;

[CustomPropertyDrawer(typeof(InteractCommand<DefaultGroup>))]
public class CommandEditorTest : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);

        SerializedProperty authority = property.FindPropertyRelative("authority");

        EditorGUI.PropertyField(position, property, label, true);
        if (property.isExpanded)
        {
            /*if (GUI.Button(new Rect(position.xMin + 30f, position.yMax - 20f, position.width - 30f, 20f), "button"))
            {
                // do things
            }*/
        }

        SerializedProperty testField2 = property.FindPropertyRelative("testField2");
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
            return EditorGUI.GetPropertyHeight(property) + 20f;
        return EditorGUI.GetPropertyHeight(property);
    }

    public void test2(object item, SerializedProperty property)
    {
        Debug.Log("qqqqqqqqqqq");
        if (item is InteractCommand<DefaultGroup>)
        {
            Debug.Log("InteractionCommand<ObjectGroup>");
            if (item is DeleteObjectCommand)
            {
                //(DeleteObjectCommand)item;
                Test3<DeleteObjectCommand>(item as DeleteObjectCommand, property);
                Debug.Log("DeleteObjectCommand");
            }
        }
        else if (item is DeleteObjectCommand)
        {
            Debug.Log("DeleteObjectCommand");
        }
    }

    public void Test3<T>(T item, SerializedProperty property)
    {
        Debug.Log("wwwwwwwwwww");
        var fields = item.GetType().GetFields();
        foreach (var field in fields)
        {
            Debug.LogAssertion($"{field.Name}({field.FieldType}) : {Convert.ChangeType(field.GetValue(item), field.FieldType)}");
            //EditorGUILayout.LabelField(field.Name, field.GetValue(item).ToString());
        }
    }
}

[CustomEditor(typeof(DefaultGroup))]
public class CommandEditorTest2 : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

