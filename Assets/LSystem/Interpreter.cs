/***
Virtual interpreter for the L-System
An Example to abstract turtle builders etc.
***/

using UnityEngine;

namespace LSystem
{
    public class Interpreter : MonoBehaviour
    {
        #region L-System parameters
        public float segmentSize;
        public float angle;
        public float snapSize;
        #endregion

        #region L-System actions
        public virtual void EmptyValues()
        {

        }

        public virtual void Forward()
        {

        }

        public virtual void TurnRight()
        {

        }

        public virtual void TurnLeft()
        {

        }

        public virtual void TurnRandom()
        {

        }

        public virtual void PopPosition()
        {

        }

        public virtual void PushPosition()
        {

        }
        #endregion
    }
}