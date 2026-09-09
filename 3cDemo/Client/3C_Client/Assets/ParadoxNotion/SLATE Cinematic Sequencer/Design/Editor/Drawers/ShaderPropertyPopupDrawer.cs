#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using ShaderUtil = UnityEditor.ShaderUtil;
using UnityEngine.Rendering;

namespace Slate
{

    [CustomPropertyDrawer(typeof(ShaderPropertyPopupAttribute))]
    public class ShaderPropertyPopupDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) { return -2; }
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent content) {
            var att = (ShaderPropertyPopupAttribute)attribute;

            if ( prop.serializedObject.targetObject is IDirectable directable ) {
                var actor = directable.actor;
                if ( actor != null ) {
                    if ( actor.TryGetComponent<Renderer>(out var renderer) ) {
                        var material = renderer.sharedMaterial;
                        if ( material != null ) {
                            var shader = material.shader;
                            var options = new List<string>();

#if UNITY_6000_3_OR_NEWER

                            for ( var i = 0; i < shader.GetPropertyCount(); i++ ) {

                                if ( shader.GetPropertyFlags(i) == ShaderPropertyFlags.HideInInspector ) {
                                    continue;
                                }

                                if ( att.propertyType != null ) {
                                    var type = shader.GetPropertyType(i);
                                    if ( att.propertyType == typeof(Color) && type != ShaderPropertyType.Color ) { continue; }
                                    if ( att.propertyType == typeof(Texture) && type != ShaderPropertyType.Texture ) { continue; }
                                    if ( att.propertyType == typeof(float) && type != ShaderPropertyType.Float && type != ShaderPropertyType.Range ) { continue; }
                                    if ( ( att.propertyType == typeof(Vector2) || att.propertyType == typeof(Vector4) ) && type != ShaderPropertyType.Vector ) { continue; }
                                }

                                options.Add(shader.GetPropertyName(i));
                            }

#else

                            for ( var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++ ) {
                                if ( ShaderUtil.IsShaderPropertyHidden(shader, i) ) {
                                    continue;
                                }

                                if ( att.propertyType != null ) {
                                    var type = ShaderUtil.GetPropertyType(shader, i);
                                    if ( att.propertyType == typeof(Color) && type != ShaderUtil.ShaderPropertyType.Color ) { continue; }
                                    if ( att.propertyType == typeof(Texture) && type != ShaderUtil.ShaderPropertyType.TexEnv ) { continue; }
                                    if ( att.propertyType == typeof(float) && type != ShaderUtil.ShaderPropertyType.Float && type != ShaderUtil.ShaderPropertyType.Range ) { continue; }
                                    if ( ( att.propertyType == typeof(Vector2) || att.propertyType == typeof(Vector4) ) && type != ShaderUtil.ShaderPropertyType.Vector ) { continue; }
                                }

                                options.Add(ShaderUtil.GetPropertyName(shader, i));
                            }

#endif


                            prop.stringValue = EditorTools.CleanPopup<string>(content.text, prop.stringValue, options);
                            return;
                        }
                    }
                }
            }

            prop.stringValue = EditorGUILayout.TextField(content.text, prop.stringValue);

        }
    }
}

#endif