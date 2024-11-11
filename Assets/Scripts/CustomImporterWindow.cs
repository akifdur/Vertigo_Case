#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditorInternal;

namespace CustomImporter
{
    public class CustomImporterWindow : EditorWindow
    {
        [SerializeField] private DefaultAsset targetFolder;

        [SerializeField] private bool showExternalAssetsGroup = true;
        [SerializeField] private string externalAssetsPath;
        [SerializeField] private List<string> externalAssets = new List<string>();

        [SerializeField] private bool showNamingGroup = true;
        [SerializeField] private string find;
        [SerializeField] private string replace;

        [SerializeField] private ReorderableList reorderableList;
        

        [MenuItem("Vertigo Case/Custom Importer Window")]
        public static void ShowExample()
        {
            CustomImporterWindow wnd = GetWindow<CustomImporterWindow>();
            wnd.titleContent = new GUIContent("Custom Importer Window");
        }

        private void OnEnable()
        {
            reorderableList = new ReorderableList(externalAssets, typeof(string), false, true, false, false);
            reorderableList.elementHeight = 20;
            reorderableList.drawHeaderCallback = DrawHeader;
            reorderableList.drawElementCallback = DrawElement;
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "External Files");
        }
        private void DrawElement(Rect rect, int index, bool active, bool focus)
        {
            if (index < 0 || index >= externalAssets.Count)
                return;

            var textRect = new Rect(rect.x, rect.y + 2, rect.width - 20, rect.height - 2);
            var buttonRect = new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height);

            EditorGUI.BeginDisabledGroup(true);
            GUI.TextField(textRect, Path.GetFileName(externalAssets[index]));
            EditorGUI.EndDisabledGroup();

            if (GUI.Button(buttonRect, "X"))
                externalAssets.RemoveAt(index);
        }
        private void FindAllExternalFiles()
        {
            var allFiles = CustomImporterUtility.FindFiles(externalAssetsPath);
            externalAssets.Clear();
            if (allFiles == null || allFiles.Count == 0)
                return;

            externalAssets.AddRange(allFiles);
        }

        private void OnGUI()
        {
            GUILayout.Label("My Custom Editor Window", EditorStyles.boldLabel);

            targetFolder = EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(DefaultAsset), false) as DefaultAsset;

            EditorGUILayout.Space(10);
            showExternalAssetsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(showExternalAssetsGroup, "External Assets");
            if (showExternalAssetsGroup)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("External Assets Path", externalAssetsPath);
                EditorGUI.EndDisabledGroup();

                reorderableList.DoLayoutList();

                if (GUILayout.Button("Pick Assets Folder", GUILayout.Height(30)))
                {
                    externalAssetsPath = EditorUtility.OpenFolderPanel("Select a folder", "Assets", "");
                    FindAllExternalFiles();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);
            showNamingGroup = EditorGUILayout.BeginFoldoutHeaderGroup(showNamingGroup, "Naming");
            if (showNamingGroup)
            {
                find = EditorGUILayout.TextField("Find", find);
                replace = EditorGUILayout.TextField("Replace", replace);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);

            bool disable = string.IsNullOrEmpty(find) || string.IsNullOrEmpty(replace);
            disable |= string.IsNullOrEmpty(externalAssetsPath) || externalAssets == null || externalAssets.Count == 0;
            disable |= targetFolder == null;

            EditorGUI.BeginDisabledGroup(disable);
            GUI.color = Color.cyan;
            if (GUILayout.Button("Import", GUILayout.Height(30)))
                Import();
            EditorGUI.EndDisabledGroup();
        }

        private void Import()
        {
            // Copy the files from the origibnal path to the new path.
            string originalPath = AssetDatabase.GetAssetPath(targetFolder);
            string createdPath = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileName(externalAssetsPath));
            AssetDatabase.CopyAsset(originalPath, createdPath);

            var originalAssetGUIDs = AssetDatabase.FindAssets("", new string[1] { originalPath });
            var createdAssetGUIDs = AssetDatabase.FindAssets("", new string[1] { createdPath });

            RenameCreatedFiles(createdAssetGUIDs);

            var filesToFix = CustomImporterUtility.FindFiles(CustomImporterUtility.ConvertToAbsolutePath(createdPath), x =>
            {
                using (var readingFile = new StreamReader(x))
                {
                    var firstLine = readingFile.ReadLine();
                    return firstLine.Contains("%YAML");
                }
            });

            FixReferences(filesToFix, originalAssetGUIDs, createdAssetGUIDs);

            ReplaceFiles(createdAssetGUIDs);

            AssetDatabase.Refresh();
        }

        private void RenameCreatedFiles(string[] createdFileGUIDs)
        {
            for (int i = createdFileGUIDs.Length - 1; i >= 0; i--)
            {
                string assetGUID = createdFileGUIDs[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);

                if (assetName.Contains(find))
                {
                    string newName = assetName.Replace(find, replace);
                    AssetDatabase.RenameAsset(assetPath, newName);
                }
            }
        }
        private void FixReferences(List<string> filesToFix, string[] originalAssetGUIDs, string[] createdAssetGUIDs)
        {
            // Replace old GUID with the new GUID given.
            foreach (var file in filesToFix)
            {
                var fileText = File.ReadAllText(file);
                for (int i = 0; i < originalAssetGUIDs.Length; i++)
                {
                    var orgGUID = CustomImporterUtility.GetGuidExpression(originalAssetGUIDs[i]);
                    var newGUID = CustomImporterUtility.GetGuidExpression(createdAssetGUIDs[i]);
                    fileText = fileText.Replace(orgGUID, newGUID);
                }

                File.WriteAllText(file, fileText);
            }
        }
        private void ReplaceFiles(string[] createdAssetGUIDs)
        {
            List<string> createdAssetPaths = new List<string>(createdAssetGUIDs.Length);
            for (int i = 0; i < createdAssetGUIDs.Length; i++)
                createdAssetPaths.Add(CustomImporterUtility.ConvertToAbsolutePath(AssetDatabase.GUIDToAssetPath(createdAssetGUIDs[i])));

            foreach (var externalAsset in externalAssets)
            {
                var assetName = Path.GetFileName(externalAsset);
                var createdAssetPath = createdAssetPaths.Find(x => Path.GetFileName(x) == assetName);
            
                File.Copy(externalAsset, createdAssetPath, true);
            }
        }
    }
}
#endif