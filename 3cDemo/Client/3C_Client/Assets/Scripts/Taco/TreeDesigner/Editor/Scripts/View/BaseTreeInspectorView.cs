using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class BaseTreeInspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BaseTreeInspectorView, UxmlTraits> { }
        protected virtual string m_VisualTreeName => "BaseTreeInspectorInside";

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;

        protected VisualElement m_OriginalGUIContainer;
        protected VisualElement m_ExposedPropertyPanel;
        protected VisualElement m_ExposedPropertyContainer;

        protected Button m_AddExposedPropertyButton;
        protected TextField m_ExposedPropertyNameInputField;
        protected DropArea m_ExposedPropertyDropArea;

        protected Dictionary<BaseExposedProperty, ExposedPropertyView> m_ExposedPropertyViewMap = new Dictionary<BaseExposedProperty, ExposedPropertyView>();
        public Dictionary<BaseExposedProperty, ExposedPropertyView> ExposedPropertyViewMap => m_ExposedPropertyViewMap;

        Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();
        
        public BaseTreeInspectorView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>($"VisualTree/{m_VisualTreeName}");
            template.CloneTree(this);
            AddToClassList("treeInspector");
            style.display = DisplayStyle.None;

            m_OriginalGUIContainer = this.Q("property-container");
            m_ExposedPropertyPanel = this.Q("exposed-property-panel");
            m_ExposedPropertyContainer = m_ExposedPropertyPanel.Q("exposed-property-container");            
            m_ExposedPropertyNameInputField = this.Q<TextField>("add-exposed-property-name-field");

            DropdownMenuHandler dropdownMenuHandler = new DropdownMenuHandler(BuildContextualMenu);
            m_AddExposedPropertyButton = this.Q<Button>("add-exposed-property-button");
            m_AddExposedPropertyButton.clicked += () => dropdownMenuHandler.ShowMenu(m_AddExposedPropertyButton);

            m_ExposedPropertyDropArea = new DropArea();
            m_ExposedPropertyDropArea.Init(this, m_ExposedPropertyContainer);
            m_ExposedPropertyDropArea.onDragUpdateEvent += (e) =>
            {
                if (DragAndDrop.GetGenericData("ExposedPropertyView") is ExposedPropertyView exposedPropertyView && m_ExposedPropertyViewMap.ContainsValue(exposedPropertyView))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    float y = e.localMousePosition.y;
                    Dictionary<BaseExposedProperty, (ExposedPropertyView, float)> exposedPropertyViewMap = new Dictionary<BaseExposedProperty, (ExposedPropertyView, float)>();
                    exposedPropertyViewMap.Add(exposedPropertyView.ExposedProperty, (exposedPropertyView, y));
                    foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
                    {
                        if (exposedPropertyViewPair.Value != exposedPropertyView)
                            exposedPropertyViewMap.Add(exposedPropertyViewPair.Key, (exposedPropertyViewPair.Value, m_ExposedPropertyContainer.WorldToLocal(exposedPropertyViewPair.Value.worldBound.position).y + exposedPropertyViewPair.Value.worldBound.height / 2));
                    }
                    exposedPropertyViewMap = exposedPropertyViewMap.OrderBy(i => i.Value.Item2).ToDictionary(i => i.Key, i => i.Value);
                    SortExposedPropertyView(exposedPropertyViewMap);
                }
            };
            m_ExposedPropertyDropArea.onDragPerformEvent += (e) =>
            {
                if (DragAndDrop.GetGenericData("ExposedPropertyView") is ExposedPropertyView exposedPropertyView && m_ExposedPropertyViewMap.ContainsValue(exposedPropertyView))
                {
                    m_Tree.ApplyModify("Move ExposedProperty", () =>
                    {
                        int index = 0;
                        foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
                        {
                            exposedPropertyViewPair.Value.ExposedProperty.Index = index;
                            index++;
                        }
                    });
                }
            };
        }

        public virtual void PopulateView(BaseTree tree)
        {
            ClearView();
            m_Tree = tree;
            BaseTreeInspector.PopulateProperties(m_Tree, m_OriginalGUIContainer, m_ValueMap);
            m_Tree.ExposedProperties.ForEach(i => CreateExposedPropertyView(i));
            m_Tree.OnExposedPropertyChanged?.Invoke();
            SortExposedPropertyView();

            style.display = DisplayStyle.Flex;
        }
        public virtual void ClearView()
        {
            m_OriginalGUIContainer.Clear();
            m_ExposedPropertyContainer.Clear();
            m_ExposedPropertyViewMap.Clear();
        }
        public virtual void SortExposedPropertyView()
        {
            foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
            {
                m_ExposedPropertyContainer.Remove(exposedPropertyViewPair.Value);
            }
            m_ExposedPropertyViewMap = m_ExposedPropertyViewMap.OrderBy(i => i.Value.ExposedProperty.Index).ToDictionary(i => i.Key, i => i.Value);
            foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
            {
                m_ExposedPropertyContainer.Add(exposedPropertyViewPair.Value);
            }
        }
        public virtual void SortExposedPropertyView(Dictionary<BaseExposedProperty, (ExposedPropertyView, float)> propertyViewMap)
        {
            foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
            {
                m_ExposedPropertyContainer.Remove(exposedPropertyViewPair.Value);
            }
            m_ExposedPropertyViewMap = m_ExposedPropertyViewMap.OrderBy(i => propertyViewMap[i.Key].Item2).ToDictionary(i => i.Key, i => i.Value);
            foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
            {
                m_ExposedPropertyContainer.Add(exposedPropertyViewPair.Value);
            }
        }
        public virtual string GetName(BaseExposedProperty exposedProperty)
        {
            string currentName = exposedProperty.Name;
            if (string.IsNullOrEmpty(currentName))
                currentName = exposedProperty.GetType().Name;
            List<string> names = new List<string>();
            m_Tree.ExposedProperties.ForEach(i =>
            {
                if (i != exposedProperty)
                    names.Add(i.Name);
            });
            while (names.Contains(currentName))
            {
                currentName += "(1)";
            }
            return currentName;
        }

        public virtual BaseExposedProperty CreateExposedProperty(Type exposedPropertyType)
        {
            BaseExposedProperty exposedProperty = null;
            m_Tree.ApplyModify("Create ExposedProperty", () =>
            {
                exposedProperty = m_Tree.CreateExposedProperty(exposedPropertyType);
                exposedProperty.Name = m_ExposedPropertyNameInputField.text;
                exposedProperty.Name = GetName(exposedProperty);
                exposedProperty.CanEdit = true;
                m_ExposedPropertyNameInputField.value = string.Empty;
                
                m_Tree.GetNewSerializedTree();
                m_Tree.OnExposedPropertyChanged?.Invoke();
                CreateExposedPropertyView(exposedProperty);
            });
            return exposedProperty;
        }
        public virtual void RemoveExposedProperty(BaseExposedProperty exposedProperty)
        {
            m_Tree.ApplyModify("RemoveTagWithChildren ExposedProperty", () =>
            {
                m_Tree.DeleteExposedProperty(exposedProperty);
                DeleteExposedPropertyView(exposedProperty);
                exposedProperty.OnRemoved?.Invoke();
                
                m_Tree.GetNewSerializedTree();
                m_Tree.OnExposedPropertyChanged?.Invoke();
                foreach (var exposedPropertyViewPair in m_ExposedPropertyViewMap)
                {
                    exposedPropertyViewPair.Value.Rebind();
                }
            });
        }

        protected virtual void CreateExposedPropertyView(BaseExposedProperty exposedProperty)
        {
            ExposedPropertyView exposedPropertyView = new ExposedPropertyView(exposedProperty, this);
            m_ExposedPropertyContainer.Add(exposedPropertyView);
            m_ExposedPropertyViewMap.Add(exposedProperty, exposedPropertyView);
        }
        protected virtual void DeleteExposedPropertyView(BaseExposedProperty exposedProperty)
        {
            if(m_ExposedPropertyViewMap.TryGetValue(exposedProperty, out ExposedPropertyView exposedPropertyView))
            {
                m_ExposedPropertyContainer.Remove(exposedPropertyView);
                m_ExposedPropertyViewMap.Remove(exposedProperty);
            }
        }
        protected virtual void BuildContextualMenu(DropdownMenu evt)
        {
            foreach (var exposedPropertyTypePair in ExposedPropertyUtility.ExposedPropertyTypeMap)
            {
                evt.AppendAction(exposedPropertyTypePair.Key.Name, (a) =>
                {
                    CreateExposedProperty(exposedPropertyTypePair.Key);
                });
            }
        }
    }
}