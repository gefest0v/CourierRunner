using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace CourierRunner.Editor
{
    // Small importer for the static, single-mesh Source 2 exports used by this prototype.
    [ScriptedImporter(3, "gltf")]
    public sealed class SimpleGltfImporter : ScriptedImporter
    {
        [Serializable] private sealed class Root { public Accessor[] accessors; public BufferView[] bufferViews; public Buffer[] buffers; public ImageInfo[] images; public MaterialInfo[] materials; public MeshInfo[] meshes; }
        [Serializable] private sealed class Accessor { public int bufferView; public int componentType; public int count; public string type; }
        [Serializable] private sealed class BufferView { public int byteOffset; public int byteLength; }
        [Serializable] private sealed class Buffer { public string uri; }
        [Serializable] private sealed class ImageInfo { public string uri; }
        [Serializable] private sealed class MaterialInfo { public string name; public string alphaMode; public float alphaCutoff = 0.5f; public Pbr pbrMetallicRoughness; }
        [Serializable] private sealed class Pbr { public TextureRef baseColorTexture; }
        [Serializable] private sealed class TextureRef { public int index; }
        [Serializable] private sealed class MeshInfo { public string name; public Primitive[] primitives; }
        [Serializable] private sealed class Primitive { public Attributes attributes; public int indices; public int material; }
        [Serializable] private sealed class Attributes { public int POSITION; public int TEXCOORD_0; public int NORMAL; }

        public override void OnImportAsset(AssetImportContext context)
        {
            Root root = JsonUtility.FromJson<Root>(File.ReadAllText(context.assetPath));
            string directory = Path.GetDirectoryName(context.assetPath).Replace('\\', '/');
            byte[] data = File.ReadAllBytes(directory + "/" + root.buffers[0].uri);
            Primitive primitive = root.meshes[0].primitives[0];

            Vector3[] vertices = ReadVector3(data, root, primitive.attributes.POSITION, true);
            Vector3[] normals = ReadVector3(data, root, primitive.attributes.NORMAL, true);
            Vector2[] uv = ReadVector2(data, root, primitive.attributes.TEXCOORD_0);
            int[] triangles = ReadIndices(data, root, primitive.indices);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
                (triangles[i], triangles[i + 2]) = (triangles[i + 2], triangles[i]);

            Mesh mesh = new() { name = root.meshes[0].name };
            if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            if (normals.Length != vertices.Length) mesh.RecalculateNormals();

            Material material = BuildMaterial(root, primitive.material, directory, context);
            GameObject model = new(Path.GetFileNameWithoutExtension(context.assetPath));
            MeshFilter filter = model.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            context.AddObjectToAsset("mesh", mesh);
            context.AddObjectToAsset("material", material);
            context.AddObjectToAsset("model", model);
            context.SetMainObject(model);
        }

        private static Material BuildMaterial(Root root, int index, string directory, AssetImportContext context)
        {
            MaterialInfo info = root.materials[index];
            Material material = new(Shader.Find("Universal Render Pipeline/Lit")) { name = info.name };
            if (info.pbrMetallicRoughness?.baseColorTexture != null)
            {
                int textureIndex = info.pbrMetallicRoughness.baseColorTexture.index;
                if (root.images != null && textureIndex < root.images.Length)
                {
                    string texturePath = directory + "/" + root.images[textureIndex].uri;
                    context.DependsOnSourceAsset(texturePath);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    material.SetTexture("_BaseMap", texture);
                }
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.12f);
            // Source 2 scenery often contains thin or inconsistently wound faces.
            // Rendering both sides prevents parts of rocks and foliage disappearing by angle.
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
            if (string.Equals(info.alphaMode, "MASK", StringComparison.OrdinalIgnoreCase))
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", Mathf.Min(info.alphaCutoff, 0.4f));
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
            }
            return material;
        }

        private static Vector3[] ReadVector3(byte[] data, Root root, int accessorIndex, bool flipZ)
        {
            Accessor accessor = root.accessors[accessorIndex];
            BufferView view = root.bufferViews[accessor.bufferView];
            Vector3[] result = new Vector3[accessor.count];
            for (int i = 0; i < result.Length; i++)
            {
                int p = view.byteOffset + i * 12;
                result[i] = new Vector3(BitConverter.ToSingle(data, p), BitConverter.ToSingle(data, p + 4),
                    BitConverter.ToSingle(data, p + 8) * (flipZ ? -1f : 1f));
            }
            return result;
        }

        private static Vector2[] ReadVector2(byte[] data, Root root, int accessorIndex)
        {
            Accessor accessor = root.accessors[accessorIndex];
            BufferView view = root.bufferViews[accessor.bufferView];
            Vector2[] result = new Vector2[accessor.count];
            for (int i = 0; i < result.Length; i++)
            {
                int p = view.byteOffset + i * 8;
                result[i] = new Vector2(BitConverter.ToSingle(data, p), 1f - BitConverter.ToSingle(data, p + 4));
            }
            return result;
        }

        private static int[] ReadIndices(byte[] data, Root root, int accessorIndex)
        {
            Accessor accessor = root.accessors[accessorIndex];
            BufferView view = root.bufferViews[accessor.bufferView];
            int[] result = new int[accessor.count];
            int stride = accessor.componentType == 5125 ? 4 : 2;
            for (int i = 0; i < result.Length; i++)
            {
                int p = view.byteOffset + i * stride;
                result[i] = stride == 4 ? (int)BitConverter.ToUInt32(data, p) : BitConverter.ToUInt16(data, p);
            }
            return result;
        }
    }
}
