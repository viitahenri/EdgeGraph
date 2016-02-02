using UnityEngine;
using System.Collections.Generic;

namespace UtilityTools
{
    public class Helper
    {
        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null) return go.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Non-idiotproof generic to get next index in a list.
        /// Goes around from end to start.
        /// </summary>
        public static int GetNextIndex<T>(List<T> list, int currentIndex)
        {
            if (currentIndex + 1 >= list.Count) return 0;
            else
                return currentIndex + 1;
        }

        /// <summary>
        /// Non-idiotproof generic to get previous index in a list.
        /// Goes around from start to end.
        /// </summary>
        public static int GetPrevIndex<T>(List<T> list, int currentIndex)
        {
            if (currentIndex - 1 < 0) return (list.Count - 1);
            else
                return currentIndex - 1;
        }

        public static void DestroyChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Returns first component of type T in prefab's children
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static T GetComponentInPrefabChildren<T>(GameObject prefab) where T : Component
        {
            T[] foundComponents = prefab.GetComponentsInChildren<T>(true);
            if (foundComponents != null && foundComponents.Length > 0)
            {
                return foundComponents[0];
            }

            return null;
        }
    }
}