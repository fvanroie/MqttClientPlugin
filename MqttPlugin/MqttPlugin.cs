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
//using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
//using MQTTnet.Formatter;
using MQTTnet.Protocol;
//using MQTTnet.Client.Subscribing;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Text.RegularExpressions;
using System.Security;
//using System.Threading;
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

    [mServer]
    Measure=Plugin
    Plugin=MqttPlugin.dll
    Server=mqtthost.local
    Port=1833
    Username=myuser
    Password=mypass

    [mTopic1]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mServer
    Topic=/stat/mything/topic

    [mTopic2]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mServer
    Topic=/stat/otherting/topic

    [Text]
    Meter=STRING
    MeasureName=mServer
    MeasureName2=mTopic1
    MeasureName3=mTopic2
    X=5
    Y=5
    W=200
    H=55
    FontColor=FFFFFF
    Text="mServer: %1#CRLF#mTopic1: %2#CRLF#mTopic: %3"
*/

namespace MqttPlugin {
    internal class Measure {

        //public string inputStr; //The string returned in GetString is stored here
        public IntPtr buffer; //Prevent marshalAs from causing memory leaks by clearing this before assigning

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
        internal Rainmeter.API Rainmeter { get; }
        internal string Name;
        internal IntPtr Skin;
        internal int DebugLevel = 0;

        // MqttClient
        MqttFactory Factory = new MqttFactory();
        // IMqttClient MqttClient;
        IManagedMqttClient MqttClient;

        // This list of all parent measures is used by the child measures to find their parent.
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();

        // Server Properties
        internal String ClientId;
        internal String Server;
        internal ushort Port;
        internal String Username;
        internal SecureString Password;

        // Server Measure Bangs
        internal String[] OnConnectBangs;
        internal String[] OnDisconnectBangs;
        internal String[] OnReloadBangs;
        internal String[] OnMessageBangs;

        Rainmeter.API Rainmeter { get; }
        // All Topics of the Parent and Child Measures
        Hashtable Topics = new Hashtable();
        Hashtable Qos = new Hashtable();

        public bool IsConnected => MqttClient != null ? MqttClient.IsConnected : false;

        // The Topic of the Parent Measure
        String Topic;

        internal void Debug(String message, int level) {
            if (DebugLevel >= level) {
                Rainmeter.Log(API.LogType.Debug, message);
            }
        }

        internal ParentMeasure(Rainmeter.API api) {
            ParentMeasures.Add(this);
            Rainmeter = api;

            Topic = api.ReadString("Topic", "defaulttopic");

            Name = api.GetMeasureName();
            Skin = api.GetSkin();
            DebugLevel = (ushort)api.ReadInt("DebugLevel", 0);

            Server = api.ReadString("Server", "localhost");
            Port = (ushort)api.ReadInt("Port", 1833);
            ClientId = api.ReadString("ClientId", Guid.NewGuid().ToString());
            Username = api.ReadString("Username", "");
            Password = new SecureString();
            foreach (char ch in api.ReadString("Password", "")) Password.AppendChar(ch);
            
            /* Mqtt Server Bangs */
            OnConnectBangs = SplitBangs( api.ReadString("OnConnect", "") );
            OnDisconnectBangs = SplitBangs( api.ReadString("OnConnect", "") );
            OnReloadBangs = SplitBangs( api.ReadString("OnReload", "") );
            OnMessageBangs = SplitBangs( api.ReadString("OnMessage", "") );

            // MqttClient = Factory.CreateMqttClient();
            MqttClient = Factory.CreateManagedMqttClient();

            MqttClient.UseConnectedHandler(e => {
                Rainmeter.Log(API.LogType.Notice, "Connected to " + Server + " : " + Port);

                /* This is now handled by the Managed MqttClient
                // Subscribe to all previous topics
                var message = new MQTTnet.MqttApplicationMessage();
                foreach (var topic in Topics.Keys) {
                    //await SubscribeAsync(topic.ToString(), 0);
                    await Task.Run(() => MqttClient.SubscribeAsync(topic.ToString(), 0));
                    Rainmeter.Log(API.LogType.Notice, "==> Subscribed to " + topic);
                    // await MqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());
                }
                */
//                await Task.Run(() => MqttClient.PublishAsync(message));

                if (OnConnectBangs.Length > 0) {
                    Rainmeter.Log(API.LogType.Notice, "Executing OnConnect Bangs");
                    ExecuteBangs(OnConnectBangs);
                }

            });

            MqttClient.UseApplicationMessageReceivedHandler(e => {
                e.GetType();
                String topic = e.ApplicationMessage.Topic;
                String payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                try {
                    Debug( "### RECEIVED APPLICATION MESSAGE ###", 3);
                    Debug( $" >> Topic = {e.ApplicationMessage.Topic}", 4);
                    Debug( $" >> Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}", 4);
                    Debug( $" >> QoS = {e.ApplicationMessage.QualityOfServiceLevel}", 5);
                    Debug( $" >> Retain = {e.ApplicationMessage.Retain}", 5);

                    if (Topics.Contains(topic)) {
                        Topics[topic] = payload;
                        Rainmeter.Log(API.LogType.Notice, "Received update for " + topic);
                    } else {
                        Topics.Add(topic, payload);
                        Rainmeter.Log(API.LogType.Warning, "Received payload for unknown topic " + topic);
                    }

                    if (OnMessageBangs.Length > 0) {
                        Rainmeter.Log(API.LogType.Notice, "Executing OnMessage Bangs");
                        ExecuteBangs(OnMessageBangs);
                    }
                }
                catch {
                    // Error Application
                }

            });

            MqttClient.UseDisconnectedHandler(e => {

                Rainmeter.Log(API.LogType.Error, e.Exception.Message);
                Rainmeter.Log(API.LogType.Error, e.AuthenticateResult.ReasonString);
                Rainmeter.Log(API.LogType.Error, e.ClientWasConnected.ToString());

                if (!MqttClient.IsConnected) {
                    Rainmeter.Log(API.LogType.Warning, "Lost previous connection to " + Server + " : " + Port);
                }

                if (OnDisconnectBangs.Length > 0) {
                    Rainmeter.Log(API.LogType.Notice, "Executing OnDisconnect Bangs");
                    ExecuteBangs(OnDisconnectBangs);
                }

                /* await Task.Delay(TimeSpan.FromSeconds(5));

                try {
                    ConnectAsync(Server, Port, Username, Password, ClientId).Wait();
                }
                catch {
                    Rainmeter.Log(API.LogType.Warning, "### RECONNECTING FAILED ###");
                }*/
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

        internal void ExecuteBangs(String[] bangs) {
            foreach (String bang in bangs) {
                Debug( "Executing Bang: " + bang, 2);
                API.Execute(Skin, bang);
            }
        }

        internal String[] SplitBangs(String input) {
            var result = new List<String>();
            int level = 0;
            StringBuilder output = new StringBuilder(input.Length);

            foreach (var character in input) {
                switch (character) {
                    case '[':
                        level++;
                        break;
                    case ']':
                        level--;
                        if (level == 0) {
                            result.Add(output.ToString());
                            Debug( " - Adding new BANG: " + output.ToString(), 5);
                            output.Clear();
                        } else if (level < 0) {
                            level = 0;
                        }
                            break;
                    default:
                        output.Append(character);
                        break;
                }
            }

            return result.ToArray();
        }

        String SecureStringToString(SecureString value) {
            IntPtr valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        internal override void Dispose() {
            Rainmeter.Log(API.LogType.Debug, "Disposing Measure...");
            DisconnectAsync().Wait();
            MqttClient.Dispose();
            ParentMeasures.Remove(this);
        }

        private async Task ConnectAsync(String server, ushort port, String username, SecureString password, String clientID = null) {
            if (MqttClient.IsConnected) {
                Debug( "Already connected...", 1);
                return;
            }

            if (clientID == null) {
                clientID = Guid.NewGuid().ToString();
            }

            var options = new MqttClientOptionsBuilder()
                .WithClientId(clientID)
                .WithTcpServer(server, port)
                .WithCredentials(username, SecureStringToString(password))
                .WithCleanSession(true)    // must be true for Managed Client, easier reconnects
                .Build();

            var managedClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            Debug( "Connecting...", 1);
            //await MqttClient.ConnectAsync(options, CancellationToken.None);
            await MqttClient.StartAsync(managedClientOptions);
        }

        private async Task DisconnectAsync() {
            if (MqttClient.IsConnected) {
                Debug( "Disconnecting", 1);
                //await MqttClient.DisconnectAsync();
                await MqttClient.StopAsync();
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

            await MqttClient.SubscribeAsync(
                    new TopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(mqttQos)
                    .Build()
                );

            /*            var result = (await MqttClient.SubscribeAsync(
                                new TopicFilterBuilder()
                                .WithTopic(topic)
                                .WithQualityOfServiceLevel(mqttQos)
                                .Build()
                            )).Items[0];


                        switch (result.ResultCode) {
                            case MqttClientSubscribeResultCode.GrantedQoS0:
                            case MqttClientSubscribeResultCode.GrantedQoS1:
                            case MqttClientSubscribeResultCode.GrantedQoS2:

                                                //    MqttClient.UseApplicationMessageReceivedHandler(me =>
                                                  //  {
                                                  //      var msg = me.ApplicationMessage;
                                                  //      var data = Encoding.UTF8.GetString(msg.Payload);
                                                  //      Rainmeter.Log(API.LogType.Debug, "Subscribed to " + data);
                                                  //  });
                                break;
                            default:
                                throw new Exception(result.ResultCode.ToString());
                        }
            */
        }

        internal override void Reload(Rainmeter.API api, ref double maxValue) {
            Topic = api.ReadString("Topic", "");

            Debug( "Reloading", 1);
            base.Reload(api, ref maxValue);
            Debug( "Reloaded", 1);

            if (OnReloadBangs.Length > 0) {
                Rainmeter.Log(API.LogType.Notice, "Executing OnReload Bangs");
                ExecuteBangs(OnReloadBangs);
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
            //if (MqttClient.IsConnected) {
                Rainmeter.Log(API.LogType.Notice, "Publish message " + topic + " = " + value);
                try {
                    PublishAsync(topic, value).Wait();
                }
                catch (AggregateException e) {
                    foreach (var ex in e.InnerExceptions) {
                        Rainmeter.Log(API.LogType.Error, "Publish failed:" + ex);
                    }
                }
                catch (Exception ex) {
                    Rainmeter.Log(API.LogType.Error, "Publish failed:" + ex);
                }

            //} else {
            //    Rainmeter.Log(API.LogType.Error, "Publish failed, client is not connected.");
            //}
        }

        internal override void ExecuteBang(String args) {
            Publish(Topic, args, 0, false);
        }

        internal override double Update() {
            // Rainmeter.Log(API.LogType.Debug, "Update " + Topic); OK
            return Convert.ToDouble( MqttClient.IsConnected );
        }

        internal override String GetString(String topic) {
            if (Topics.ContainsKey(topic)) {
                Debug( "GetString " + topic, 3);
                String value = Topics[topic].ToString();

                return value;
            } else {
                Debug( "GetString " + topic + " not found", 1);
            }

            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

        internal override string GetString() {
            return (MqttClient.IsConnected ? "Connected" : "Disconnected");
        }

        internal double GetValue(String topic) {
            // Rainmeter.Log(API.LogType.Debug, "GetValue"); OK
            String strValue = GetString(topic);

            if (Double.TryParse(strValue, out double dblValue)) {
                return dblValue;
            } else {
                return 0.0;
            }
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
                // Child Topic value
                return ParentMeasure.GetValue(Topic);
            } else {
                // Server Connection state
                return Convert.ToDouble(ParentMeasure.IsConnected);
            }
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
