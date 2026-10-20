using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MinhaVila.Unity
{
    public sealed class MeshAccumulator
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        public int VertexCount { get { return _vertices.Count; } }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            int first = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
            _triangles.Add(first);
            _triangles.Add(first + 2);
            _triangles.Add(first + 3);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int first = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
        }

        public void AddBox(Vector3 center, Vector3 size, Color color)
        {
            float x = size.x * 0.5f;
            float y = size.y * 0.5f;
            float z = size.z * 0.5f;
            Vector3 p000 = center + new Vector3(-x, -y, -z);
            Vector3 p001 = center + new Vector3(-x, -y, z);
            Vector3 p010 = center + new Vector3(-x, y, -z);
            Vector3 p011 = center + new Vector3(-x, y, z);
            Vector3 p100 = center + new Vector3(x, -y, -z);
            Vector3 p101 = center + new Vector3(x, -y, z);
            Vector3 p110 = center + new Vector3(x, y, -z);
            Vector3 p111 = center + new Vector3(x, y, z);

            AddQuad(p000, p010, p110, p100, color);
            AddQuad(p001, p101, p111, p011, color);
            AddQuad(p000, p001, p011, p010, color);
            AddQuad(p100, p110, p111, p101, color);
            AddQuad(p010, p011, p111, p110, color);
            AddQuad(p000, p100, p101, p001, color);
        }

        public void AddGableRoof(Vector3 center, float width, float depth, float eavesY, float ridgeY, Color roofColor, Color gableColor)
        {
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;
            Vector3 leftFront = new Vector3(center.x - halfWidth, eavesY, center.z - halfDepth);
            Vector3 leftBack = new Vector3(center.x - halfWidth, eavesY, center.z + halfDepth);
            Vector3 rightFront = new Vector3(center.x + halfWidth, eavesY, center.z - halfDepth);
            Vector3 rightBack = new Vector3(center.x + halfWidth, eavesY, center.z + halfDepth);
            Vector3 ridgeFront = new Vector3(center.x, ridgeY, center.z - halfDepth);
            Vector3 ridgeBack = new Vector3(center.x, ridgeY, center.z + halfDepth);

            AddQuad(leftBack, ridgeBack, ridgeFront, leftFront, roofColor);
            AddQuad(rightFront, ridgeFront, ridgeBack, rightBack, roofColor);
            AddTriangle(leftFront, ridgeFront, rightFront, gableColor);
            AddTriangle(rightBack, ridgeBack, leftBack, gableColor);
            AddBox(new Vector3(center.x, ridgeY + 0.015f, center.z),
                new Vector3(0.06f, 0.05f, depth + 0.08f), Color.Lerp(roofColor, Color.black, 0.18f));
        }

        public void AddCylinder(Vector3 center, float radius, float height, int segments, Color color)
        {
            int count = Mathf.Max(5, segments);
            float bottom = center.y - height * 0.5f;
            float top = center.y + height * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float angleA = (Mathf.PI * 2 * i) / count;
                float angleB = (Mathf.PI * 2 * (i + 1)) / count;
                Vector3 bottomA = new Vector3(center.x + Mathf.Cos(angleA) * radius, bottom, center.z + Mathf.Sin(angleA) * radius);
                Vector3 bottomB = new Vector3(center.x + Mathf.Cos(angleB) * radius, bottom, center.z + Mathf.Sin(angleB) * radius);
                Vector3 topA = new Vector3(bottomA.x, top, bottomA.z);
                Vector3 topB = new Vector3(bottomB.x, top, bottomB.z);
                AddQuad(bottomA, topA, topB, bottomB, color);
                AddTriangle(new Vector3(center.x, top, center.z), topB, topA, color);
                AddTriangle(new Vector3(center.x, bottom, center.z), bottomA, bottomB, color);
            }
        }

        public void AddCone(Vector3 center, float radius, float height, int segments, Color color)
        {
            int count = Mathf.Max(5, segments);
            Vector3 tip = center + Vector3.up * (height * 0.5f);
            float bottom = center.y - height * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float angleA = (Mathf.PI * 2 * i) / count;
                float angleB = (Mathf.PI * 2 * (i + 1)) / count;
                Vector3 a = new Vector3(center.x + Mathf.Cos(angleA) * radius, bottom, center.z + Mathf.Sin(angleA) * radius);
                Vector3 b = new Vector3(center.x + Mathf.Cos(angleB) * radius, bottom, center.z + Mathf.Sin(angleB) * radius);
                AddTriangle(a, tip, b, color);
            }
        }

        public void AddSphere(Vector3 center, Vector3 scale, int segments, Color color)
        {
            int longitudes = Mathf.Max(5, segments);
            int latitudes = Mathf.Max(3, segments / 2);
            for (int y = 0; y < latitudes; y++)
            {
                float v0 = y / (float)latitudes;
                float v1 = (y + 1) / (float)latitudes;
                float phi0 = Mathf.PI * v0;
                float phi1 = Mathf.PI * v1;
                for (int x = 0; x < longitudes; x++)
                {
                    float u0 = x / (float)longitudes;
                    float u1 = (x + 1) / (float)longitudes;
                    Vector3 a = SpherePoint(center, scale, phi0, u0 * Mathf.PI * 2);
                    Vector3 b = SpherePoint(center, scale, phi0, u1 * Mathf.PI * 2);
                    Vector3 c = SpherePoint(center, scale, phi1, u1 * Mathf.PI * 2);
                    Vector3 d = SpherePoint(center, scale, phi1, u0 * Mathf.PI * 2);
                    AddQuad(a, b, c, d, color);
                }
            }
        }

        public Mesh CreateMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            if (_vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 SpherePoint(Vector3 center, Vector3 scale, float phi, float theta)
        {
            float sinPhi = Mathf.Sin(phi);
            return center + new Vector3(
                Mathf.Cos(theta) * sinPhi * scale.x,
                Mathf.Cos(phi) * scale.y,
                Mathf.Sin(theta) * sinPhi * scale.z);
        }
    }
}
