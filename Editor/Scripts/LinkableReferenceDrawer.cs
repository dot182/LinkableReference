using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LinkableReferenceAttribute))]
public class LinkableReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // MODE A: The "Pointer" Slot (Has both SerializeReference AND LinkableReference)
        if (property.propertyType == SerializedPropertyType.ManagedReference)
        {
            DrawPointerSlot(position, property, label);
        }
        else if (property.propertyType != SerializedPropertyType.Generic)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Set up the rectangle for your help box
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpBoxRect, $"Using LinkableReference when it is a value type", MessageType.Warning);
            // Adjust the Y position so the next field draws below the warning
            position.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float total = EditorGUI.GetPropertyHeight(property, label, true);
        if (property.propertyType != SerializedPropertyType.Generic)
            total += EditorGUIUtility.singleLineHeight + 2f;
        return total;
    }

    // ==========================================
    // DRAW LOGIC: POINTER SLOT (MODE A)
    // ==========================================
    private void DrawPointerSlot(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Type targetType = GetFieldOrElementType();
        if (targetType == null)
        {
            EditorGUI.LabelField(position, label.text, "Unknown field type.");
            EditorGUI.EndProperty();
            return;
        }

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(labelRect, label);

        // Peek inside the linked class to see if it has a "Name" field we can display
        object currentValue = property.managedReferenceValue;

        string displayName = currentValue != null
            ? $"{targetType} (Linked)"
            : "None (Right-Click to Paste Reference)";

        // Draw the clean ScriptableObject-style slot
        GUI.Box(fieldRect, displayName, EditorStyles.objectField);

        // Handle pasting into this slot
        HandlePointerContextMenu(fieldRect, property, targetType);

        EditorGUI.EndProperty();
    }

    private void HandlePointerContextMenu(Rect rect, SerializedProperty property, Type targetType)
    {
        Event e = Event.current;
        if (e.type != EventType.ContextClick || !rect.Contains(e.mousePosition)) return;

        var menu = new GenericMenu();
        bool canPaste = LinkableReferenceClipboard.Value != null && targetType.IsInstanceOfType(LinkableReferenceClipboard.Value);

        if (canPaste)
        {
            menu.AddItem(new GUIContent("Paste Reference (Link)"), false, () =>
            {
                property.serializedObject.Update();
                property.managedReferenceValue = LinkableReferenceClipboard.Value;
                property.serializedObject.ApplyModifiedProperties();

                if (LinkableReferenceClipboard.Owner != null && LinkableReferenceClipboard.Owner != property.serializedObject.targetObject)
                {
                    Debug.LogWarning(
                        $"[LinkableReference] Linked across two different objects! This shared instance will NOT survive project compile/reload.",
                        property.serializedObject.targetObject);
                }
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("Paste Reference (Link) [Incompatible Type]"));
        }

        if (property.managedReferenceValue != null)
        {
            menu.AddItem(new GUIContent("Clear Link"), false, () =>
            {
                property.serializedObject.Update();
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
        e.Use();
    }

    // ==========================================
    // DRAW LOGIC: ORIGINAL FIELD (MODE B)
    // ==========================================
    private void DrawOriginalField(Rect position, SerializedProperty property, GUIContent label)
    {
        // Overlay an invisible context-menu listener on top of the field's main label row
        Rect contextArea = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        Event e = Event.current;
        if (contextArea.Contains(e.mousePosition) && e.type == EventType.ContextClick)
        {
            // 1. CRITICAL FOR LISTS: Create a frozen snapshot copy of the exact property element
            SerializedProperty propertyCopy = property.Copy();

            // 2. Safely resolve the exact object instance anywhere inside the object tree hierarchy
            object targetObject = propertyCopy.boxedValue;

            if (targetObject != null)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy Reference Pointer"), false, () =>
                {
                    // The lambda closure now reads the safe, resolved target object safely
                    LinkableReferenceClipboard.Value = targetObject;
                    LinkableReferenceClipboard.Owner = propertyCopy.serializedObject.targetObject;

                    Debug.Log($"Successfully copied reference to: {targetObject.GetType().Name}");
                });

                menu.ShowAsContext();
                e.Use();
            }
        }
        // Let Unity draw the original fields completely naturally (restores text fields, lists, etc.)
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    // ==========================================
    // HELPERS
    // ==========================================
    private Type GetFieldOrElementType()
    {
        if (fieldInfo == null) return null;
        Type type = fieldInfo.FieldType;
        if (type.IsArray) return type.GetElementType();
        if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType) return type.GetGenericArguments()[0];
        return type;
    }
}
