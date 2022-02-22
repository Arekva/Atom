using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class WavefrontLoader
{
    private struct MeshStruct<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        public Vector3D<float>[] Vertices;
        public Vector3D<float>[] Normals;
        public Vector2D<float>[] Uv;
        public T[] Triangles;
        public Vector3D<float>[] FaceData;
        public String FileName;
    }

    // Use this for initialization
    public static (GVertex[] vertex, T[] triangles) ImportFile<T>(string filePath)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException();
        MeshStruct<T> new_mesh = CreateMeshStruct<T>(filePath);
        PopulateMeshStruct(ref new_mesh);

        Vector3D<float>[] new_verts = new Vector3D<float>[new_mesh.FaceData.Length];
        Vector2D<float>[] new_uvs = new Vector2D<float>[new_mesh.FaceData.Length];
        Vector3D<float>[] new_normals = new Vector3D<float>[new_mesh.FaceData.Length];
        int i = 0;
        /* The following foreach loops through the face data and assigns the appropriate vertex, uv, or normal
     * for the appropriate Unity mesh array.
     */
        foreach (Vector3D<float> v in new_mesh.FaceData)
        {
            new_verts[i] = new_mesh.Vertices[(Int32)v.X - 1];
            if (v.Y >= 1)
            {
                new_uvs[i] = new_mesh.Uv[(Int32)v.Y - 1];
            }

            if (v.Z >= 1)
            {
                new_normals[i] = new_mesh.Normals[(Int32)v.Z - 1];
            }

            i++;
        }


        GVertex[] vertices = new GVertex[new_verts.Length];

        for (int j = 0; j < vertices.Length; j++)
            vertices[j] = new GVertex()
            {
                Position = new_verts[j],
                Normal = new_normals[j],
                UV = new_uvs[j]
            };

        return (vertices, new_mesh.Triangles);
    }

    private static MeshStruct<T> CreateMeshStruct<T>(String filename)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        Int32 triangles = 0;
        Int32 vertices = 0;
        Int32 vt = 0;
        Int32 vn = 0;
        Int32 face = 0;
        MeshStruct<T> mesh = new MeshStruct<T> { FileName = filename };
        StreamReader stream = File.OpenText(filename);
        String entireText = stream.ReadToEnd();
        stream.Close();
        using (StringReader reader = new StringReader(entireText))
        {
            String currentText = reader.ReadLine();
            Char[] splitIdentifier = { ' ' };
            while (currentText != null)
            {
                if (!currentText.StartsWith("f ") && !currentText.StartsWith("v ") && !currentText.StartsWith("vt ")
                    && !currentText.StartsWith("vn "))
                {
                    currentText = reader.ReadLine();
                    currentText = currentText?.Replace("  ", " ");
                }
                else
                {
                    currentText = currentText.Trim(); //Trim the current line
                    String[] brokenString = currentText.Split(splitIdentifier, 50);
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (brokenString[0])
                    {
                        case "v":
                            vertices++;
                            break;
                        case "vt":
                            vt++;
                            break;
                        case "vn":
                            vn++;
                            break;
                        case "f":
                            face = face + brokenString.Length - 1;
                            triangles +=
                                3 * (brokenString.Length -
                                     2); /*brokenString.Length is 3 or greater since a face must have at least
                                                                                     3 vertices.  For each additional vertex, there is an additional
                                                                                     triangle in the mesh (hence this formula).*/
                            break;
                    }

                    currentText = reader.ReadLine();
                    currentText = currentText?.Replace("  ", " ");
                }
            }
        }

        mesh.Triangles = new T[triangles];
        mesh.Vertices = new Vector3D<float>[vertices];
        mesh.Uv = new Vector2D<float>[vt];
        mesh.Normals = new Vector3D<float>[vn];
        mesh.FaceData = new Vector3D<float>[face];
        return mesh;
    }

    private static void PopulateMeshStruct<T>(ref MeshStruct<T> mesh)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        StreamReader stream = File.OpenText(mesh.FileName);
        String entireText = stream.ReadToEnd();
        stream.Close();
        using (StringReader reader = new StringReader(entireText))
        {
            String currentText = reader.ReadLine();

            Char[] splitIdentifier = { ' ' };
            Char[] splitIdentifier2 = { '/' };
            UInt32 f = 0;
            T f2 = Scalar<T>.Zero;
            UInt32 v = 0;
            UInt32 vn = 0;
            UInt32 vt = 0;
            UInt32 vt1 = 0;
            UInt32 vt2 = 0;
            while (currentText != null)
            {
                if (!currentText.StartsWith("f ") && !currentText.StartsWith("v ") && !currentText.StartsWith("vt ") &&
                    !currentText.StartsWith("vn ") && !currentText.StartsWith("g ") &&
                    !currentText.StartsWith("usemtl ") &&
                    !currentText.StartsWith("mtllib ") && !currentText.StartsWith("vt1 ") &&
                    !currentText.StartsWith("vt2 ") &&
                    !currentText.StartsWith("vc ") && !currentText.StartsWith("usemap "))
                {
                    currentText = reader.ReadLine();
                    currentText = currentText?.Replace("  ", " ");
                }
                else
                {
                    currentText = currentText.Trim();
                    String[] brokenString = currentText.Split(splitIdentifier, 50);
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (brokenString[0])
                    {
                        case "g":
                            break;
                        case "usemtl":
                            break;
                        case "usemap":
                            break;
                        case "mtllib":
                            break;
                        case "v":
                            mesh.Vertices[v] = new Vector3D<float>(Convert.ToSingle(brokenString[1]),
                                Convert.ToSingle(brokenString[2]),
                                Convert.ToSingle(brokenString[3]));
                            v++;
                            break;
                        case "vt":
                            mesh.Uv[vt] = new Vector2D<float>(Convert.ToSingle(brokenString[1]),
                                Convert.ToSingle(brokenString[2]));
                            vt++;
                            break;
                        case "vt1":
                            mesh.Uv[vt1] = new Vector2D<float>(Convert.ToSingle(brokenString[1]),
                                Convert.ToSingle(brokenString[2]));
                            vt1++;
                            break;
                        case "vt2":
                            mesh.Uv[vt2] = new Vector2D<float>(Convert.ToSingle(brokenString[1]),
                                Convert.ToSingle(brokenString[2]));
                            vt2++;
                            break;
                        case "vn":
                            mesh.Normals[vn] = new Vector3D<float>(Convert.ToSingle(brokenString[1]),
                                Convert.ToSingle(brokenString[2]),
                                Convert.ToSingle(brokenString[3]));
                            vn++;

                            break;
                        case "vc":
                            break;
                        case "f":

                            Int32 j = 1;
                            List<T> intArray = new List<T>();
                            while (j < brokenString.Length && ("" + brokenString[j]).Length > 0)
                            {
                                Vector3D<float> temp = new Vector3D<float>();
                                String[] brokenBrokenString = brokenString[j].Split(splitIdentifier2, 3);
                                temp.X = Convert.ToUInt32(brokenBrokenString[0]);
                                if (brokenBrokenString.Length > 1) //Some .obj files skip UV and normal
                                {
                                    if (brokenBrokenString[1] != "") //Some .obj files skip the uv and not the normal
                                    {
                                        temp.Y = Convert.ToInt32(brokenBrokenString[1]);
                                    }

                                    temp.Z = Convert.ToInt32(brokenBrokenString[2]);
                                }

                                j++;

                                mesh.FaceData[Scalar.As<T, int>(f2)] = temp;
                                intArray.Add(f2);
                                f2 = Scalar.Add(f2, Scalar<T>.One);
                            }

                            j = 1;
                            while
                                (j + 2 < brokenString
                                    .Length) //Create triangles out of the face data.  There will generally be more than 1 triangle per face.
                            {
                                mesh.Triangles[f] = intArray[0];
                                f++;
                                mesh.Triangles[f] = intArray[j];
                                f++;
                                mesh.Triangles[f] = intArray[j + 1];
                                f++;

                                j++;
                            }

                            break;
                    }

                    currentText = reader.ReadLine();
                    currentText =
                        currentText?.Replace("  ", " "); //Some .obj files insert Double spaces, this removes them.
                }
            }
        }
    }
}