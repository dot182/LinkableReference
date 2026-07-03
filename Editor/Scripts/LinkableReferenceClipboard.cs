using Object = UnityEngine.Object;
using UnityEditor;
using UnityEngine;

public static class LinkableReferenceClipboard
{
    public static object Value;
    public static Object Owner;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
    }

    private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Generic || property.propertyType == SerializedPropertyType.ManagedReference)
        {
            SerializedProperty propertyCopy = property.Copy();

            // 2. Safely resolve the exact object instance anywhere inside the object tree hierarchy
            object targetObject = propertyCopy.boxedValue;

            if (targetObject != null)
            {
                menu.AddItem(new GUIContent("Copy Reference Pointer"), false, () =>
                {
                    // The lambda closure now reads the safe, resolved target object safely
                    Value = targetObject;
                    Owner = propertyCopy.serializedObject.targetObject;

                    Debug.Log($"Successfully copied {property.name} to: {targetObject.GetType().Name}");
                });
            }
        }
    }
}
