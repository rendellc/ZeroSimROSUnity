﻿// using System.Numerics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using ZO.Physics;
using ZO.Util.Extensions;

namespace ZO {

    [ExecuteAlways]
    /// <summary>
    /// An occurrence (or instance) of a ZoSim "component".  
    /// NOTE: In the context of ZoSim a "component" is different then a Unity Component.  
    /// In this case a component defines the various assets, such as visual and collision 
    /// meshes and invariant properties such as mass and inertia tensors.  The occurrence
    /// defines "instance" properties such as position & orientation.
    /// 
    /// </summary>
    public class ZOSimOccurrence : MonoBehaviour, ZOSimTypeInterface {

        [ZO.Util.ZOReadOnly] [SerializeField] public ZOSimDocumentRoot _documentRoot;

        public ZOSimDocumentRoot DocumentRoot {
            set { _documentRoot = value; }
            get {
                if (_documentRoot == null) {  // traverse up hierarchy to find parent base component
                    Transform parent = transform.parent;
                    while (parent != null) {
                        _documentRoot = parent.GetComponent<ZOSimDocumentRoot>();
                        if (_documentRoot != null) {
                            break;  // found
                        }
                        parent = parent.transform.parent; // keep traversing up
                    }
                }
                return _documentRoot;
            }
        }
        private JObject _json;
        public JObject JSON {
            get => _json;
            set => _json = value;
        }

        public string Name {
            get {
                return gameObject.name;
            }
            private set => gameObject.name = value;
        }

        public string Type {
            get {
                return "occurrence";
            }
        }

        private void Start() {
            if (Application.IsPlaying(gameObject) == false) { // In Editor Mode 
                // update root component
                ZOSimDocumentRoot rootComponent = DocumentRoot;
            }
        }

        public ZOSimOccurrence GetOccurrence(string occurrenceName) {
            Transform t = transform.Find(occurrenceName);
            if (t != null) {
                ZOSimOccurrence simOccurrence = t.GetComponent<ZOSimOccurrence>();
                return simOccurrence;
            }
            return null;
        }



        public void LoadFromJSON(JObject json) {
            Name = json["name"].Value<string>();
            JSON = json;

            // get the transform
            Quaternion rotation = Quaternion.identity;
            Vector3 translation = Vector3.zero;
            Vector3 scale = Vector3.one;
            if (json.ContainsKey("transform")) {  // has 4x4 matrix for transform
                // set the occurrence GameObject transform
                List<float> transformList = json["transform"].ToObject<List<float>>();
                Vector4 c0 = new Vector4(transformList[0], transformList[1], transformList[2], transformList[3]);
                Vector4 c1 = new Vector4(transformList[4], transformList[5], transformList[6], transformList[7]);
                Vector4 c2 = new Vector4(transformList[8], transformList[9], transformList[10], transformList[11]);
                Vector4 c3 = new Vector4(transformList[12], transformList[13], transformList[14], transformList[15]);
                Matrix4x4 matrix4X4 = new Matrix4x4(c0, c1, c2, c3);
                matrix4X4 = matrix4X4.transpose;  // go from fusion360 row major to Unity column major
                rotation = ZO.Math.ZOMatrix4x4Util.GetRotation(matrix4X4);
                translation = ZO.Math.ZOMatrix4x4Util.GetTranslation(matrix4X4);
                scale = ZO.Math.ZOMatrix4x4Util.GetScale(matrix4X4);
            }

            translation = json.ToVector3OrDefault("translation", translation);
            rotation = json.ToQuaternionOrDefault("rotation_quaternion", rotation);
            scale = json.ToVector3OrDefault("scale", scale);
            rotation = Quaternion.Euler(json.ToVector3OrDefault("rotation_euler_degrees", rotation.eulerAngles));

            this.transform.localRotation = rotation;
            this.transform.localPosition = translation;
            this.transform.localScale = scale;

            // load primitive type if exists
            if (json.ContainsKey("primitive")) {
                JObject primitiveJSON = json["primitive"].Value<JObject>();
                // get mesh filter. if it doesn't exist create it.
                MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = null;
                if (meshFilter == null) {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    meshRenderer.material = new Material(Shader.Find("Diffuse"));

                    if (primitiveJSON["type"].Value<string>() == "cube") {
                        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                        meshFilter.sharedMesh.name = "Cube Instance";
                        bool hasCollisions = primitiveJSON.ValueOrDefault<bool>("has_collisions", false);
                        if (hasCollisions) {
                            gameObject.AddComponent<BoxCollider>();
                        }
                    }

                    if (primitiveJSON["type"].Value<string>() == "sphere") {
                        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                        meshFilter.sharedMesh.name = "Sphere Instance";
                        bool hasCollisions = primitiveJSON.ValueOrDefault<bool>("has_collisions", false);
                        if (hasCollisions) {
                            gameObject.AddComponent<SphereCollider>();
                        }
                    }

                    if (primitiveJSON["type"].Value<string>() == "capsule") {
                        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                        meshFilter.sharedMesh.name = "Capsule Instance";
                        bool hasCollisions = primitiveJSON.ValueOrDefault<bool>("has_collisions", false);
                        if (hasCollisions) {
                            gameObject.AddComponent<CapsuleCollider>();
                        }
                    }

                    if (primitiveJSON["type"].Value<string>() == "cylinder") {
                        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                        meshFilter.sharedMesh.name = "Cylinder Instance";
                        bool hasCollisions = primitiveJSON.ValueOrDefault<bool>("has_collisions", false);
                        if (hasCollisions) {
                            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                            meshCollider.convex = true;
                        }
                    }
                }

                // set color
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial.color = primitiveJSON.ToColorOrDefault("color", meshRenderer.sharedMaterial.color);


                this.transform.localScale = primitiveJSON.ToVector3OrDefault("dimensions", this.transform.localScale);
                
            }


            // load rigid body if exists
            if (json.ContainsKey("rigidbody")) {
                JObject rbJson = json["rigidbody"].Value<JObject>();
                Rigidbody rb = GetComponent<Rigidbody>();

                if (rb == null) {
                    rb = this.gameObject.AddComponent<Rigidbody>();
                }
                rb.mass = rbJson.ContainsKey("mass") ? rbJson["mass"].Value<float>() : 1.0f;
                rb.drag = rbJson.ContainsKey("drag") ? rbJson["drag"].Value<float>() : 0;
                rb.angularDrag = rbJson.ContainsKey("angular_drag") ? rbJson["angular_drag"].Value<float>() : 0.01f;
                rb.useGravity = rbJson.ContainsKey("use_gravity") ? rbJson["use_gravity"].Value<bool>() : true;
                rb.isKinematic = rbJson.ContainsKey("is_kinematic") ? rbJson["is_kinematic"].Value<bool>() : false;
            }


            // load the joints
            if (json.ContainsKey("joints")) {
                foreach (JObject joint in json["joints"].Value<JArray>()) {
                    if (joint["type"].Value<string>() == "joint.hinge") {
                        ZOHingeJoint hingeJoint = null;
                        // check if joint exists 
                        ZOHingeJoint[] existingHingeJoints = this.GetComponents<ZOHingeJoint>();
                        foreach (ZOHingeJoint hj in existingHingeJoints) {
                            if (hj.Name == joint["name"].Value<string>()) {
                                hingeJoint = hj;
                            }
                        }
                        // if hinge joint doesn't exist then create it
                        if (hingeJoint == null) {
                            hingeJoint = this.gameObject.AddComponent<ZOHingeJoint>();
                            hingeJoint.CreateRequirements();
                        }

                        hingeJoint.LoadFromJSON(joint);
                    }
                }
            }

            // go through children
            foreach (JObject occurrenceJSON in JSON["children"].Value<JArray>()) {
                string occurrenceName = occurrenceJSON["name"].Value<string>();

                ZOSimOccurrence simOccurrence = GetOccurrence(occurrenceName);
                if (simOccurrence == null) {
                    // create new sim occurrence gameobject
                    GameObject go = new GameObject(occurrenceName);
                    go.transform.parent = this.transform;
                    simOccurrence = go.AddComponent<ZOSimOccurrence>();
                }
                simOccurrence.LoadFromJSON(occurrenceJSON);
            }

            //TODO: remove any children not in the children list

            //TODO:  apply any fixups such as joint connected bodies

        }

        public void ImportZeroSim(JObject json) {
            _json = json;

#if UNITY_EDITOR            

            // get the transform
            Quaternion rotation = Quaternion.identity;
            Vector3 translation = Vector3.zero;
            Vector3 scale = Vector3.one;
            if (json.ContainsKey("transform")) {  // has 4x4 matrix for transform
                // set the occurrence GameObject transform
                List<float> transformList = json["transform"].ToObject<List<float>>();
                Vector4 c0 = new Vector4(transformList[0], transformList[1], transformList[2], transformList[3]);
                Vector4 c1 = new Vector4(transformList[4], transformList[5], transformList[6], transformList[7]);
                Vector4 c2 = new Vector4(transformList[8], transformList[9], transformList[10], transformList[11]);
                Vector4 c3 = new Vector4(transformList[12], transformList[13], transformList[14], transformList[15]);
                Matrix4x4 matrix4X4 = new Matrix4x4(c0, c1, c2, c3);
                matrix4X4 = matrix4X4.transpose;  // go from fusion360 row major to Unity column major
                rotation = ZO.Math.ZOMatrix4x4Util.GetRotation(matrix4X4);
                translation = ZO.Math.ZOMatrix4x4Util.GetTranslation(matrix4X4);
                scale = ZO.Math.ZOMatrix4x4Util.GetScale(matrix4X4);
            }

            translation = json.ToVector3OrDefault("translation", translation);
            rotation = json.ToQuaternionOrDefault("rotation_quaternion", rotation);
            scale = json.ToVector3OrDefault("scale", scale);
            rotation = Quaternion.Euler(json.ToVector3OrDefault("rotation_euler_degrees", rotation.eulerAngles));

            this.transform.localRotation = rotation;
            this.transform.localPosition = translation;


            // if (json.ContainsKey("translation")) {
            //     List<float> translation_list = json["translation"].ToObject<List<float>>();
            //     translation.x = translation_list[0];
            //     translation.y = translation_list[1];
            //     translation.z = translation_list[2];
            // }

            // if (json.ContainsKey("rotation_quaternion")) {
            //     List<float> rotation_list = json["rotation_quaternion"].ToObject<List<float>>();
            //     rotation.x = rotation_list[0];
            //     rotation.y = rotation_list[1];
            //     rotation.z = rotation_list[2];
            //     rotation.w = rotation_list[3];
            // }

            List<float> positionTransformScale = DocumentRoot.JSON["position_transform_scale"].ToObject<List<float>>();


            translation.x = translation.x * positionTransformScale[0];
            translation.y = translation.y * positionTransformScale[1];
            translation.z = translation.z * positionTransformScale[2];

            gameObject.transform.localRotation = rotation;
            gameObject.transform.localPosition = translation;
            gameObject.transform.localScale = scale;

            if (json.ContainsKey("component_name") == true) {
                JObject componentJson = _documentRoot.GetComponentJSON(json["component_name"].Value<string>());


                if (componentJson.ContainsKey("visual_mesh_file") == true) {
                    // create a visual gameobject
                    GameObject visualsGo = new GameObject("visuals");
                    visualsGo.transform.parent = gameObject.transform;
                    visualsGo.transform.localPosition = Vector3.zero;
                    visualsGo.transform.localRotation = Quaternion.identity;

                    // Find the visual mesh prefab associated with the occurrence component
                    string visual_mesh_file = componentJson["visual_mesh_file"].Value<string>();

                    Debug.Log("INFO: visual_obj_mesh_file: " + visual_mesh_file);
                    string visualMeshFile = Path.GetFileNameWithoutExtension(componentJson["visual_mesh_file"].Value<string>());
                    Debug.Log("INFO: visual mesh file path: " + visualMeshFile);
                    string[] visualMeshPrefabGUIDs = AssetDatabase.FindAssets(visualMeshFile);

                    // NOTE: should only ever be one prefab but we will just go ahead and go through all potential returns   
                    foreach (string meshPrefabGUID in visualMeshPrefabGUIDs) {
                        // string meshPrefabGUID = visualMeshPrefabGUIDs[0];  //BUGBUG:  Always first one found
                        string visualMeshPrefabPath = AssetDatabase.GUIDToAssetPath(meshPrefabGUID);

                        if (visualMeshPrefabPath.Contains("/" + visualMeshFile + ".obj") || visualMeshPrefabPath.Contains("/" + visualMeshFile + ".fbx")) {
                            Debug.Log("INFO: Found visual mesh prefab: " + visualMeshPrefabPath);

                            GameObject visualMeshGoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualMeshPrefabPath);
                            GameObject visualMeshGo = PrefabUtility.InstantiatePrefab(visualMeshGoPrefab, visualsGo.transform) as GameObject;

                            Vector3 newScale = visualMeshGo.transform.localScale;
                            List<float> meshTransformScale = DocumentRoot.JSON["mesh_transform_scale"].ToObject<List<float>>();

                            newScale.x = newScale.x * meshTransformScale[0];
                            newScale.y = newScale.y * meshTransformScale[1];
                            newScale.z = newScale.z * meshTransformScale[2];
                            visualMeshGo.transform.localScale = newScale;

                        }
                    }
                }

                if (componentJson.ContainsKey("collision_meshes")) {
                    // create a collision gameobject
                    GameObject collisionsGo = new GameObject("collisions");
                    collisionsGo.transform.parent = gameObject.transform;
                    collisionsGo.transform.localPosition = Vector3.zero;
                    collisionsGo.transform.localRotation = Quaternion.identity;

                    // find the collision mesh prefabs associated with the occurrence component
                    foreach (string fileName in componentJson["collision_meshes"]) {
                        string collisionMeshFile = Path.GetFileNameWithoutExtension(fileName);
                        string[] collisionMeshPrefabGUIDS = AssetDatabase.FindAssets(collisionMeshFile);
                        foreach (string collisionMeshPrefabGUID in collisionMeshPrefabGUIDS) {
                            string collisionMeshPrefabPath = AssetDatabase.GUIDToAssetPath(collisionMeshPrefabGUID);
                            string collisionMeshPrefabPathBaseName = Path.GetFileNameWithoutExtension(collisionMeshPrefabPath);
                            // FindAssets will glob a bunch of assets that aren't exactly the one we want so make sure here
                            if (collisionMeshPrefabPathBaseName.Equals(collisionMeshFile)) {
                                Debug.Log("INFO: Found collision mesh prefab: " + collisionMeshPrefabPath);
                                GameObject collisionMeshGoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(collisionMeshPrefabPath);

                                // apply transforms on collision mesh
                                GameObject collisionMeshGo = new GameObject(collisionMeshFile);
                                collisionMeshGo.transform.parent = collisionsGo.transform;
                                collisionMeshGo.transform.localPosition = Vector3.zero;
                                collisionMeshGo.transform.localRotation = Quaternion.identity;
                                Vector3 newScale = collisionMeshGo.transform.localScale;
                                List<float> meshTransformScale = DocumentRoot.JSON["mesh_transform_scale"].ToObject<List<float>>();
                                newScale.x = newScale.x * meshTransformScale[0];
                                newScale.y = newScale.y * meshTransformScale[1];
                                newScale.z = newScale.z * meshTransformScale[2];
                                collisionMeshGo.transform.localScale = newScale;

                                MeshCollider meshCollider = collisionMeshGo.AddComponent<MeshCollider>();
                                MeshFilter meshFilter = collisionMeshGoPrefab.GetComponentInChildren<MeshFilter>();
                                meshCollider.sharedMesh = meshFilter.sharedMesh;
                                meshCollider.convex = true;

                            }


                        }
                    }

                }

            }
#endif // #if UNITY_EDITOR
        }

        public JObject BuildJSON(Object parent = null) {
            JObject occurrence = new JObject();

            // TODO: check with document root if there is a zosim component reference 

            occurrence["name"] = Name;

            occurrence["translation"] = transform.localPosition.ToJSON();
            occurrence["rotation_quaternion"] = transform.localRotation.ToJSON();
            occurrence["scale"] = transform.localScale.ToJSON();

            if (parent != null) {
                occurrence["parent_name"] = ((ZOSimOccurrence)parent).Name;
            }

            // build 3d primitive if exists
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter) {
                
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

                if (meshFilter.sharedMesh.name == "Cube Instance") {
                    occurrence["primitive"] = new JObject(
                        new JProperty("type", "cube"),
                        new JProperty("dimensions", transform.localScale.ToJSON()),
                        new JProperty("has_collisions", gameObject.GetComponent<BoxCollider>() ? true : false),
                        new JProperty("color", meshRenderer.sharedMaterial.color.ToJSON())
                    );
                }
                if (meshFilter.sharedMesh.name == "Sphere Instance") {
                    occurrence["primitive"] = new JObject(
                        new JProperty("type", "sphere"),
                        new JProperty("dimensions", transform.localScale.ToJSON()),
                        new JProperty("has_collisions", gameObject.GetComponent<SphereCollider>() ? true : false),
                        new JProperty("color", meshRenderer.sharedMaterial.color.ToJSON())
                    ); 
                } 
                if (meshFilter.sharedMesh.name == "Capsule Instance") {
                    occurrence["primitive"] = new JObject(
                        new JProperty("type", "capsule"),
                        new JProperty("dimensions", transform.localScale.ToJSON()),
                        new JProperty("has_collisions", gameObject.GetComponent<CapsuleCollider>() ? true : false),
                        new JProperty("color", meshRenderer.sharedMaterial.color.ToJSON())
                    ); 
                } 
                if (meshFilter.sharedMesh.name == "Cylinder Instance") {
                    occurrence["primitive"] = new JObject(
                        new JProperty("type", "cylinder"),
                        new JProperty("dimensions", transform.localScale.ToJSON()),
                        new JProperty("has_collisions", gameObject.GetComponent<MeshCollider>() ? true : false),
                        new JProperty("color", meshRenderer.sharedMaterial.color.ToJSON())
                    ); 
                } 
                

            }
            // Debug.Log("INFO: Mesh name: " + mesh.name);

            // rigid body
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null) {
                occurrence["rigidbody"] = new JObject(
                    new JProperty("mass", rigidbody.mass),
                    new JProperty("drag", rigidbody.drag),
                    new JProperty("angular_drag", rigidbody.angularDrag),
                    new JProperty("use_gravity", rigidbody.useGravity),
                    new JProperty("is_kinematic", rigidbody.isKinematic)
                );
            } else if (occurrence.ContainsKey("rigidbody") == true) {
                occurrence.Remove("rigidbody");
            }

            // Joints & Controllers
            JArray joints = new JArray();
            JArray controllers = new JArray();

            // FixedJoint
            // TODO: make a ZOFixedJoint 
            foreach (FixedJoint fixedJoint in GetComponents<FixedJoint>()) {
                JObject fixedJointJSON = new JObject(
                    new JProperty("type", "fixed")
                );
                if (fixedJoint.connectedBody != null) {
                    fixedJointJSON["connected_occurrence"] = fixedJoint.connectedBody.gameObject.name;
                }

                joints.Add(fixedJointJSON);

            }

            // save rest of objects that implement the ZOSimTypeInterface
            ZOSimTypeInterface[] simTypes = GetComponents<ZO.ZOSimTypeInterface>();
            foreach (ZOSimTypeInterface simType in simTypes) {
                string[] subtypes = simType.Type.Split('.');

                // handle joint types
                if (subtypes[0] == "joint") {
                    joints.Add(simType.JSON);
                } else if (subtypes[0] == "controller") {
                    controllers.Add(simType.JSON);
                }

            }


            occurrence["joints"] = joints;
            occurrence["controllers"] = controllers;

            // go through the children
            JArray children = new JArray();
            foreach (Transform child in transform) {
                ZOSimOccurrence simOccurrence = child.GetComponent<ZOSimOccurrence>();
                if (simOccurrence) {
                    JObject child_json = simOccurrence.BuildJSON(this);
                    children.Add(child_json);
                }
            }
            occurrence["children"] = children;


            return occurrence;

        }
    }
}