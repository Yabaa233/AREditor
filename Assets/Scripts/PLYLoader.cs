using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BinaryPLYLoader : MonoBehaviour
{
    public string plyFilePath = "path/to/your/file.ply";

    void Start()
    {
        Mesh mesh = LoadBinaryPLYFile(plyFilePath);
        if (mesh != null)
        {
            GameObject meshObject = new GameObject("PLY Mesh");
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }
    }

    Mesh LoadBinaryPLYFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("PLY文件不存在: " + filePath);
            return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // 读取文件头
            string line;
            int vertexCount = 0;
            int faceCount = 0;

            while ((line = ReadLine(reader)) != "end_header")
            {
                if (line.StartsWith("element vertex"))
                {
                    vertexCount = int.Parse(line.Split(' ')[2]);
                }
                else if (line.StartsWith("element face"))
                {
                    faceCount = int.Parse(line.Split(' ')[2]);
                }
            }

            // 读取顶点数据
            for (int i = 0; i < vertexCount; i++)
            {
                double x = reader.ReadDouble();
                double y = reader.ReadDouble();
                double z = reader.ReadDouble();
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();

                vertices.Add(new Vector3((float)x, (float)y, (float)z));
                colors.Add(new Color(r / 255f, g / 255f, b / 255f));
            }

            // 读取面数据
            for (int i = 0; i < faceCount; i++)
            {
                byte vertexCountPerFace = reader.ReadByte();
                for (int j = 0; j < vertexCountPerFace; j++)
                {
                    uint vertexIndex = reader.ReadUInt32();
                    triangles.Add((int)vertexIndex);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    string ReadLine(BinaryReader reader)
    {
        List<byte> lineBytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0x0A) // 0x0A 是换行符
        {
            lineBytes.Add(b);
        }
        return System.Text.Encoding.ASCII.GetString(lineBytes.ToArray());
    }
}