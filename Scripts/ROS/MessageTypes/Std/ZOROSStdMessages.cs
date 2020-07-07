using System;
namespace ZO.ROS.MessageTypes.Std {

    public class TimeMessage : ZOROSMessageInterface {

        [Newtonsoft.Json.JsonIgnore]
        string ZOROSMessageInterface.MessageType { get { return "std_msgs/Time"; } }
        public uint secs { get; set; }
        public uint nsecs { get; set; }

        public TimeMessage() {
            secs = 0;
            nsecs = 0;
        }

        public TimeMessage(uint secs, uint nsecs) {
            this.secs = secs;
            this.nsecs = nsecs;
        }

        /// <summary>
        /// Sets the timestamp to Now
        /// </summary>
        public void Now() {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.Now.ToUniversalTime() - epoch;
            double msecs = timeSpan.TotalMilliseconds;
            secs = (uint)(msecs / 1000);
            nsecs = (uint)((msecs / 1000 - secs) * 1e+9);
        }

    }

    public class HeaderMessage : ZOROSMessageInterface {

        [Newtonsoft.Json.JsonIgnore]
        string ZOROSMessageInterface.MessageType { get { return "std_msgs/Header"; } }

        //  Standard metadata for higher-level stamped data types.
        //  This is generally used to communicate timestamped data 
        //  in a particular coordinate frame.
        //  
        //  sequence ID: consecutively increasing ID 
        public uint seq { get; set; }
        // Two-integer timestamp that is expressed as:
        //  * stamp.sec: seconds (stamp_secs) since epoch (in Python the variable is called 'secs')
        //  * stamp.nsec: nanoseconds since stamp_secs (in Python the variable is called 'nsecs')
        //  time-handling sugar is provided by the client library
        public TimeMessage stamp { get; set; }
        // Frame this data is associated with
        //  0: no frame
        //  1: global frame
        public string frame_id { get; set; }

        public HeaderMessage() {
            this.seq = 0;
            this.stamp = new TimeMessage();
            this.frame_id = "";
        }

        public HeaderMessage(uint seq, TimeMessage stamp, string frame_id) {
            this.seq = seq;
            this.stamp = stamp;
            this.frame_id = frame_id;
        }

        /// <summary>
        /// Update the header timestamp and sequence
        /// </summary>
        public void Update() {
            // increment sequence
            this.seq++;

            // set the timestamp
            this.stamp.Now();
        }
    }


    /// <summary>
    /// Generic Int32 message
    /// </summary>
    public class Int32Message : ZOROSMessageInterface {

        [Newtonsoft.Json.JsonIgnore]
        string ZOROSMessageInterface.MessageType { get { return "std_msgs/Int32"; } }
        public Int32 data { get; set; }

        public Int32Message() {
            data = 0;
        }

        public Int32Message(Int32 data) {
            this.data = data;
        }
    }

    /// <summary>
    /// Generic string message.
    /// </summary>
    public class StringMessage : ZOROSMessageInterface {
        string ZOROSMessageInterface.MessageType { get { return "std_msgs/String"; } }

        public string data { get; set; }

        public StringMessage() {
            this.data = "";
        }

        public StringMessage(string data) {
            this.data = data;
        }
    }

    /// <summary>
    /// Generic bool setting service request message.
    /// </summary>
    public class SetBoolServiceRequest : ZOROSMessageInterface {
        string ZOROSMessageInterface.MessageType { get { return "std_srvs/SetBool"; } }

        public bool data { get; set; }

        public SetBoolServiceRequest() {
            this.data = true;
        }

        public SetBoolServiceRequest(bool data) {
            this.data = data;
        }
    }

    /// <summary>
    /// Generic bool setting service response message.
    /// </summary>
    public class SetBoolServiceResponse : ZOROSMessageInterface {
        string ZOROSMessageInterface.MessageType { get { return "std_srvs/SetBool"; } }

        public bool success { get; set; }
        public string message { get; set; }

        public SetBoolServiceResponse() {
            this.success = true;
            this.message = "";
        }

        public SetBoolServiceResponse(bool success, string message) {
            this.success = success;
            this.message = message;
        }
    }

    public class EmptyServiceRequest : ZOROSMessageInterface {
        string ZOROSMessageInterface.MessageType { get { return "std_srvs/Empty"; } }

    }
    
    public class EmptyServiceRespone : ZOROSMessageInterface {
        string ZOROSMessageInterface.MessageType { get { return "std_srvs/Empty"; } }

    }

}