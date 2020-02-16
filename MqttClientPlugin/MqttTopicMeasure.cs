using Newtonsoft.Json.Linq;
using Rainmeter;
using System;

namespace NetwiZe.MqttClientPlugin
{
    internal class MqttTopicMeasure : Measure
    {
        private MqttClientMeasure ParentMeasure = null;
        // The Topic of the Child Measure
        String Topic;
        String Property;
        String ParentName;
        IntPtr Skin;
        internal int DebugLevel = 0;

        internal MqttTopicMeasure(Rainmeter.API api)
        {
            this.Rainmeter = api;
            this.Name = api.GetMeasureName();
        }

        internal void Debug(String message, int level)
        {
            if (DebugLevel >= level)
            {
                Log(API.LogType.Debug, message);
            }
        }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        internal async void Log(API.LogType type, String message)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            if (MqttClientMeasure.ParentMeasures.Contains(ParentMeasure) &&
                MqttClientMeasure.ParentRainmeterApis.Contains(Rainmeter))
            {
                try
                {
                    //    await Task.Run(() => Rainmeter.Log(type, message));
                    //    Rainmeter.Log(API.LogType.Debug, message);
                }
                catch
                {
                    ParentMeasure.DebugLevel += 0;    // breakpoint
                }
            }
        }

        internal override void Dispose()
        {
            Debug("Disposing Topic Measure " + this.Name + " ...", 1);
            this.ClearBuffer();
        }

        internal override void Reload(Rainmeter.API api, ref double maxValue)
        {
            Rainmeter = api;
            base.Reload(api, ref maxValue);

            Topic = api.ReadString("Topic", "defaulttopic");
            Property = api.ReadString("Property", "");
            var qos = api.ReadInt("Qos", 0);

            ParentName = api.ReadString("ParentName", "");
            Skin = api.GetSkin();

            // Find parent using name AND the skin handle to be sure that it's the right one.
            ParentMeasure = null;
            foreach (MqttClientMeasure parentMeasure in MqttClientMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(Skin) && parentMeasure.Name.Equals(ParentName))
                {
                    ParentMeasure = parentMeasure;
                    try
                    {
                        DebugLevel = parentMeasure.DebugLevel;
                        ParentMeasure.Subscribe(Topic, (byte)qos);
                    }
                    catch
                    {
                        Debug("Error Subscribing !", 1);
                    }
                }
            }

            if (ParentMeasure == null)
            {
                Log(API.LogType.Error, "ParentChild.dll: ParentName=" + ParentName + " not valid");
            }
        }

        internal override double Update()
        {
            // Check is the ParentMeasure is still there.

            // Find parent using name AND the skin handle to be sure that it's the right one.
            ParentMeasure = null;
            foreach (MqttClientMeasure parentMeasure in MqttClientMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(Skin) && parentMeasure.Name.Equals(ParentName))
                {
                    ParentMeasure = parentMeasure;
                    try
                    {
                        // Child Topic value
                        return ParentMeasure.GetValue(Topic);
                    }
                    catch
                    {
                        Debug("Error Updating !", 1);
                    }
                }
            }

            return 0.0;
        }

        internal override String GetString()
        {
            // Find parent using name AND the skin handle to be sure that it's the right one.
            ParentMeasure = null;
            foreach (MqttClientMeasure parentMeasure in MqttClientMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(Skin) && parentMeasure.Name.Equals(ParentName))
                {
                    ParentMeasure = parentMeasure;
                    try
                    {

                        Debug(Topic, 5);
                        String data = ParentMeasure.GetString(Topic);

                        if (Property != "")
                        {
                            try
                            {
                                JObject o = JObject.Parse(data);
                                data = (string)o.SelectToken(Property).ToString();
                            }
                            catch
                            {
                                Log(API.LogType.Warning, Property + " not valid");
                            }
                            return data;
                        }
                        else
                        {
                            return data;
                        }

                    }
                    catch
                    {
                        Debug("Error Retrieving String !", 1);
                    }
                }
            }

            return "";
        }
    }
}