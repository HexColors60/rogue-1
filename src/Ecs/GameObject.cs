﻿#region copyright
// Copyright (C) 2018 "Daniel Bramblett" <bram4@pdx.edu>, "Daniel Dupriest" <kououken@gmail.com>, "Brandon Goldbeck" <bpg@pdx.edu>
// This software is licensed under the MIT License. See LICENSE file for the full text.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using IO;

namespace Ecs
{
    /// <summary>
    /// An Entity object intended for use with the Entity-Component-System architecture.
    /// </summary>
    public class GameObject
    {
        private static Dictionary<String, List<GameObject>> gameObjectsTagMap = new Dictionary<String, List<GameObject>>();
        private static Dictionary<int, GameObject> gameObjectsIdMap = new Dictionary<int, GameObject>();
        private static int IDCounter = 0;
        private static List<int> deadList = new List<int>();
        private static List<GameObject> gameObjectsToAdd = new List<GameObject>();

        private List<Component> components = new List<Component>();
        private List<Component> componentsToRemove = new List<Component>();


        private bool isActive = true;
        private String tag = "";
        private int id = -1;

        /// <summary>
        /// Auto property for the name of the GameObject.
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// Auto property for the transform of the GameObject.
        /// </summary>
        public Transform Transform { get; private set; }
        
        /// <summary>
        /// GameObject private contructor meant to be private/hidden from outside eyes.
        /// </summary>
        private GameObject() { }

        /// <summary>
        /// Calls a method by a name on all components of type 'T'.
        /// </summary>
        /// <typeparam name="T">The type of component to find on this GameObject.</typeparam>
        /// <param name="name">The name of the function to call.</param>
        /// <param name="parameters">The parameters to use for the function call.</param>
        public void SendMessage<T>(string name, object[] parameters = null)
        {
            List<T> interfaceables = GetComponents<T>();
            foreach (T interfaceable in interfaceables)
            {
                bool called = false;

                MethodInfo methodInfo = typeof(T).GetMethod(name);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(interfaceable, parameters);
                    called = true;
                }

                if (!called)
                {
                    Debug.LogError("SendInterfaceMessage<T>() Could not call method named " + name);
                }
            }
            return;
        }

        /// <summary>
        /// Determines if this GameObject is active in the game.
        /// </summary>
        public bool IsActiveSelf()
        {
            return this.isActive;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsActiveInHierarchy()
        {
            return IsActiveInHierarchy(Transform);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private bool IsActiveInHierarchy(Transform current)
        {
            if (current == null || !current.gameObject.IsActiveSelf())
            {
                return false;
            }

            // We made it to the root parent if the current.Parent is null.
            return current.Parent == null ? true : IsActiveInHierarchy(current.Parent);
        }

        /// <summary>
        /// Changes this GameObject's active state.
        /// </summary>
        /// <param name="active">The true/false state to modify this GameObject as.</param>
        /// <example>
        /// <code>
        /// GameObject go = GameObject.Instantiate();
        /// go.SetActive(false)
        /// </code>
        /// </example>
        public void SetActive(bool active)
        {
            if (this.isActive == active)
            {
                return;
            }
            foreach (Component component in components)
            { 
                if (!this.isActive && component.IsActive())
                {
                    component.OnEnable();
                }
                else
                {
                    component.OnDisable();
                }
            }

            this.isActive = active;
            return;
        }
        
        /// <summary>
        /// Accessor method for retrieving the tag on this GameObject.
        /// </summary>
        /// <returns>The string representing the tag on this GameObject</returns>
        public String Tag()
        {
            return this.tag;
        }

        /// <summary>
        /// This function is called by the Application on every updated frame. 
        /// It calls the EarlyUpdate() method on every Component attached to this
        /// GameObject.
        /// </summary>
        public static void EarlyUpdate()
        {
            // TODO: Can improve this loop performance by updating from the root game objects, recursively
            // down the tree.
            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                if (entry.Value.IsActiveInHierarchy())
                { 
                    foreach (Component component in entry.Value.GetComponents<Component>())
                    {
                        if (component.IsActive())
                        {
                            component.EarlyUpdate();
                        }
                    }
                    entry.Value.EarlyUpdateChildren();
                }
            }

            return;
        }

        /// <summary>
        /// This function is called by the Application on every updated frame. 
        /// It calls the Update() method on every Component attached to this
        /// GameObject.
        /// </summary>
        public static void Update()
        {
            // TODO: Can improve this loop performance by updating from the root game objects, recursively
            // down the tree.
            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                if (entry.Value.IsActiveInHierarchy())
                {
                    foreach (Component component in entry.Value.GetComponents<Component>())
                    {
                        if (component.IsActive())
                        {
                            component.Update();
                        }
                    }
                    entry.Value.UpdateChildren();
                }
            }

            return;
        }

        private static bool drawDebug = false;

        /// <summary>
        /// This function is called by the Application on every updated frame, but only
        /// after Update() has been invoked on each GameObject. It will call LateUpdate()
        /// on every Component attached to this GameObject.
        /// </summary>
        public static void LateUpdate()
        {
            // TODO: Can improve this loop performance by updating from the root game objects, recursively
            // down the tree.
            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                if (entry.Value.IsActiveInHierarchy())
                {
                    foreach (Component component in entry.Value.GetComponents<Component>())
                    {
                        if (component.IsActive())
                        {
                            component.LateUpdate();
                        }
                    }
                    entry.Value.LateUpdateChildren();
                }
            }
#if DEBUG
            if (Input.ReadKey().Key == ConsoleKey.D)
            {
                drawDebug = !drawDebug;
            }
#endif
            return;
        }

        /// <summary>
        /// This function is called by the Application if the window was resized.
        /// </summary>
        public static void OnResize()
        {
            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                foreach (Component component in entry.Value.GetComponents<Component>())
                {
                    if (component.IsActive())
                    {
                        component.OnResize();
                    }
                    
                }
                entry.Value.OnResizeChildren();
            }
            return;
        }

        /// <summary>
        /// Called by the Application on every updated frame during the
        /// rendering phase. It will call Render() on every Component attached to this GameObject.
        /// </summary>
        public static void Render()
        {
            
            ForceFlush();
#if DEBUG
            
            if (drawDebug)
            {
                int line = 1;
                SortedSet<int> drawnObjects = new SortedSet<int>();
                foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
                {
                    if (entry.Value.Transform.Parent == null)
                    {
                        DebugDrawRecursive(entry.Value, 0, ref line);
                    }
                    entry.Value.RenderChildren();
                }
                return;
            }
#endif
            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                foreach (Component component in entry.Value.GetComponents<Component>())
                {
                    if (component.IsActive())
                    {
                        component.Render();
                    }
                }
                entry.Value.RenderChildren();
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        /// <param name="level"></param>
        /// <param name="line"></param>
        private static void DebugDrawRecursive(GameObject go, int level, ref int line)
        {
            StringBuilder sb = new StringBuilder(" ");
            foreach (Component component in go.components)
            {
                sb.Append("(" + component.GetType().Name + ") ");
            }

            Color color = go.isActive ? Color.Gold : Color.Gray;

            ConsoleUI.Write(0, ConsoleUI.MaxHeight() - (line++), "-".PadRight(level, '-') + go.Name + sb, color);

            if (go.Transform.ChildCount() < 5)
            { 
                foreach (Transform child in go.Transform)
                {
                    DebugDrawRecursive(child.gameObject, level + 2, ref (line));
                }
            }
            else
            {
                ConsoleUI.Write(0, ConsoleUI.MaxHeight() - (line++), "-".PadRight(level + 2, '-') + "Children Collapsed", Color.Gold);
            }
            return;
        }

        /// <summary>
        /// Called by the Destroy method when called by some user. It will call OnDestroy()
        /// for each component in this GameObject.
        /// </summary>
        private void OnDestroy()
        {
            foreach (Component component in this.components)
            {
                component.OnDestroy();
            }
            this.Transform.DestroyParentReference();

            return;
        }

        /// <summary>
        /// Returns a single component of type T on this GameObject or any of its children.
        /// </summary>
        /// <returns>Component The component found, if any.</returns>
        public Component GetComponentInChildren<T>()
        {
            return GetComponentInChildren<T>(Transform);
        }

        /// <summary>
        /// Helper function to find a component in the children of a GameObject.
        /// </summary>
        /// <param name="transform">The transform node to start from.</param>
        /// <returns></returns>
        private Component GetComponentInChildren<T>(Transform transform)
        {
            if (transform != null)
            {
                if (transform.gameObject.GetComponent<T>() != null)
                {
                    return transform.gameObject.GetComponent<T>();
                }

                foreach (Transform child in transform)
                {
                    return GetComponentInChildren<T>(child);
                }

            }
            return null;
        }

        /// <summary>
        /// Add a component to the System.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>
        /// The component that was added.
        /// </returns>
        public Component AddComponent(Component component)
        {
            // We point the component's game object to point to this game object.
            component.gameObject = this;
            component.transform = this.Transform;
            
            this.components.Add(component);
            component.SetActive(true);
            component.Start();

            return component;
        }

        /// <summary>
        /// Add a component to the System.
        /// </summary>
        /// <typeparam name="T">The type of component to add.</typeparam>
        /// <returns>
        /// The component that was added.
        /// </returns>
        public Component AddComponent<T>()
        {
            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                var obj = (T)Activator.CreateInstance(typeof(T));
                return AddComponent(obj as Component);
            }

            return null;
        }

        /// <summary>
        /// Find and return a Component on this GameObject by a type.
        /// </summary>
        /// <param name="type">The type of component to find.</param>
        /// <returns>
        /// The component, if it could be found. Otherwise, null.
        /// </returns>
        public Component GetComponent(Type type)
        {
            Component retrieved = null;

            foreach (Component component in components)
            {
                if (type.IsAssignableFrom(component.GetType()))
                {
                    retrieved = component;
                    break;
                }
            }

            return retrieved;
        }

        /// <summary>
        /// Find and return a Component on this GameObject by a type.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <returns>
        /// The component, if it could be found. Otherwise, null.
        /// </returns>
        public Component GetComponent<T>()
        {
            return GetComponent(typeof(T));
        }

        /// <summary>
        /// Find and return a list of Components on this GameObject by a type.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <returns>
        /// The components found, if any could be found. Otherwise, an empty list.
        /// </returns>
        public List<T> GetComponents<T>()
        {
            List<T> retrieved = new List<T>();

            foreach (Component component in components)
            {
                if (component is T)
                {
                    retrieved.Add((T)(object)component);
                }
            }
            return retrieved;
        }

        /// <summary>
        /// Retrieve the identifier of this GameObject.
        /// </summary>
        /// <returns>
        /// The unique identifier integer for this GameObject.
        /// </returns>
        public int InstanceID()
        {
            return this.id;
        }


        /// <summary>
        /// Creates a new GameObject.
        /// </summary>
        /// <returns>
        /// The new instance of a GameObject.
        /// </returns>
        public static GameObject Instantiate()
        {
            return GameObject.Instantiate("");
        }

        /// <summary>
        /// Creates a new GameObject with a given tag for easy access.
        /// </summary>
        /// <returns>
        /// <param name="tag">The tag to store this GameObject.</param>
        /// The new instance of a GameObject.
        /// </returns>
        public static GameObject Instantiate(String tag)
        {
            GameObject go = new GameObject();

            // Every game object will have a transform component.
            Transform transform = new Transform();
            go.AddComponent(transform);
            go.Transform = transform;
            go.id = IDCounter++;
            go.tag = tag;
            gameObjectsToAdd.Add(go);

            if (tag != null && tag != "")
            {
                // Add the game object to the data structures.
                if (!gameObjectsTagMap.ContainsKey(tag))
                {
                    gameObjectsTagMap.Add(tag, new List<GameObject>());
                }

                if (gameObjectsTagMap.TryGetValue(tag, out List<GameObject> goList))
                {
                    goList.Add(go);
                }
            }

            return go;
        }

        /// <summary>
        /// Remove a Component from the System.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void Destroy(Component component)
        {
            if (component != null)
            {
                // OnDestroy? Probably
                if (component.gameObject != null)
                {
                    component.OnDestroy();
                }
                component.gameObject.componentsToRemove.Add(component);
            }
            return;
        }
        
        /// <summary>
        /// Remove a GameObject from the System.
        /// </summary>
        /// <param name="go">The GameObject instance to remove.</param>
        public static void Destroy(GameObject go)
        {      
            if (go != null)
            {
                go.OnDestroy();
                // Remove all the children game objects along with this game object.
                foreach (Transform t in go.Transform)
                {
                    Destroy(t.gameObject);
                }
                if (go.tag != "")
                {
                    // If the game object has a tag value, we need to remove it from the tag map.
                    if (gameObjectsTagMap.TryGetValue(go.tag, out List<GameObject> goList))
                    {
                        goList.Remove(go);
                        if (goList.Count == 0)
                        {
                            gameObjectsTagMap.Remove(go.tag);
                        }
                    }
                    
                }
                // Add the game object from the game objects to the dead list.
                deadList.Add(go.id);
                
            }
            return;
        }

        /// <summary>
        /// Add new GameObjects that were added this frame. Remove dead GameObjects that
        /// were destroyed this frame.
        /// This adds the newly created game objects for this frame as well ass clearing out the
        /// dead ones.
        /// </summary>
        private static void ForceFlush()
        {
            AddNewGameObjects();
            ClearDeadGameObjects();

            foreach (KeyValuePair<int, GameObject> entry in gameObjectsIdMap)
            {
                entry.Value.ClearDeadComponents();
            }
            return;
        }

        /// <summary>
        /// Clear out the list of components that were removed.
        /// </summary>
        public void ClearDeadComponents()
        {
            foreach (Component component in componentsToRemove)
            {
                components.Remove(component);
            }
            return;
        }

        /// <summary>
        /// Clear out the list of GameObjects that were removed.
        /// </summary>
        private static void ClearDeadGameObjects()
        {
            foreach (int id in deadList)
            {
                if (gameObjectsIdMap.TryGetValue(id, out GameObject go))
                { 
                    go.componentsToRemove.AddRange(go.components);
                    go.ClearDeadComponents();
                    gameObjectsIdMap.Remove(go.id);
                }

            }
            deadList.Clear();

            return;
        }

        /// <summary>
        /// Add new GameObjects from the newly created GameObjects.
        /// </summary>
        private static void AddNewGameObjects()
        {
            foreach (GameObject go in gameObjectsToAdd)
            {
                gameObjectsIdMap.Add(go.id, go);
                
            }
            gameObjectsToAdd.Clear();

            return;
        }
        
        /// <summary>
        /// Find the first GameObject that is returned by a particular tag.
        /// </summary>
        /// <param name="tag">The tag to search for a GameObject.</param>
        /// <returns>
        /// The GameObject that was found, or null if none was found.
        /// </returns>
        public static GameObject FindWithTag(String tag)
        {
            GameObject go = null;
            if (gameObjectsTagMap.TryGetValue(tag, out List<GameObject> goList))
            {
                if (goList.Count > 0)
                {
                    go = goList.ElementAt<GameObject>(0);
                }
            }
            return go;
        }

        /// <summary>
        /// Find a list of GameObjects with a particular tag.
        /// </summary>
        /// <param name="tag">The tag to use to search for GameObjects</param>
        /// <returns>
        /// The list of GameObjects that were found, or null if nothing was found.
        /// </returns>
        public static List<GameObject> FindGameObjectsWithTag(String tag)
        {
            if (gameObjectsTagMap.TryGetValue(tag, out List<GameObject> goList))
            {
                return goList;
            }
            return null;
        }

        /// <summary>
        /// Changes the active state of this GameObject and all it's children.
        /// </summary>
        /// <param name="state"></param>
        public void SetActiveRecursively(bool state)
        {
            SetActive(state);
            if (Transform != null)
            {
                foreach (Transform t in Transform)
                {
                    t.gameObject.SetActiveRecursively(state);
                }
            }
            return;
        }

        public static GameObject Instantiate(GameObject parent)
        {
            GameObject go = new GameObject();

            Transform transform = new Transform();
            go.AddComponent(transform);
            go.Transform = transform;
            go.id = IDCounter++;
            go.tag = "";

            transform.SetParent(parent.Transform);
            return go;
        }

        public void EarlyUpdateChildren()
        {
            foreach(Transform child in this.Transform)
            {
                if(child?.gameObject != null)
                {
                    if(child.gameObject.IsActiveInHierarchy())
                    {
                        foreach(Component component in child.gameObject.GetComponents<Component>())
                        {
                            if(component.IsActive())
                            {
                                component.EarlyUpdate();
                            }
                        }
                        child.gameObject.EarlyUpdateChildren();
                    }
                }
            }
        }

        public void UpdateChildren()
        {
            foreach (Transform child in this.Transform)
            {
                if (child?.gameObject != null)
                {
                    if (child.gameObject.IsActiveInHierarchy())
                    {
                        foreach (Component component in child.gameObject.GetComponents<Component>())
                        {
                            if (component.IsActive())
                            {
                                component.Update();
                            }
                        }
                        child.gameObject.UpdateChildren();
                    }
                }
            }
        }

        public void LateUpdateChildren()
        {
            foreach (Transform child in this.Transform)
            {
                if (child?.gameObject != null)
                {
                    if (child.gameObject.IsActiveInHierarchy())
                    {
                        foreach (Component component in child.gameObject.GetComponents<Component>())
                        {
                            if (component.IsActive())
                            {
                                component.LateUpdate();
                            }
                        }
                        child.gameObject.LateUpdateChildren();
                    }
                }
            }
        }

        public void OnResizeChildren()
        {
            foreach (Transform child in this.Transform)
            {
                if (child?.gameObject != null)
                {
                    if (child.gameObject.IsActiveInHierarchy())
                    {
                        foreach (Component component in child.gameObject.GetComponents<Component>())
                        {
                            if (component.IsActive())
                            {
                                component.OnResize();
                            }
                        }
                        child.gameObject.OnResizeChildren();
                    }
                }
            }
        }

        public void RenderChildren()
        {
            foreach (Transform child in this.Transform)
            {
                if (child?.gameObject != null)
                {
                    if (child.gameObject.IsActiveInHierarchy())
                    {
                        foreach (Component component in child.gameObject.GetComponents<Component>())
                        {
                            if (component.IsActive())
                            {
                                component.Render();
                            }
                        }
                        child.gameObject.RenderChildren();
                    }
                }
            }
        }

        public static GameObject Instantiate(GameObject parent, String tag)
        {
            GameObject go = new GameObject();

            // Every game object will have a transform component.
            Transform transform = new Transform();
            go.AddComponent(transform);
            go.Transform = transform;
            go.id = IDCounter++;
            go.tag = tag;
            transform.SetParent(parent.Transform);

            if (tag != null && tag != "")
            {
                // Add the game object to the data structures.
                if (!gameObjectsTagMap.ContainsKey(tag))
                {
                    gameObjectsTagMap.Add(tag, new List<GameObject>());
                }

                if (gameObjectsTagMap.TryGetValue(tag, out List<GameObject> goList))
                {
                    goList.Add(go);
                }
            }

            return go;
        }
    }


}
