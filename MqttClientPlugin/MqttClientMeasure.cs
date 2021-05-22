/*
  Copyright (C) 2020 NetwiZe.be

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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text; // Encoding
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Rainmeter;

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

namespace NetwiZe.MqttClientPlugin
{

    internal class MqttClientMeasure : Measure
    {
        internal IntPtr Skin;
        internal int DebugLevel = 0;

        // MqttClient
        MqttFactory Factory = new MqttFactory();
        // IMqttClient MqttClient;
        IManagedMqttClient MqttClient;

        // This list of all parent measures is used by the child measures to find their parent.
        internal static List<MqttClientMeasure> ParentMeasures = new List<MqttClientMeasure>();
        internal static List<Rainmeter.API> ParentRainmeterApis = new List<Rainmeter.API>();

        // Server Properties
        internal String ClientId;
        internal String Server;
        internal ushort Port;
        internal String Username;
        internal SecureString Password;
        internal double RetryInterval;

        // Server Measure Bangs
        internal String[] OnConnectBangs;
        internal String[] OnDisconnectBangs;
        internal String[] OnReloadBangs;
        internal String[] OnMessageBangs;

        // All Topics of the Parent and Child Measures
        Hashtable Topics = new Hashtable();
        Hashtable Qos = new Hashtable();

        public bool IsConnected => MqttClient != null ? MqttClient.IsConnected : false;

        // The Topic of the Parent Measure
        String Topic;

        internal MqttClientMeasure(Rainmeter.API api)
        {
            ParentMeasures.Add(this);
            ParentRainmeterApis.Add(api);
            this.Rainmeter = api;
            this.Name = api.GetMeasureName();
            Skin = api.GetSkin();
            DebugLevel = (ushort)api.ReadInt("DebugLevel", 0);

            Server = api.ReadString("Server", "localhost");
            Port = (ushort)api.ReadInt("Port", 1883);
            RetryInterval = (ushort)api.ReadDouble("RetryInterval", 5.0);
            ClientId = api.ReadString("ClientId", Guid.NewGuid().ToString());
            Username = api.ReadString("Username", "");
            Password = new SecureString();
            foreach (char ch in api.ReadString("Password", "")) Password.AppendChar(ch);

            /* Mqtt Server Bangs */
            OnConnectBangs = SplitBangs(api.ReadString("OnConnect", ""));
            OnDisconnectBangs = SplitBangs(api.ReadString("OnDisconnect", ""));
            OnReloadBangs = SplitBangs(api.ReadString("OnReload", ""));
            OnMessageBangs = SplitBangs(api.ReadString("OnMessage", ""));

            MqttClient = Factory.CreateManagedMqttClient();

            /* Setup Event Handlers */
            MqttClient.UseConnectedHandler(e =>
            {
                if (!MqttClientMeasure.ParentRainmeterApis.Contains(Rainmeter)) { return; }

                Log(API.LogType.Notice, "Connected to " + Server + " : " + Port);

                if (OnConnectBangs.Length > 0)
                {
                    Log(API.LogType.Notice, "Executing OnConnect Bangs");
                    ExecuteBangs(OnConnectBangs);
                }
            });

            MqttClient.UseApplicationMessageReceivedHandler(e =>
           {
               if (!MqttClientMeasure.ParentRainmeterApis.Contains(Rainmeter)) { return; }

               e.GetType();
               String topic = e.ApplicationMessage.Topic;
               String payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
               try
               {
                   Debug("### RECEIVED APPLICATION MESSAGE ###", 3);
                   Debug($" >> Topic = {e.ApplicationMessage.Topic}", 4);
                   Debug($" >> Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}", 4);
                   Debug($" >> QoS = {e.ApplicationMessage.QualityOfServiceLevel}", 5);
                   Debug($" >> Retain = {e.ApplicationMessage.Retain}", 5);

                   if (Topics.Contains(topic))
                   {
                       Topics[topic] = payload;
                       Log(API.LogType.Notice, "Received update for " + topic);
                   }
                   else
                   {
                       Topics.Add(topic, payload);
                       Log(API.LogType.Warning, "Received payload for unknown topic " + topic);
                   }

                   if (OnMessageBangs.Length > 0)
                   {
                       Log(API.LogType.Notice, "Executing OnMessage Bangs");
                       ExecuteBangs(OnMessageBangs);
                   }
               }
               catch
               {
                   // Error Application
               }

           });

            MqttClient.UseDisconnectedHandler(e =>
           {
               if (!MqttClientMeasure.ParentRainmeterApis.Contains(Rainmeter)) { return; }

               Log(API.LogType.Error, e.Exception?.Message);
               Log(API.LogType.Error, e.AuthenticateResult?.ReasonString);
               Log(API.LogType.Error, e.ClientWasConnected.ToString());

               if (!MqttClient.IsConnected)
               {
                   Log(API.LogType.Warning, "Lost previous connection to " + Server + " : " + Port);
               }

               if (OnDisconnectBangs.Length > 0)
               {
                   Log(API.LogType.Notice, "Executing OnDisconnect Bangs");
                   ExecuteBangs(OnDisconnectBangs);
               }
           });

            try
            {
                Log(API.LogType.Warning, "Connecting to " + Server + " : " + Port + "...");
                ConnectAsync(Server, Port, Username, Password, ClientId).Wait();
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, "Exception trying to connect: " + ex);
                return;
            }
        }

        internal void ExecuteBangs(String[] bangs)
        {
            foreach (String bang in bangs)
            {
                Debug("Executing Bang: " + bang, 2);
                if (ParentMeasures.Contains(this))
                {
                    API.Execute(Skin, bang);
                }
            }
        }

        internal String[] SplitBangs(String input)
        {
            var result = new List<String>();
            int level = 0;
            StringBuilder output = new StringBuilder(input.Length);

            foreach (var character in input)
            {
                switch (character)
                {
                    case '[':
                        level++;
                        break;
                    case ']':
                        level--;
                        if (level == 0)
                        {
                            result.Add(output.ToString());
                            Debug(" - Adding new BANG: " + output.ToString(), 5);
                            output.Clear();
                        }
                        else if (level < 0)
                        {
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
        internal void Debug(String message, int level)
        {
            if (DebugLevel >= level)
            {
                Log(API.LogType.Debug, message);
            }
        }
        internal async void Log(API.LogType type, String message)
        {
            if (ParentMeasures.Contains(this) &&
                ParentRainmeterApis.Contains(Rainmeter))
            {
                try
                {
                    await Task.Run(() => Rainmeter.Log(type, message));
                    //Rainmeter.Log(API.LogType.Debug, message);
                }
                catch
                {
                    DebugLevel += 0;    // breakpoint
                }
            }
        }

        internal String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        internal override void Dispose()
        {
            ParentMeasures.Remove(this);
            ParentRainmeterApis.Remove(Rainmeter);
            Debug("Disposing Client Measure " + this.Name + " ...", 1);
            DisconnectAsync().Wait();
            MqttClient.Dispose();
            this.ClearBuffer();
        }

        private async Task ConnectAsync(String server, ushort port, String username, SecureString password, String clientID = null)
        {
            if (MqttClient.IsConnected)
            {
                Debug("Already connected...", 1);
                return;
            }

            if (clientID == null)
            {
                clientID = Guid.NewGuid().ToString();
            }

            var options = new MqttClientOptionsBuilder()
                .WithClientId(clientID)
                .WithTcpServer(server, port)
                .WithCleanSession(true)    // must be true for Managed Client, easier reconnects
                ;
            //.Build();

            // Only use authentication when a username or password is specified
            if (username != "" || SecureStringToString(password) != "") options = options.WithCredentials(username, SecureStringToString(password));


            var managedClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(RetryInterval))
                .WithClientOptions(options.Build())
                .Build();

            Debug("Connecting...", 1);
            //await MqttClient.ConnectAsync(options, CancellationToken.None);
            await MqttClient.StartAsync(managedClientOptions);
        }

        private async Task DisconnectAsync()
        {
            if (MqttClient.IsConnected)
            {
                Debug("Disconnecting", 1);
                //await MqttClient.DisconnectAsync();
                await MqttClient.StopAsync();
            }
        }

        private async Task PublishAsync(String topic, String value, byte qos = 0, bool flag = false)
        {

            var q = MqttQualityOfServiceLevel.AtMostOnce;
            if (qos == 1)
                q = MqttQualityOfServiceLevel.AtLeastOnce;
            else if (qos == 2)
                q = MqttQualityOfServiceLevel.ExactlyOnce;

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(value)
                .WithQualityOfServiceLevel(q)
                .WithRetainFlag(flag)
                .Build();
            await MqttClient.PublishAsync(message);
        }

        private async Task SubscribeAsync(String topic, byte qos)
        {
            MqttQualityOfServiceLevel mqttQos;

            switch (qos)
            {
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

            if (string.IsNullOrEmpty(topic))
            {
                throw new Exception("Topic cannot be empty.");
            }

            await MqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(mqttQos)
                    .Build()
                );
        }

        internal override void Reload(Rainmeter.API api, ref double maxValue)
        {
            Topic = api.ReadString("Topic", "");

            Debug("Reloading", 1);
            base.Reload(api, ref maxValue);
            Debug("Reloaded", 1);

            if (OnReloadBangs.Length > 0)
            {
                Log(API.LogType.Notice, "Executing OnReload Bangs");
                ExecuteBangs(OnReloadBangs);
            }

        }


        internal void Subscribe(String topic, byte qos)
        {
            if (!Qos.Contains(topic))
            {
                Qos.Add(topic, qos);
            }
            if (!Topics.Contains(topic))
            {
                Topics.Add(topic, "");
            }

            try
            {
                SubscribeAsync(topic, qos).Wait();
                Log(API.LogType.Notice, "Subscribed to " + topic);
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, ex.ToString());
            }
        }

        internal override void Publish(String topic, String value, byte qos = 0, bool retain = false)
        {
            //if (MqttClient.IsConnected) {
            Log(API.LogType.Notice, "Publish message " + topic + " = " + value + "," + qos.ToString() + "," + retain.ToString() + ")");
            try
            {
                PublishAsync(topic, value, qos, retain).Wait();
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    Log(API.LogType.Error, "Publish failed:" + ex);
                }
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, "Publish failed:" + ex);
            }

            //} else {
            //    Rainmeter.Log(API.LogType.Error, "Publish failed, client is not connected.");
            //}
        }

        internal override void ExecuteBang(String args)
        {
            Log(API.LogType.Notice, "Execute Bang: " + args);
            if (args.ToLower().StartsWith("publish("))
            {
                // format: publish(a,b,c,d)
                args = args.Substring(8).Trim(')');
                string[] arglist = args.Split(',');
                if (arglist.Length == 2)
                    Publish(arglist[0], arglist[1], 0, false);
                if (arglist.Length == 3 || arglist.Length == 4)
                {
                    try
                    {
                        var qos = Convert.ToByte(arglist[2]);
                        if (arglist.Length == 3)
                        {
                            Publish(arglist[0], arglist[1], qos);
                        }
                        else
                        {
                            bool retained = arglist[3].ToLower() == "true" || arglist[3] == "1";
                            Publish(arglist[0], arglist[1], qos, retained);
                        }
                    }
                    catch
                    {
                        Publish(arglist[0], arglist[1], 0, false);
                    }
                }
            }
            
        }

        internal override double Update()
        {
            // Rainmeter.Log(API.LogType.Debug, "Update " + Topic); OK
            return Convert.ToDouble(MqttClient.IsConnected);
        }

        internal override String GetString(String topic)
        {
            if (Topics.ContainsKey(topic))
            {
                Debug("GetString " + topic, 3);
                String value = Topics[topic].ToString();

                return value;
            }
            else
            {
                Debug("GetString " + topic + " not found", 1);
            }

            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

        internal override string GetString()
        {
            return (MqttClient.IsConnected ? "Connected" : "Disconnected");
        }

        internal double GetValue(String topic)
        {
            // Rainmeter.Log(API.LogType.Debug, "GetValue"); OK
            String strValue = GetString(topic);

            if (Double.TryParse(strValue, out double dblValue))
            {
                return dblValue;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
