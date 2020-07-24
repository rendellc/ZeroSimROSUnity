﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Newtonsoft.Json.Linq;
using ZO.Util.Extensions;
using ZO;

namespace ZO.Physics {

    /// <summary>
    /// A Zero Sim wrapper class for a Unity Hinge joint. Supports writing and reading to ZeroSim 
    /// JSON format.
    /// 
    /// The HingeJoint groups together 2 rigid bodies, constraining them to move like connected by a hinge.
    ///
    /// The HingeJoint has a motor which can be used to make the hinge spin around the joints axis. A spring 
    /// which attempts to reach for a target angle by spinning around the joints axis. And a limit which 
    /// constrains the joint angle.
    /// </summary>
    [ExecuteAlways]
    public class ZOHingeJoint : MonoBehaviour, ZOSerializationInterface {

        [SerializeField] public UnityEngine.HingeJoint _hingeJoint;

        /// <summary>
        /// The Unity hinge joint linked to this ZOSim hinge joint.
        /// </summary>
        /// <value></value>
        public UnityEngine.HingeJoint UnityHingeJoint {
            get {
                return _hingeJoint;
            }
        }




        /// <summary>
        /// Set and get the angular velocity of the hinge joint.  In degrees.
        /// </summary>
        /// <value>Angular velocity in degrees</value>
        public float AngularVelocityDegrees {
            set {
                JointMotor motor = _hingeJoint.motor;
                motor.targetVelocity = value;
                motor.freeSpin = false;
                _hingeJoint.motor = motor;
                _hingeJoint.useMotor = true;
                _hingeJoint.useSpring = false;
            }

            get {
                return UnityHingeJoint.velocity;
            }
        }

        /// <summary>
        /// The current angle in degrees of the hinge joint relative to it's start position.
        /// </summary>
        /// <value></value>
        public float AngleDegrees {
            get {
                return UnityHingeJoint.angle;
            }
        }

        /// <summary>
        /// Read only maximum torque of this hinge joint motor.
        /// </summary>
        /// <value></value>
        public float TorqueNewtonMeters {
            get {
                return UnityHingeJoint.motor.force;
            }
        }



        private void Reset() {
            CreateRequirements();
        }

        public void CreateRequirements() {
            // when creating this a ZOHingeJoint we need to create an actual Unity Hinge Joint.
            if (UnityHingeJoint == null) { // create Unity Hinge Joint
                _hingeJoint = gameObject.AddComponent<UnityEngine.HingeJoint>();
            }

            if (_name == null) {
                _name = Type;

                ZOSimOccurrence occurrence = GetComponent<ZOSimOccurrence>();
                if (occurrence) {
                    _name = _name + "_from_" + occurrence.Name;
                }

                if (UnityHingeJoint.connectedBody) {
                    ZOSimOccurrence connected_occurrence = UnityHingeJoint.connectedBody.gameObject.GetComponent<ZOSimOccurrence>();

                    if (connected_occurrence) {
                        _name = _name + "_to_" + connected_occurrence.Name;
                    }
                }
            }

        }

        // private void OnDestroy() {
        //     if ((Application.isEditor ==true) && (Application.isPlaying == false) && (_hingeJoint != null) && (Application.isLoadingLevel == false)) {
        //         DestroyImmediate(_hingeJoint);
        //     }
        // }


        #region ZOSerializationInterface
        public string Type {
            get { return "joint.hinge"; }
        }

        [SerializeField] public string _name;
        public string Name {
            get {
                return _name;
            }
            private set {
                _name = value;
            }
        }

        private JObject _json;
        public JObject JSON {
            get {
                // if (_json == null) {
                //     _json = BuildJSON();
                // }
                return _json;

            }
        }


        public JObject Serialize(ZOSimDocumentRoot documentRoot, UnityEngine.Object parent = null) {
            // calculate the world anchor position
            Vector3 worldAnchor = this.transform.TransformPoint(UnityHingeJoint.anchor);
            Vector3 worldConnectedAnchor = this.transform.TransformPoint(UnityHingeJoint.connectedAnchor);
            Vector3 worldAxis = this.transform.rotation * UnityHingeJoint.axis;
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("type", Type),
                new JProperty("anchor", ZOSimDocumentRoot.ToJSON(UnityHingeJoint.anchor)),
                new JProperty("world_anchor", ZOSimDocumentRoot.ToJSON(worldAnchor)),
                new JProperty("axis", ZOSimDocumentRoot.ToJSON(UnityHingeJoint.axis)),
                new JProperty("world_axis", ZOSimDocumentRoot.ToJSON(worldAxis)),
                new JProperty("connected_anchor", ZOSimDocumentRoot.ToJSON(UnityHingeJoint.connectedAnchor)),
                new JProperty("world_connected_anchor", ZOSimDocumentRoot.ToJSON(worldConnectedAnchor)),
                new JProperty("use_spring", UnityHingeJoint.useSpring),
                new JProperty("spring", new JObject(
                    new JProperty("spring", UnityHingeJoint.spring.spring),
                    new JProperty("damper", UnityHingeJoint.spring.damper),
                    new JProperty("target_position", UnityHingeJoint.spring.targetPosition)
                )),
                new JProperty("use_motor", UnityHingeJoint.useMotor),
                new JProperty("motor", new JObject(
                    new JProperty("target_velocity", UnityHingeJoint.motor.targetVelocity),
                    new JProperty("force", UnityHingeJoint.motor.force),
                    new JProperty("free_spin", UnityHingeJoint.motor.freeSpin)
                )),
                new JProperty("use_limits", UnityHingeJoint.useLimits),
                new JProperty("limits", new JObject(
                    new JProperty("min", UnityHingeJoint.limits.min),
                    new JProperty("max", UnityHingeJoint.limits.max),
                    new JProperty("bounciness", UnityHingeJoint.limits.bounciness),
                    new JProperty("bounce_min_velocity", UnityHingeJoint.limits.bounceMinVelocity),
                    new JProperty("contact_distance", UnityHingeJoint.limits.contactDistance)
                ))
            );

            if (UnityHingeJoint.connectedBody) {
                ZOSimOccurrence connected_occurrence = UnityHingeJoint.connectedBody.gameObject.GetComponent<ZOSimOccurrence>();

                if (connected_occurrence) {
                    json["connected_occurrence"] = connected_occurrence.Name;
                } else {
                    Debug.LogWarning("WARNING: Could not get connected occurrence for ZOHingeJoint: " + Name + "\nPerhaps there is a missing ZOSimOccurrence?");
                }
            } else {
                Debug.LogWarning("WARNING: Could not get connected occurrence for ZOHingeJoint: " + Name);
            }

            ZOSimOccurrence parent_occurrence = GetComponent<ZOSimOccurrence>();
            if (parent_occurrence) {
                json["parent_occurrence"] = parent_occurrence.Name;
            }

            _json = json;

            return json;
        }


        public void Deserialize(ZOSimDocumentRoot documentRoot, JObject json) {
            // Assert.Equals(json["type"].Value<string>() == Type);

            _json = json;
            Name = json.ValueOrDefault("name", Name);
            UnityHingeJoint.anchor = json.ToVector3OrDefault("anchor", UnityHingeJoint.anchor);
            UnityHingeJoint.axis = json.ToVector3OrDefault("axis", UnityHingeJoint.axis);
            UnityHingeJoint.connectedAnchor = json.ToVector3OrDefault("connected_anchor", UnityHingeJoint.connectedAnchor);
            UnityHingeJoint.useSpring = json.ValueOrDefault<bool>("use_spring", UnityHingeJoint.useSpring);


            if (json.ContainsKey("spring")) {
                JObject springJSON = json["spring"].Value<JObject>();
                JointSpring spring = UnityHingeJoint.spring;
                spring.spring = springJSON.ValueOrDefault<float>("spring", spring.spring);
                spring.damper = springJSON.ValueOrDefault<float>("damper", spring.damper);
                spring.targetPosition = springJSON.ValueOrDefault<float>("target_position", spring.targetPosition);
                UnityHingeJoint.spring = spring;

            }

            UnityHingeJoint.useMotor = json.ValueOrDefault<bool>("use_motor", UnityHingeJoint.useMotor);
            if (json.ContainsKey("use_motor")) {
                JObject motorJSON = json["motor"].Value<JObject>();
                JointMotor motor = UnityHingeJoint.motor;
                motor.targetVelocity = motorJSON.ValueOrDefault<float>("target_velocity", motor.targetVelocity);
                motor.force = motorJSON.ValueOrDefault<float>("force", motor.force);
                motor.freeSpin = motorJSON.ValueOrDefault<bool>("free_spin", motor.freeSpin);
                UnityHingeJoint.motor = motor;
            }

            UnityHingeJoint.useLimits = json.ValueOrDefault<bool>("use_limits", UnityHingeJoint.useLimits);
            if (json.ContainsKey("limits")) {
                JObject limitsJSON = json["limits"].Value<JObject>();
                JointLimits limits = UnityHingeJoint.limits;
                limits.min = limitsJSON.ValueOrDefault<float>("min", limits.min);
                limits.max = limitsJSON.ValueOrDefault<float>("max", limits.max);
                limits.bounciness = limitsJSON.ValueOrDefault<float>("bounciness", limits.bounciness);
                limits.bounceMinVelocity = limitsJSON.ValueOrDefault<float>("bounce_min_velocity", UnityHingeJoint.limits.bounceMinVelocity);
                limits.contactDistance = limitsJSON.ValueOrDefault<float>("contact_distance", limits.contactDistance);
                UnityHingeJoint.limits = limits;
            }

            // find connected body.  this likely will need to be done post LoadFromJSON as it may
            // not be created yet.
            documentRoot.OnPostDeserializationNotification((docRoot) => {
                if (JSON.ContainsKey("connected_occurrence")) {
                    ZOSimOccurrence connectedOccurrence = docRoot.GetOccurrence(JSON["connected_occurrence"].Value<string>());
                    UnityHingeJoint.connectedBody = connectedOccurrence.GetComponent<Rigidbody>();
                }
            });

        }
        #endregion

    }

}
