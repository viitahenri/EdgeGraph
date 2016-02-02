/***
Prototype class for EdgeGraph utilization
***/

using UnityEngine;
using System.Collections.Generic;
using EdgeGraph;

[RequireComponent(typeof(Graph))]
public class FacadeBuilder : MonoBehaviour
{
    // Unity can't serialize list of lists
    [System.Serializable]
    class Building
    {
        public List<BuildingFacade> building;

        public Building(List<BuildingFacade> _building)
        {
            building = _building;
        }
    }

    public bool m_buildRoof = false;
    // Facades will be moved this much inside
    public float inSet = 0f;
    public GameObject facadeStretchPrefab;
    public List<GameObject> facadePrefabs;
    public Material roofSideMaterial;
    public Material roofMiddleMaterial;
    public float roofHeight = 3f;
    public float roofMiddleAddHeight = .5f;
    public float roofAccentWidth = 1f;

    private Graph graph;
    //private BuildingFacade facadeStretch;
    private List<BuildingFacade> facadePrefabScripts;
    // Built facades
    [HideInInspector]
    [SerializeField]
    private List<BuildingFacade> facades;
    // Grouped up facades as buildings
    [HideInInspector]
    [SerializeField]
    private List<Building> buildings;
    // Nodes to use in building the facades
    [HideInInspector]
    [SerializeField]
    private List<Node> nodes;
    // Nodes used to make the roof
    [HideInInspector]
    [SerializeField]
    private List<Node> roofNodes;

    private Vector3 cursor;
    [HideInInspector]
    [SerializeField]
    private GameObject facadeParent;
    [HideInInspector]
    [SerializeField]
    private GameObject buildingParent;
    private int facadeCounter = 0;
    private float noiseStep = 1f;
    private float noiseCurrent = 0f;
    private float noiseY = 0f;

    public void BuildFacades()
    {
        if (facadePrefabs == null) return;

        Random.seed = System.Guid.NewGuid().GetHashCode();

        facades = new List<BuildingFacade>();
        facadePrefabScripts = new List<BuildingFacade>();

        graph = GetComponent<Graph>();

        List<Primitive> minimalCycles = graph.mainPrimitives.FindAll(p => p.type == Primitive.PrimitiveType.MinimalCycle);
        if (minimalCycles == null || minimalCycles.Count == 0)
        {
            Debug.Log("FacadeBuildet::Build() - No minimal cycles found!");
            return;
        }

        //GameObject facadeStretchObj = Instantiate(facadeStretchPrefab);
        //facadeStretch = facadeStretchObj.GetComponentInChildren<BuildingFacade>();
        //DestroyImmediate(facadeStretchObj);

        for (int i = 0; i < facadePrefabs.Count; i++)
        {
            GameObject facadeObj = Instantiate(facadePrefabs[i]);
            facadePrefabScripts.Add(facadeObj.GetComponentInChildren<BuildingFacade>());
            DestroyImmediate(facadeObj);
        }

        UtilityTools.Helper.DestroyChildren(transform);

        for (int i = 0; i < minimalCycles.Count; i++)
        {
            //noiseCurrent = Random.Range(0f, 100f);
            noiseY = Random.Range(0f, 100f);
            noiseCurrent = 0f;

            Primitive primitiveCopy = new Primitive(minimalCycles[i]);
            if (inSet > 0f)
            {
                primitiveCopy.edges.ForEach((e) => e.Width += inSet);
                primitiveCopy.Process();
            }

            List<Node> _nodes = primitiveCopy.nodes;

            nodes = new List<Node>();

            foreach (var n in _nodes)
            {
                nodes.Add(new Node(n));
            }

            primitiveCopy = new Primitive(minimalCycles[i]);
            primitiveCopy.edges.ForEach((e) => e.Width = roofAccentWidth + inSet);
            primitiveCopy.Process(false);

            _nodes = primitiveCopy.nodes;

            roofNodes = new List<Node>();

            foreach (var n in _nodes)
            {
                roofNodes.Add(new Node(n));
            }

            buildings = new List<Building>();

            facadeParent = new GameObject("Facades" + (i + 1).ToString());
            facadeParent.transform.SetParent(transform);
            facadeParent.transform.localPosition = Vector3.zero;

            facadeCounter = 0;

            for (int j = 0; j < nodes.Count; j++)
            {
                if (j == nodes.Count - 1)
                    BuildFromToNode(nodes[j], nodes[0]);
                else
                    BuildFromToNode(nodes[j], nodes[j + 1]);
            }

            GroupFacadesToBuildings();
        }
    }

    void BuildFromToNode(Node from, Node to)
    {
        Vector3 dirFromTo = (to.Position - from.Position).normalized;

        float space = Vector3.Distance(from.Position, to.Position);

        cursor = transform.TransformPoint(from.Position);

        GameObject facadeSide = new GameObject("FacadeSide");
        facadeSide.transform.SetParent(facadeParent.transform);
        facadeSide.transform.localScale = Vector3.one;
        facadeSide.transform.localPosition = Vector3.zero;

        int currentIndex = int.MinValue;

        for (int i = 0; i < 100f; i++)
        {
            currentIndex = GetRandomFacadeIndex(space);
            // Place facade
            GameObject facadeObj = null;
            BuildingFacade facade = null;
            if (currentIndex == -1)
            {
                facadeObj = Instantiate(facadeStretchPrefab);
                facadeObj.transform.SetParent(facadeSide.transform);
                facadeObj.transform.localPosition = Vector3.zero;
                facadeObj.transform.localScale = new Vector3(space, 1f, 1f);
            }
            else
            {
                facadeObj = Instantiate(facadePrefabs[currentIndex]);
                facadeObj.transform.SetParent(facadeSide.transform);
                facadeObj.transform.localPosition = Vector3.zero;
                facadeObj.transform.localScale = Vector3.one;
            }

            facade = facadeObj.GetComponentInChildren<BuildingFacade>();

            if (currentIndex == -1)
            {
                float width = Vector3.Distance(facade.nodes[0].Position, facade.nodes[1].Position);
                facadeObj.transform.localScale = new Vector3(space / width, 1f, 1f);
            }

            if (i == 0)
            {
                facade.isFirst = true;
                facade.nodes[0].dirToInside = from.dirToInside;
            }
            if (facade.isStretchable)
            {
                facade.nodes[1].dirToInside = to.dirToInside;
            }
            facade.ID = facadeCounter++;
            facades.Add(facade);

            facadeObj.transform.rotation = Quaternion.LookRotation(UtilityTools.MathHelper.LeftSideNormal(dirFromTo));
            // Set position in regards to the first node
            //facade.transform.position = cursor - footPrint.nodes[0].Position;
            Vector3 toCursor = cursor - facadeObj.transform.TransformPoint(facade.nodes[0].Position);
            facadeObj.transform.position += toCursor;

            if (currentIndex != -1)
            {
                float facadeWidth = Vector3.Distance(facade.nodes[0].Position, facade.nodes[1].Position);
                cursor += dirFromTo * facadeWidth;
                space -= facadeWidth;
            }
            else
                break;
        }
    }

    int GetRandomFacadeIndex(float space)
    {
        // Determine widest footprint that fits
        int index = -1;
        for (int i = facadePrefabScripts.Count - 1; i >= 0; i--)
        {
            float width = Vector3.Distance(facadePrefabScripts[i].nodes[0].Position, facadePrefabScripts[i].nodes[1].Position);
            if (space - width > 0)
            {
                index = i;
                break;
            }
        }

        // If index is still -1, only the stretchable fits
        if (index == -1)
            return index;

        float noisePart = 1f / (index + 1);
        float noise = Mathf.PerlinNoise(noiseCurrent, noiseY);
        noiseCurrent += noiseStep;
        int retval = (int)(noise / noisePart);
        return retval;
    }

    void GroupFacadesToBuildings()
    {
        buildings = new List<Building>();

        buildingParent = new GameObject("BuildingParent");
        buildingParent.transform.SetParent(transform);
        buildingParent.transform.localPosition = Vector3.zero;

        // Find all stretchables and mark them and the pieces next to them to be combined
        for (int i = 0; i < facades.Count; i++)
        {
            if (facades[i].isStretchable)
            {
                facades[i].combine = true;
                // Previous
                if (i == 0)
                    facades[facades.Count - 1].combine = true;
                else
                    facades[i - 1].combine = true;
                // Next
                if (i == facades.Count - 1)
                    facades[0].combine = true;
                else
                    facades[i + 1].combine = true;
            }
        }

        // Go through facades and make the buildings

        int idx = 0;
        int buildingCount = 0;

        for (int iter = 0; iter < 1000; iter++)
        {
            if (buildingCount >= facades.Count) break;

            // Check if this is one building
            bool nonCombinedFound = false;
            for (int i = 0; i < facades.Count; i++)
            {
                if (!facades[i].combine)
                    nonCombinedFound = true;
            }

            // Start from non-combined facade
            if (iter == 0 && nonCombinedFound)
            {
                while (facades[idx].combine)
                {
                    idx++;
                    if (idx > facades.Count)
                    {
                        break;
                    }
                }
            }

            List<BuildingFacade> _building = new List<BuildingFacade>();
            GameObject buildingObj = new GameObject("Building" + iter.ToString());
            buildingObj.transform.SetParent(buildingParent.transform);
            buildingObj.transform.localPosition = Vector3.zero;

            if (facades[idx].combine)
            {
                // Combine facades in this row
                _building.Add(facades[idx]);
                facades[idx].transform.parent.SetParent(buildingObj.transform);

                int next = idx;
                int safe = 0;

                while (safe < 1000)
                {
                    next = UtilityTools.Helper.GetNextIndex<BuildingFacade>(facades, next);
                    if (facades[next].combine)
                    {
                        _building.Add(facades[next]);
                        facades[next].transform.parent.SetParent(buildingObj.transform);
                    }
                    else
                    {
                        break;
                    }
                    safe++;
                }

                idx = next;
            }
            else
            {
                _building.Add(facades[idx]);
                facades[idx].transform.parent.SetParent(buildingObj.transform);
                if (idx + 1 == facades.Count)
                    idx = 0;
                else
                    idx++;
            }

            buildingCount += _building.Count;

            buildings.Add(new Building(_building));
        }

        DestroyImmediate(facadeParent);

        if (m_buildRoof)
            BuildRoof();
    }

    void BuildRoof()
    {
        List<Vector3> roofSideVertices = new List<Vector3>();

        // First vertices twice so it loops
        roofSideVertices.Add(nodes[0].Position + Vector3.up * roofHeight);
        roofSideVertices.Add(roofNodes[0].Position + Vector3.up * (roofHeight + roofMiddleAddHeight));

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            roofSideVertices.Add(nodes[i].Position + Vector3.up * roofHeight);
            if (i >= roofNodes.Count)
                Debug.Log("FacadeBuilder::BuildRoof() - Roof has less nodes than the building.");
            else
                roofSideVertices.Add(roofNodes[i].Position + Vector3.up * (roofHeight + roofMiddleAddHeight));
        }

        // Middle of the roof
        List<Vector3> roofMiddleVertices = new List<Vector3>();

        for (int i = roofNodes.Count - 1; i >= 0; i--)
        {
            roofMiddleVertices.Add(roofNodes[i].Position + Vector3.up * (roofHeight + roofMiddleAddHeight));
        }

        GameObject roofSide = UtilityTools.MeshTool.CreateMeshStrip(roofSideVertices, buildingParent.transform);
        roofSide.name = "RoofSide";
        if (roofSideMaterial != null)
        {
            MeshRenderer mr = roofSide.GetComponent<MeshRenderer>();
            if (mr)
                mr.sharedMaterial = roofSideMaterial;
        }

        GameObject roofMiddle = UtilityTools.MeshTool.CreateMeshFromPolygon(roofMiddleVertices, buildingParent.transform);
        roofMiddle.name = "RoofMiddle";
        if (roofMiddleMaterial != null)
        {
            MeshRenderer mr = roofMiddle.GetComponent<MeshRenderer>();
            if (mr)
                mr.sharedMaterial = roofMiddleMaterial;
        }
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;

    //    if (roofNodes != null && roofNodes.Count > 0)
    //    {
    //        for (int i = 0; i < roofNodes.Count;i++)
    //        {
    //            Node n1 = roofNodes[i];
    //            Node n2;

    //            if (i == roofNodes.Count - 1)
    //                n2 = roofNodes[0];
    //            else
    //                n2 = roofNodes[i + 1];

    //            Vector3 n1World = transform.TransformPoint(n1.Position);
    //            Vector3 n2World = transform.TransformPoint(n2.Position);

    //            Gizmos.DrawLine(n1World, n2World);
    //        }
    //    }
    //}
}
