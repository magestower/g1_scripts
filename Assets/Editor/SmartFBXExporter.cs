using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.Collections.Generic;
using System.IO;

public class SmartFBXExporter
{
    [MenuItem("Tools/Smart FBX Exporter/Export Selected (Preserve Setup)")]
    static void ExportSelected()
    {
        GameObject[] selection = Selection.gameObjects;

        if (selection.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        string rootFolder = EditorUtility.SaveFolderPanel(
            "Choose Export Folder", "", "Export");

        if (string.IsNullOrEmpty(rootFolder))
            return;

        string exportName = selection[0].name;
        string exportFolder = Path.Combine(rootFolder, "Export_" + exportName);

        Directory.CreateDirectory(exportFolder);

        string materialFolder = Path.Combine(exportFolder, "Materials");
        string textureFolder = Path.Combine(exportFolder, "Textures");

        Directory.CreateDirectory(materialFolder);
        Directory.CreateDirectory(textureFolder);

        // =============================
        // CREATE TEMP EXPORT ROOT
        // =============================
        GameObject tempRoot = new GameObject(exportName + "_EXPORT");
        
        // Catat original parent untuk restore nanti (opsional)
        Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();
        
        foreach (var go in selection)
        {
            // Simpan parent asli
            originalParents[go] = go.transform.parent;
            
            GameObject clone = Object.Instantiate(go);
            clone.name = go.name;

            // Clone child objects dan preserve hierarchy
            CloneChildren(go, clone);
            
            // Set parent ke tempRoot dengan mempertahankan world position
            clone.transform.SetParent(tempRoot.transform, true);
        }

        // =============================
        // EKSPOR FBX
        // =============================
        string fbxPath = Path.Combine(exportFolder, exportName + ".fbx");

        ModelExporter.ExportObject(fbxPath, tempRoot);

        Debug.Log("FBX Exported: " + fbxPath);

        // =============================
        // COPY MATERIAL + TEXTURE FILES
        // =============================
        ExportDependencies(selection, materialFolder, textureFolder);

        // =============================
        // CLEANUP
        // =============================
        Object.DestroyImmediate(tempRoot);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Export Complete",
            "FBX exported with preserved hierarchy & dependencies!\nLocation: " + exportFolder,
            "OK");
    }

    // ======================================
    // CLONE HIERARCHY
    // ======================================
    static void CloneChildren(GameObject original, GameObject clone)
    {
        // Loop through each child in the original
        foreach (Transform child in original.transform)
        {
            // Cari child yang sesuai di clone berdasarkan nama
            Transform cloneChild = clone.transform.Find(child.name);
            
            if (cloneChild == null)
            {
                // Jika child tidak ditemukan (karena Instantiate hanya deep copy), 
                // kita perlu membuat mapping
                continue;
            }
            
            // Rekursif untuk children dari child ini
            CloneChildren(child.gameObject, cloneChild.gameObject);
        }
        
        // Catatan: Sebenarnya Instantiate sudah melakukan deep copy,
        // jadi fungsi ini hanya untuk memastikan struktur sama persis
    }

    // ======================================
    // DEPENDENCY EXPORT (SAFE VERSION)
    // ======================================
    static void ExportDependencies(
        GameObject[] objects,
        string matFolder,
        string texFolder)
    {
        HashSet<string> copied = new HashSet<string>();

        foreach (var go in objects)
        {
            Renderer[] renderers =
                go.GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;

                    // Copy material file
                    string matSrc = AssetDatabase.GetAssetPath(mat);
                    if (!string.IsNullOrEmpty(matSrc) && File.Exists(matSrc))
                    {
                        string matDst = Path.Combine(matFolder, Path.GetFileName(matSrc));
                        if (!copied.Contains(matSrc))
                        {
                            File.Copy(matSrc, matDst, true);
                            copied.Add(matSrc);
                            Debug.Log($"Material copied: {mat.name}");
                        }
                    }

                    // Get all dependencies (textures)
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    string[] deps = AssetDatabase.GetDependencies(matPath);

                    foreach (string dep in deps)
                    {
                        // Skip jika dependency adalah material itu sendiri
                        if (dep == matPath) continue;
                        
                        Object asset = AssetDatabase.LoadAssetAtPath<Object>(dep);
                        
                        if (asset is Texture || asset is Texture2D)
                        {
                            CopyAsset(asset, texFolder, copied);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Total assets copied: {copied.Count}");
    }

    static void CopyAsset(
        Object asset,
        string targetFolder,
        HashSet<string> copied)
    {
        string src = AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(src)) return;
        if (!File.Exists(src)) return;
        if (copied.Contains(src)) return;

        string dst = Path.Combine(
            targetFolder,
            Path.GetFileName(src));

        // Handle file name conflicts
        dst = GetUniqueFilePath(dst);
        
        File.Copy(src, dst, true);
        copied.Add(src);
        
        Debug.Log($"Asset copied: {asset.name}");
    }
    
    static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;
            
        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        
        int counter = 1;
        string newPath;
        
        do
        {
            newPath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));
        
        return newPath;
    }
}