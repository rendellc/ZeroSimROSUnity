﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZO.Document;


namespace ZO.Editor {


    /// <summary>
    /// Saving and loading ZOSimDocumentRoot.
    /// </summary>
    [CustomEditor(typeof(ZOSimDocumentRoot))]
    public class ZOSimDocumentRootEditor : UnityEditor.Editor {

        private string _URDFExportDirectory = "";

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            ZOSimDocumentRoot documentRoot = (ZOSimDocumentRoot)target;

            /* TODO: no longer supporting ZOSim files.  Remove this.
            if (GUILayout.Button("Save ZOSim")) {
                Debug.Log("INFO: ZOSimDocumentRoot Save ZOSim");
                if (EditorUtility.DisplayDialog("Override File:", "Override File: " + documentRoot.ZOSimDocumentFilePath, "OK", "Cancel")) {
                    documentRoot.SaveToZOSimFile(documentRoot.ZOSimDocumentFilePath);
                }

            }

            if (GUILayout.Button("Load ZOSim")) {
                Debug.Log("INFO: ZOSimDocumentRoot Load ZOSim");
                if (EditorUtility.DisplayDialog("Override Object:", "Override Object with file: " + documentRoot.ZOSimDocumentFilePath, "OK", "Cancel")) {

                    documentRoot.LoadFromZOSimFile(documentRoot.ZOSimDocumentFilePath);
                }
            }
            */
            if (GUILayout.Button("Export URDF...")) {
                Debug.Log("INFO: ZOSimDocumentRoot Save URDF");
                _URDFExportDirectory = EditorUtility.OpenFolderPanel("Select URDF export directory", _URDFExportDirectory, "");

                if (_URDFExportDirectory.Length == 0) {
                    return;
                } else if (!System.IO.Directory.Exists(_URDFExportDirectory)) {
                    EditorUtility.DisplayDialog("URDF Export Error", "Export root folder must be defined and folder must exist.", "Ok");
                } else {
                    documentRoot.ExportURDF(_URDFExportDirectory);
                }


            }
        }

    }
}