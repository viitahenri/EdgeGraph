using UnityEngine;
using System.Collections.Generic;

namespace UtilityTools
{
    public class MeshTool
    {
        private struct MeshObject
        {
            public GameObject parent;
            public MeshFilter mf;
            public MeshRenderer mr;

            public MeshObject(GameObject _parent, MeshFilter _mf, MeshRenderer _mr)
            {
                parent = _parent;
                mf = _mf;
                mr = _mr;
            }
        }

        private static MeshObject InitTransformForMesh(Transform transform)
        {
            GameObject parentObject = new GameObject("Polygon");
            parentObject.transform.parent = transform;
            parentObject.transform.localPosition = Vector3.zero;

            MeshFilter mf = Helper.EnsureComponent<MeshFilter>(parentObject);
            MeshRenderer mr = Helper.EnsureComponent<MeshRenderer>(parentObject);

            return new MeshObject(parentObject, mf, mr);
        }

        public static GameObject CreateMeshFromPolygon(List<Vector3> vertices, Transform transform, bool debugMode = false)
        {
            MeshObject meshObject = InitTransformForMesh(transform);

            if (debugMode)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    Debug.Log("Vertice[" + i.ToString() + "] position (" + vertices[i].ToString() + ")");
                }
            }

            // UVs
            List<Vector2> uvs = new List<Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs.Add(new Vector2(vertices[i].x, vertices[i].y));
            }

            List<int> triangles = new List<int>();

            bool[] availableVertices = new bool[vertices.Count];
            for (int i = 0; i < availableVertices.Length; i++) availableVertices[i] = true;

            // cursors pointing to vertices list
            int n0 = 0;
            int n1 = 1;
            int n2 = 2;
            int verticesLeft = vertices.Count;
            int safeCounter = verticesLeft * 10;
            while (verticesLeft > 0)
            {
                safeCounter--;
                if (safeCounter < 0)
                {
                    Debug.LogError("MeshTool::CreateMeshFromPolygon safe counter reached in while loop");
                    break;
                }

                bool canCreateTriangle = true;
                for (int i = 0; i < vertices.Count; i++)
                {
                    if (n0 == i || n1 == i || n2 == i) continue;
                    if (MathHelper.PointInTriangle(vertices[i], vertices[n0], vertices[n1], vertices[n2]))
                    {
                        canCreateTriangle = false;
                        break;
                    }
                }

                if (canCreateTriangle)
                {
                    Vector3 line = vertices[n1] - vertices[n0];
                    Vector3 tangent = (line + (vertices[n2] - vertices[n1]));
                    Vector3 normal = MathHelper.LeftSideNormal(tangent);

                    float angle = Vector3.Angle(-line, normal);
                    canCreateTriangle = angle > 90f;
                }

                if (!canCreateTriangle)
                {
                    // Cant create triangle, move cursors to next available positions
                    n0 = n1;
                    n1 = n2;
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.Log(string.Format("Triangle [{0},{1},{2}]", n0, n1, n2));

                        Vector3 offset = transform.position + Vector3.up * 3f;

                        Debug.DrawLine(vertices[n0] + offset,
                                       vertices[n1] + offset, Color.red);
                        Debug.DrawLine(vertices[n1] + offset,
                                       vertices[n2] + offset, Color.red);
                        Debug.DrawLine(vertices[n2] + offset,
                                       vertices[n0] + offset, Color.red);
                    }

                    triangles.Add(n0);
                    triangles.Add(n1);
                    triangles.Add(n2);

                    verticesLeft--;
                    if (verticesLeft <= 2)
                    {
                        if (debugMode) Debug.Log("MeshTool::CreateMeshFromPolygon done creating");
                        break;
                    }
                    availableVertices[n1] = false;
                    n1 = n2;
                }


                int cursor = n1;
                int safeCounter2 = vertices.Count;
                while (n1 == n2)
                {
                    safeCounter2--;
                    if (safeCounter2 < 0)
                    {
                        Debug.LogError("MeshTool::CreateMeshFromPolygon safe counter 2 reached in while loop");
                        break;
                    }

                    cursor++;
                    if (cursor >= vertices.Count) cursor = 0;

                    if (availableVertices[cursor] && n0 != cursor && n1 != cursor)
                    {
                        // found next available vertex
                        if (debugMode) Debug.Log("Found new available vertex [" + cursor.ToString() + "]");
                        n2 = cursor;
                        break;
                    }
                }
            }

            if (!debugMode)
            {
                Mesh mesh = new Mesh();
                mesh.name = transform.name;
                mesh.vertices = vertices.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.triangles = triangles.ToArray();

                mesh.RecalculateNormals();
                mesh.Optimize();

                meshObject.mf.sharedMesh = mesh;
            }

            return meshObject.parent;
        }

        public static GameObject CreateMeshStrip(List<Vector3> vertices, Transform transform, bool debugMode = false)
        {
            MeshObject meshObject = InitTransformForMesh(transform);

            // UVs
            List<Vector2> uvs = new List<Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs.Add(new Vector2(vertices[i].x, vertices[i].y));
            }

            List<int> triangles = new List<int>();

            for (int i = 0; i < vertices.Count - 2; i++)
            {
                triangles.Add(i);

                if (i % 2 == 0)
                {
                    triangles.Add(i + 2);
                    triangles.Add(i + 1);
                }
                else
                {
                    triangles.Add(i + 1);
                    triangles.Add(i + 2);
                }
            }

            if (!debugMode)
            {
                Mesh mesh = new Mesh();
                mesh.name = transform.name;
                mesh.vertices = vertices.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.triangles = triangles.ToArray();

                mesh.RecalculateNormals();
                mesh.Optimize();

                meshObject.mf.sharedMesh = mesh;
            }

            return meshObject.parent;
        }
    }
}