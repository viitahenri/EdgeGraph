using UnityEngine;

namespace UtilityTools
{
    public abstract class MathHelper
    {
        public static bool PointInTriangle(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            double s = p0.z * p2.x - p0.x * p2.z + (p2.z - p0.z) * p.x + (p0.x - p2.x) * p.z;
            double t = p0.x * p1.z - p0.z * p1.x + (p0.z - p1.z) * p.x + (p1.x - p0.x) * p.z;

            if ((s < 0) != (t < 0))
                return false;

            double A = -p1.z * p2.x + p0.z * (p2.x - p1.x) + p0.x * (p1.z - p2.z) + p1.x * p2.z;
            if (A < 0.0)
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) < A;
        }

        public static Vector3 LeftSideNormal(Vector3 tangent)
        {
            return new Vector3(-tangent.z, 0f, tangent.x).normalized;
        }

        #region Line tools
        public static Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {

            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;

            return Mathf.Pow(1f - t, 3f) * p0 +
                    3f * Mathf.Pow(1f - t, 2f) * t * p1 +
                    3f * (1f - t) * Mathf.Pow(t, 2f) * p2 +
                    Mathf.Pow(t, 3f) * p3;
        }

        public static int AreIntersecting(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float errorMargin)
        {

            Vector2 t1 = p1 - p0;
            Vector2 t2 = p3 - p2;

            p0 += t1.normalized * errorMargin;
            p1 -= t1.normalized * errorMargin;

            p2 += t2.normalized * errorMargin;
            p3 -= t2.normalized * errorMargin;

            return CheckSegmentIntersection(p0.x, p0.y, p1.x, p1.y, p2.x, p2.y, p3.x, p3.y);
        }

        public static int CheckSegmentIntersection(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            float ax = x2 - x1;
            float ay = y2 - y1;
            float dx = x4 - x3;
            float dy = y4 - y3;
            float dot = ax * dy - ay * dx;

            if (Mathf.Approximately(dot, 0f))
            {
                return 0;
            }

            float cx = x3 - x1;
            float cy = y3 - y1;
            float t = (cx * dy - cy * dx) / dot;

            if (t < 0f || t > 1f)
            {
                return 0;
            }

            float u = (cx * ay - cy * ax) / dot;

            if (u < 0f || u > 1f)
            {
                return 0;
            }

            return 1;//new Vector2(x1+t*bx, y1+t*by);
        }

        public static int AreIntersecting(out Vector3 interSectPoint, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return CheckSegmentIntersection(out interSectPoint, p0.x, p0.z, p1.x, p1.z, p2.x, p2.z, p3.x, p3.z);
        }

        public static int AreIntersecting(out Vector3 interSectPoint, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {

            //Vector2 t1 = p1-p0;
            //Vector2 t2 = p3-p2;

            /*
            p0+=t1.normalized*deltaError;
            p1-=t1.normalized*deltaError;
		
            p2+=t2.normalized*deltaError;
            p3-=t2.normalized*deltaError;
            */

            return CheckSegmentIntersection(out interSectPoint, p0.x, p0.y, p1.x, p1.y, p2.x, p2.y, p3.x, p3.y);
        }

        public static int CheckSegmentIntersection(out Vector3 interSectPoint, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            interSectPoint = Vector2.zero;

            float ax = x2 - x1;
            float ay = y2 - y1;
            float dx = x4 - x3;
            float dy = y4 - y3;
            float dot = ax * dy - ay * dx;

            if (Mathf.Approximately(dot, 0f))
            {
                return 0;
            }

            float cx = x3 - x1;
            float cy = y3 - y1;
            float t = (cx * dy - cy * dx) / dot;

            if (t < 0f || t > 1f)
            {
                return 0;
            }

            float u = (cx * ay - cy * ax) / dot;

            if (u < 0f || u > 1f)
            {
                return 0;
            }

            interSectPoint = new Vector2(x1 + t * ax, y1 + t * ay);

            return 1;
        }
        #endregion

    }
}