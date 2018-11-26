using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshGenerator : MonoBehaviour {

    //private MeshRenderer wallRenderer;
    public SquareGrid squareGrid;
    public MeshFilter walls;
    public MeshFilter cave;
    public GameObject Torch;
    //public MeshFilter floor;
    public bool is2D;
    List<Vector3> vertices;
    List<int> triangles;
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();


    List<Vector3> wallVerticesForGizmos;



    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

    public void GenerateMesh(int[,] map, float squareSize, float wallHeight)
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentZ = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentZ);
        }
        mesh.uv = uvs;

        if (!is2D)
        {
            CreateWallMesh(map, squareSize, wallHeight);
        }

        //generateTorches(vertices);

        //wallRenderer = GameObject.Find("Walls").GetComponent<MeshRenderer>();
        //wallRenderer.shadowCastingMode.Equals(ShadowCastingMode.TwoSided);
    }

    void CreateWallMesh(int[,] map, float squareSize, float wallHeight)
    {
        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        List<int> topIndices = new List<int>();
        List<int> bottomIndices = new List<int>();
        Mesh wallMesh = new Mesh();

        
        //float wallHeight = 8;

        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left
                topIndices.Add(wallVertices.Count);
                wallVertices.Add(vertices[outline[i + 1]]); // right
                topIndices.Add(wallVertices.Count);
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                bottomIndices.Add(wallVertices.Count);

                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right
                bottomIndices.Add(wallVertices.Count);

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

        //UV's for wall tiles
        int tileAmount = 5;
        Vector2[] wallUVs = new Vector2[wallVertices.Count];
        for (int i = 0; i < wallVertices.Count; i+=4)
        {
            float u0, v0, u1, v1, u2, v2, u3, v3;
            v0 = 0;
            v1 = 0;
            v2 = 7;
            v3 = 7;
            //U coordinate is x,z wallvert[0] has a U of 0
            //wallvert[1] has a U of 

            //float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, wallVertices[i].x) * tileAmount;
            //float percentY = Mathf.InverseLerp(-wallHeight / 2, wallHeight / 2, wallVertices[i].y) * tileAmount;

            if(i == 0)
            {
                u0 = 0;
                u2 = 0;
            }
            else
            {
                u0 = wallUVs[i - 2].x;
                u2 = wallUVs[i - 4].x;
            }

            //float distance = (wallVertices[i + 5] - wallVertices[i]).magnitude;
            //float distance = 1;
            //float distance = wallVertices[i + 1].x
            //u1 = (u0 + distance);
            //u3 = (u2 + distance);

            u1 = 1;
            u3 = 1;
 
            //float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, wallVertices[i].x) * tileAmount;
            //float percentZ = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, wallVertices[i].z) * tileAmount;
            wallUVs[i] = new Vector2(u0, v0);
            wallUVs[i + 1] = new Vector2(u1, v1);
            wallUVs[i + 2] = new Vector2(u2, v2);
            wallUVs[i + 3] = new Vector2(u3, v3);
        }
        wallMesh.uv = wallUVs;

        generateTorches(wallVertices);

        MeshCollider wallCollider = new MeshCollider();
        if(walls.gameObject.GetComponent<MeshCollider>() != null)
        {
            Destroy(walls.gameObject.GetComponent<MeshCollider>());
        }
        wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;

        wallVerticesForGizmos = wallVertices;
    }

    void generateTorches(List<Vector3> wallVertices)
    {
        int verticesSinceLastTorch = 0;
        int randomTorchLimit = 0;
        System.Random pseudoRandom = new System.Random(1);

        for(int i = 0; i < wallVertices.Count; i+=4)
        {
            if(verticesSinceLastTorch == randomTorchLimit)
            {
                //Debug.Log("Vertex " + i + ": " + wallVertices[i].ToString());
                randomTorchLimit = pseudoRandom.Next(14, 35);
                Vector3 newPos = wallVertices[i];

                //Vector3 dir = wallVertices[i] - wallVertices[i - 4];
                //obj.rotation = Quaternion.FromToRotation(obj.up, dir) * obj.rotation;
                if (i == 0)
                {

                }
                else
                {
                    newPos = Vector3.Lerp(wallVertices[i], wallVertices[i - 4], 0.5f);

                    //Vector3 abs1 = new Vector3(Mathf.Abs(wallVertices[i].x), Mathf.Abs(wallVertices[i].y), Mathf.Abs(wallVertices[i].z));
                    //Vector3 abs2 = new Vector3(Mathf.Abs(wallVertices[i - 4].x), Mathf.Abs(wallVertices[i - 4].y), Mathf.Abs(wallVertices[i - 4].z));

                    //Vector3 difference =abs1 - abs2;
                    Vector3 difference = wallVertices[i] - wallVertices[i - 4];
                    Debug.Log(difference);

                    newPos.z += 3.55f;
                    newPos.x -= 2.15f;
                    newPos.y -= .5f;

                    Quaternion testRotation = Quaternion.Euler(new Vector3(17, 0, 0));
                    Quaternion torchRotation = determineTorchRotation(difference);
                    GameObject torch = (GameObject)Instantiate(Torch, newPos, torchRotation);


                    verticesSinceLastTorch = 0;
                }   

            }
            else
            {
                verticesSinceLastTorch++;
            }

        }
    }

    Quaternion determineTorchRotation(Vector3 difference)
    {
       if(difference == new Vector3(1f, 0f, 0f))
        {
            //Debug.Log("Reached 1");
            return Quaternion.Euler(new Vector3(17, 0, 0));
        }
       else if(difference == new Vector3(-1f, 0f, 0f))
        {
            //Debug.Log("Reached 2");
            return Quaternion.Euler(new Vector3(17, 180, 0));
        }
        else if (difference == new Vector3(0f, 0f, 1f))
        {
            //Debug.Log("Reached 3");
            return Quaternion.Euler(new Vector3(17, 270, 0));
        }
        else if (difference == new Vector3(0f, 0f, -1f))
        {
            //Debug.Log("Reached 4");
            return Quaternion.Euler(new Vector3(17, 90, 0));
        }
        else if (difference == new Vector3(0.5f, 0f, -0.5f))
        {
            //Debug.Log("Reached 5");
            return Quaternion.Euler(new Vector3(17, 45, 0));
        }
        else if (difference == new Vector3(-0.5f, 0f, 0.5f))
        {
            //Debug.Log("Reached 6");
            return Quaternion.Euler(new Vector3(17, 225, 0));
        }
        else if (difference == new Vector3(0.5f, 0f, 0.5f))
        {
            //Debug.Log("Reached 7");
            return Quaternion.Euler(new Vector3(17, 315, 0));
        }
        else if (difference == new Vector3(-0.5f, 0f, -0.5f))
        {
            //Debug.Log("Reached 8");
            return Quaternion.Euler(new Vector3(17, 135, 0));
        }
        else
        {
            Debug.Log("ERROR SHUOLD NEVER REACH THIS ERROR");
            return Quaternion.Euler(new Vector3(17, 0, 0)); ;
        }

    }

    void TriangulateSquare(Square square)
    {
        switch(square.configuration)
        {
            case 0:
                break;

            //1 point cases
            case 1:
                MeshFromPoints(square.midLeft, square.midBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.midBottom, square.midRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.midRight, square.midTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.midTop, square.midLeft);
                break;

            //2 point cases
            case 3:
                MeshFromPoints(square.midRight, square.bottomRight, square.bottomLeft, square.midLeft);
                break;
            case 6:
                MeshFromPoints(square.midTop, square.topRight, square.bottomRight, square.midBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.midTop, square.midBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.midRight, square.midLeft);
                break;
            case 5:
                MeshFromPoints(square.midTop, square.topRight, square.midRight, square.midBottom, square.bottomLeft, square.midLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.midTop, square.midRight, square.bottomRight, square.midBottom, square.midLeft);
                break;

            //3 point cases
            case 7:
                MeshFromPoints(square.midTop, square.topRight, square.bottomRight, square.bottomLeft, square.midLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.midTop, square.midRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.midRight, square.midBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.midBottom, square.midLeft);
                break;
            
            //4 point case
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertex);
                checkedVertices.Add(square.topRight.vertex);
                checkedVertices.Add(square.bottomRight.vertex);
                checkedVertices.Add(square.bottomLeft.vertex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
        {
            CreateTriangle(points[0], points[1], points[2]);
        }
        if(points.Length >= 4)
        {
            CreateTriangle(points[0], points[2], points[3]);
        }
        if (points.Length >= 5)
        {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if (points.Length >= 6)
        {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }

    void AssignVertices(Node[] points)
    {
        for(int i = 0; i < points.Length; i++)
        {
            if (points[i].vertex == -1)
            {
                points[i].vertex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertex);
        triangles.Add(b.vertex);
        triangles.Add(c.vertex);

        Triangle triangle = new Triangle(a.vertex, b.vertex, c.vertex);
        addTriangleToDictionary(triangle.vertexIndexA, triangle);
        addTriangleToDictionary(triangle.vertexIndexB, triangle);
        addTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void addTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if(triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines()
    {

        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    /*void OnDrawGizmos()
    {
        if (squareGrid != null)
        {
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {

                    Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);


                    Gizmos.color = Color.grey;
                    Gizmos.DrawCube(squareGrid.squares[x, y].midTop.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].midRight.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].midBottom.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].midLeft.position, Vector3.one * .15f);

                }
            }
        }
    }*/

    /*void OnDrawGizmos()
    {
        int verticesSinceLastTorch = 0;
        int randomTorchLimit = 0;
        System.Random pseudoRandom = new System.Random(1);

        for(int i = 0; i<wallVerticesForGizmos.Count; i+=1)
        {
            if(verticesSinceLastTorch == randomTorchLimit)
            {
                //randomTorchLimit = pseudoRandom.Next(7, 12);
                Vector3 newPos = wallVerticesForGizmos[i];
                newPos.z += 3.6f;
                newPos.x -= 2.15f;
                Gizmos.DrawCube(newPos, Vector3.one*.5f);
                verticesSinceLastTorch = 0;
            }
            else
            {
                verticesSinceLastTorch++;
            }

        }
    }*/

    public class SquareGrid {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize) {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node midTop, midRight, midBottom, midLeft;
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            midTop = topLeft.right;
            midRight = bottomRight.above;
            midBottom = bottomLeft.right;
            midLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

	public class Node {
        public Vector3 position;
        public int vertex = -1;

        public Node(Vector3 pos) {
            position = pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 pos, bool active, float squareSize) : base(pos) {
            this.active = active;
            above = new Node(pos + Vector3.forward * squareSize / 2f);
            right = new Node(pos + Vector3.right * squareSize / 2f);
        }
    }
}
