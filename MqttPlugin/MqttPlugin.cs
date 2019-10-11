/*
  Copyright (C) 2019 NetwiZe.be

  This program is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Client.Subscribing;
//using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using Rainmeter;

using System.Text; // Encoding

using Newtonsoft.Json.Linq;


// Overview: This example demonstrates a basic implementation of a parent/child
// measure structure. In this particular example, we have a "parent" measure
// which contains the values for the options "ValueA", "ValueB", and "ValueC".
// The child measures are used to return a specific value from the parent.

// Use case: You could, for example, have a "main" parent measure that queries
// information some data set. The child measures can then be used to return
// specific information from the data queried by the parent measure.

// Sample skin:
/*
    [Rainmeter]
    Update=1000
    BackgroundMode=2
    SolidColor=000000

    [mParent]
    Measure=Plugin
    Plugin=ParentChild.dll
    ValueA=111
    ValueB=222
    ValueC=333
    Type=A

    [mChild1]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mParent
    Type=B

    [mChild2]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mParent
    Type=C

    [Text]
    Meter=STRING
    MeasureName=mParent
    MeasureName2=mChild1
    MeasureName3=mChild2
    X=5
    Y=5
    W=200
    H=55
    FontColor=FFFFFF
    Text="mParent: %1#CRLF#mChild1: %2#CRLF#mChild2: %3"
*/

namespace MqttPlugin {
    internal class Measure {

        //public string inputStr; //The string returned in GetString is stored here
        public IntPtr buffer; //Prevent marshalAs from causing memory leaks by clearing this before assigning
        public bool IsConnected;

        internal virtual void Dispose() {
        }

        internal virtual void Reload(Rainmeter.API api, ref double maxValue) {
        }

        internal virtual void ExecuteBang(String args) {
        }
        internal virtual void Publish(String topic, String value, byte qos = 0, bool retain = false) {
        }

        internal virtual double Update() {
            return 0.0;
        }

        internal virtual String GetString(String topic) {
            return "";
        }

        internal virtual String GetString() {
            return "";
        }
    }

    internal class ParentMeasure : Measure {
        // This list of all parent measures is used by the child measures to find their parent.
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();

        internal string Name;
        internal IntPtr Skin;

        internal String Server;
        internal ushort Port;
        internal String Username;
        internal String Password;
        internal String ClientId;
        internal String OnConnect;
        internal String OnReload;
        internal String OnMessage;

        MqttFactory Factory = new MqttFactory();
        IMqttClient MqttClient;

        Rainmeter.API Rainmeter { get; }
        // All Topics of the Parent and Child Measures
        Hashtable Topics = new Hashtable();
        Hashtable Qos = new Hashtable();
        // The Topic of the Parent Measure
        String Topic;

        internal ParentMeasure(Rainmeter.API api) {
            ParentMeasures.Add(this);
            Rainmeter = api;

            Topic = api.ReadString("Topic", "defaulttopic");

            Name = api.GetMeasureName();
            Skin = api.GetSkin();

            Server = api.ReadString("Server", "localhost");
            Port = (ushort)api.ReadInt("Port", 1833);
            ClientId = api.ReadString("ClientId", Guid.NewGuid().ToString());
            Username = api.ReadString("Username", "");
            Password = api.ReadString("Password", "");
            OnConnect = api.ReadString("OnConnect", "");
            OnReload = api.ReadString("OnReload", "");
            OnMessage = api.ReadString("OnMessage", "");

            MqttClient = Factory.CreateMqttClient();
            MqttClient.UseConnectedHandler(async e => {
                Rainmeter.Log(API.LogType.Notice, "Connected to " + Server + " : " + Port);

                // Subscribe to all previous topics
                var message = new MQTTnet.MqttApplicationMessage();
                foreach (var topic in Topics.Keys) {
                    //await SubscribeAsync(topic.ToString(), 0);
                    await Task.Run(() => MqttClient.SubscribeAsync(topic.ToString(), 0));
                    Rainmeter.Log(API.LogType.Notice, "==> Subscribed to " + topic);
                    // await MqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());
                }
                
//                await Task.Run(() => MqttClient.PublishAsync(message));

                if (OnConnect != "") {
                    Rainmeter.Log(API.LogType.Debug, "Executing OnConnect Bang " + OnConnect);
                    API.Execute(Skin, OnConnect);
                }

            });

            MqttClient.UseApplicationMessageReceivedHandler(e => {
                e.GetType();
                String topic = e.ApplicationMessage.Topic;
                String payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                try {
                    Rainmeter.Log(API.LogType.Debug, "### RECEIVED APPLICATION MESSAGE ###");
                    Rainmeter.Log(API.LogType.Debug, $"+ Topic = {e.ApplicationMessage.Topic}");
                    Rainmeter.Log(API.LogType.Debug, $"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Rainmeter.Log(API.LogType.Debug, $"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Rainmeter.Log(API.LogType.Debug, $"+ Retain = {e.ApplicationMessage.Retain}");

                    if (Topics.Contains(topic)) {
                        Topics[topic] = payload;
                    } else {
                        Topics.Add(topic, payload);
                        Rainmeter.Log(API.LogType.Warning, "Received payload for unknown topic " + topic);
                    }

                    if (OnMessage != "") {
                        Rainmeter.Log(API.LogType.Debug, "Executing OnMessage Bang " + OnMessage);
                        API.Execute(Skin, OnMessage);
                    }
                }
                catch {
                    // Error Application
                }

            });

            MqttClient.UseDisconnectedHandler(async e => {
                if (!MqttClient.IsConnected) {
                    Rainmeter.Log(API.LogType.Warning, "Lost previous connection to " + Server + " : " + Port);
                }

                // await Task.Delay(TimeSpan.FromSeconds(5));

                try {
                    // ConnectAsync(Server, Port, Username, Password, ClientId).Wait();
                }
                catch {
                    // Rainmeter.Log(API.LogType.Warning, "### RECONNECTING FAILED ###");
                }
            });

            try {
                Rainmeter.Log(API.LogType.Warning, "Connecting to " + Server + " : " + Port);
                ConnectAsync(Server, Port, Username, Password, ClientId).Wait();
            }
            catch (Exception ex) {
                Rainmeter.Log(API.LogType.Error, "Exception trying to connect: " + ex);
                return;
            }
            finally {
            }
        }

        internal override void Dispose() {
            ParentMeasures.Remove(this);
        }

        private async Task ConnectAsync(String server, ushort port = 1833, String username = "", String password = "", String clientID = null) {
            if (MqttClient.IsConnected) {
                Rainmeter.Log(API.LogType.Debug, "Already connected...");
                return;
            }

            if (clientID == null) {
                clientID = Guid.NewGuid().ToString();
            }

            var options = new MqttClientOptionsBuilder()
                .WithClientId(clientID)
                .WithTcpServer(server, port)
                .WithCredentials(username, password)
                .WithCleanSession(false)
                .Build();

            Rainmeter.Log(API.LogType.Debug, "Connecting...");
            await MqttClient.ConnectAsync(options, CancellationToken.None);
        }


        private async Task DisconnectAsync() {
            if (MqttClient.IsConnected) {
                Rainmeter.Log(API.LogType.Debug, "Disconnecting");
                await MqttClient.DisconnectAsync();
            }
        }

        private async Task PublishAsync(String topic, String value) {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(value)
                .WithExactlyOnceQoS()
                // .WithRetainFlag()
                .Build();
            await MqttClient.PublishAsync(message);
        }

        private async Task SubscribeAsync(String topic, byte qos) {
            MqttQualityOfServiceLevel mqttQos;

            switch (qos) {
                case 0:
                    mqttQos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce;
                    break;

                case 1:
                    mqttQos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce;
                    break;

                case 2:
                    mqttQos = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce;
                    break;

                default:
                    throw new Exception("Invalid Qos value (0-2).");
            }

            if (string.IsNullOrEmpty(topic)) {
                throw new Exception("Topic cannot be empty.");
            }

            var result = (await MqttClient.SubscribeAsync(
                    new TopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(mqttQos)
                    .Build()
                )).Items[0];


            switch (result.ResultCode) {
                case MqttClientSubscribeResultCode.GrantedQoS0:
                case MqttClientSubscribeResultCode.GrantedQoS1:
                case MqttClientSubscribeResultCode.GrantedQoS2:

                    /*                    MqttClient.UseApplicationMessageReceivedHandler(me =>
                                        {
                                            var msg = me.ApplicationMessage;
                                            var data = Encoding.UTF8.GetString(msg.Payload);
                                            Rainmeter.Log(API.LogType.Debug, "Subscribed to " + data);
                                        });
                                        */
                    break;
                default:
                    throw new Exception(result.ResultCode.ToString());
            }

        }

        internal override void Reload(Rainmeter.API api, ref double maxValue) {
            Topic = api.ReadString("Topic", "");

            Rainmeter.Log(API.LogType.Debug, "Reloading");
            base.Reload(api, ref maxValue);
            Rainmeter.Log(API.LogType.Debug, "Reloaded");

            if (OnReload != "") {
                Rainmeter.Log(API.LogType.Debug, "Executing OnReload Bang " + OnReload);
                API.Execute(Skin, OnReload);
            }

        }


        internal void Subscribe(String topic, byte qos) {
            if (!Qos.Contains(topic)) {
                Qos.Add(topic, qos);
            }
            if (!Topics.Contains(topic)) {
                Topics.Add(topic, "");
            }

            /*String[] topics = new String[Qos.Count];
            byte[] qosLevels = new Byte[Qos.Count];
            Qos.Keys.CopyTo(topics, 0);
            Qos.Values.CopyTo(qosLevels, 0);*/

            try {
                SubscribeAsync(topic, qos).Wait();
                Rainmeter.Log(API.LogType.Notice, "Subscribed to " + topic);
            }
            catch (Exception ex) {
                Rainmeter.Log(API.LogType.Error, ex.ToString());
            }
        }

        internal override void Publish(String topic, String value, byte qos = 0, bool retain = false) {
            if (MqttClient.IsConnected) {
                Rainmeter.Log(API.LogType.Notice, "Publish message " + topic + " = " + value);
                try {
                    PublishAsync(topic, value).Wait();
                }
                catch (Exception ex) {
                    Rainmeter.Log(API.LogType.Error, "Publish failed:" + ex);
                }

            } else {
                Rainmeter.Log(API.LogType.Error, "Publish failed, client is not connected.");
            }
        }

        internal override void ExecuteBang(String args) {
            Publish(Topic, args, 0, false);
        }

        internal override double Update() {
            Rainmeter.Log(API.LogType.Debug, "Update " + Topic);

/*            if (!MqttClient.IsConnected && FailedConnects < 2) {
                try {
                    ConnectAsync(Server, Port, Username, Password, ClientId).Wait();
                }
                catch (Exception ex) {

                }
                if (!MqttClient.IsConnected) {
                    FailedConnects++;
                    Rainmeter.Log(API.LogType.Warning, "Client failed to reconnect to " + Server + " : " + Port);
                } else {
                    FailedConnects = 0;
                }
            }*/

            return GetValue();
        }

        internal override String GetString(String topic) {
            if (Topics.ContainsKey(topic)) {
                Rainmeter.Log(API.LogType.Debug, "GetString " + topic);
                String value = Topics[topic].ToString();

                return value;
            } else {
                Rainmeter.Log(API.LogType.Debug, "GetString " + topic + " not found");
            }

            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

        internal double GetValue() {
            Rainmeter.Log(API.LogType.Debug, "GetValue");
            return 0.0;
        }

    }

    internal class ChildMeasure : Measure {
        private ParentMeasure ParentMeasure = null;
        // The Topic of the Child Measure
        String Topic;
        String Property;
        Rainmeter.API Rainmeter { get; }

        internal ChildMeasure(Rainmeter.API api) {
            Rainmeter = api;
        }


        internal override void Reload(Rainmeter.API api, ref double maxValue) {
            Topic = api.ReadString("Topic", "defaulttopic");
            Property = api.ReadString("Property", "");
            var qos = api.ReadInt("Qos", 0);
            base.Reload(api, ref maxValue);

            string parentName = api.ReadString("ParentName", "");
            IntPtr skin = api.GetSkin();

            // Find parent using name AND the skin handle to be sure that it's the right one.
            ParentMeasure = null;
            foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures) {
                if (parentMeasure.Skin.Equals(skin) && parentMeasure.Name.Equals(parentName)) {
                    ParentMeasure = parentMeasure;
                    ParentMeasure.Subscribe(Topic, (byte)qos);
                }
            }

            if (ParentMeasure == null) {
                api.Log(API.LogType.Error, "ParentChild.dll: ParentName=" + parentName + " not valid");
            }
        }

        internal override double Update() {
            if (ParentMeasure != null) {
                return ParentMeasure.GetValue();
            }

            return 0.0;
        }
        internal override String GetString() {
            if (ParentMeasure != null) {
                String data = ParentMeasure.GetString(Topic);

                if (Property != "") {
                    try {
                        JObject o = JObject.Parse(data);
                        data = (string)o.SelectToken(Property).ToString();
                    }
                    catch {
                        Rainmeter.Log(API.LogType.Warning, Property + " not valid");
                    }
                    return data;
                } else {
                    return data;
                }
            }

            return "";
        }

    }

    public static class Plugin {
        static IntPtr StringBuffer = IntPtr.Zero;
        static Rainmeter.API Rainmeter;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm) {
            Rainmeter.API api = new Rainmeter.API(rm);

            string parent = api.ReadString("ParentName", "");
            Measure measure;
            if (String.IsNullOrEmpty(parent)) {
                measure = new ParentMeasure((Rainmeter.API)rm);
            } else {
                measure = new ChildMeasure((Rainmeter.API)rm);
            }

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
        }

        [DllExport]
        public static void Finalize(IntPtr data) {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue) {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data) {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data) {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero) {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null) {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args) {
            Rainmeter.Log(API.LogType.Debug, "Plugin Execute Bang");

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(args);
        }


        [DllExport]
        public static IntPtr Publish(IntPtr data, int argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv) {
            // Rainmeter.Log(API.LogType.Debug, "Plugin Publish"); // OK

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (measure.buffer != IntPtr.Zero) {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            //If we are given two or more arguments
            if (argc == 1) {
                measure.Publish(argv[0], "");
                measure.buffer = Marshal.StringToHGlobalUni("Pub");
            } else if (argc == 2) {
                measure.Publish(argv[0], argv[1]);
                measure.buffer = Marshal.StringToHGlobalUni("Pub");
            }
              //If we are given more arguments
              else {
                measure.Publish("atopic", "avalue");
                measure.buffer = Marshal.StringToHGlobalUni("Arg count must be 2");
            }

            return Marshal.StringToHGlobalUni("");
        }

    }
}
