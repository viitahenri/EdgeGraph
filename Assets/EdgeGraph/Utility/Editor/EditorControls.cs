using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UtilityTools
{
    public class EditorControls
    {
        //Size of the gizmos bar on top of the scene view
        static readonly float SCENE_GIZMOS_SIZE = 78.0f;

        public GameObject targetObj;

        //Is mouse button held down
        public bool mouseIsPressed;
        //Is shift pressed
        public bool shiftIsPressed;
        //Is control pressed
        public bool controlIsPressed;
        //Are control and shift both pressed
        public bool controlAndShiftIsPressed;

        //Mouse cursor's world position
        public Vector3 cursorWorldPosition;
        //Mouse cursor's local position
        public Vector3 cursorLocalPosition;

        public EditorControls() { }

        public EditorControls(GameObject _target)
        {
            targetObj = _target;
        }

        public void Update()
        {
            UpdateModifierStates();

            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }

            UpdateMouseState();
        }

        public bool MouseClickedDown()
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;

            // check if we're not playing with gizmos
            Vector2 mousePos = Event.current.mousePosition;
            if (mousePos.x >= Screen.width - SCENE_GIZMOS_SIZE && mousePos.y <= SCENE_GIZMOS_SIZE) return false;

            mouseIsPressed = true;
            return true;
        }

        public bool MouseClickedUp()
        {
            if (Event.current.type != EventType.MouseUp || Event.current.button != 0) return false;

            // check if we're not playing with gizmos
            Vector2 mousePos = Event.current.mousePosition;
            if (mousePos.x >= Screen.width - SCENE_GIZMOS_SIZE && mousePos.y <= SCENE_GIZMOS_SIZE) return false;
            return true;
        }

        void UpdateModifierStates()
        {
            shiftIsPressed = Event.current.shift;
            controlIsPressed = Event.current.control;

            if (shiftIsPressed && controlIsPressed)
            {
                controlAndShiftIsPressed = true;
            }
            else if (!shiftIsPressed && !controlIsPressed)
            {
                controlAndShiftIsPressed = false;
            }
        }

        void UpdateMouseState()
        {
            MouseClickedDown();
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                mouseIsPressed = false;
            }

            UpdateMousePosition();
        }

        void UpdateMousePosition()
        {
            Vector3 planeOffset = targetObj.transform.position;
            Plane xzPlane = new Plane(Vector3.up, planeOffset);

            Vector2 screenCursorPos = Event.current.mousePosition;
            Ray cursorRay = HandleUtility.GUIPointToWorldRay(screenCursorPos);

            float enterDistance;
            if (xzPlane.Raycast(cursorRay, out enterDistance))
            {
                Vector3 initCursorPos = cursorWorldPosition;
                cursorLocalPosition = cursorRay.GetPoint(enterDistance) - planeOffset;

                cursorWorldPosition = cursorRay.GetPoint(enterDistance);

                if (initCursorPos != cursorWorldPosition)
                {
                    SceneView.RepaintAll();
                }
            }
        }
    }

    /// <summary>
    /// Source: https://gist.github.com/benblo/10732554
    /// </summary>
    public class EditorCoroutine
    {
        public static EditorCoroutine start(IEnumerator _routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(_routine);
            coroutine.start();
            return coroutine;
        }

        readonly IEnumerator routine;
        EditorCoroutine(IEnumerator _routine)
        {
            routine = _routine;
        }

        void start()
        {
            //Debug.Log("start");
            EditorApplication.update += update;
        }
        public void stop()
        {
            //Debug.Log("stop");
            EditorApplication.update -= update;
        }

        void update()
        {
            /* NOTE: no need to try/catch MoveNext,
			 * if an IEnumerator throws its next iteration returns false.
			 * Also, Unity probably catches when calling EditorApplication.update.
			 */

            //Debug.Log("update");
            if (!routine.MoveNext())
            {
                stop();
            }
        }
    }
}