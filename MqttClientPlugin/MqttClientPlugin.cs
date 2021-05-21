using Rainmeter;
using System;
using System.Runtime.InteropServices;

namespace NetwiZe.MqttClientPlugin
{

    public static class MqttClientPlugin
    {

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Rainmeter.API api = new Rainmeter.API(rm);
            string parent = api.ReadString("ParentName", "");
            Measure measure;
            if (String.IsNullOrEmpty(parent))
            {
                measure = new MqttClientMeasure(api);
            }
            else
            {
                measure = new MqttTopicMeasure(api);
            }

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                measure.ClearBuffer();
                measure.StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }
            return measure.StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(args);
        }

        [DllExport]
        public static IntPtr Publish(IntPtr data, int argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            //If we are given two or more arguments
            if (argc == 1)
            {
                measure.Publish(argv[0], "");
                //measure.buffer = Marshal.StringToHGlobalUni("Pub");
            }
            else if (argc == 2)
            {
                measure.Publish(argv[0], argv[1]);
                //measure.buffer = Marshal.StringToHGlobalUni("Pub");
            }
            //If we are given more arguments
            else if (argc == 3 || argc == 4)
            {

                // try convert the string to a byte
                try
                {
                    var qos = Convert.ToByte(argv[2]);
                    
                    if (argc == 3)
                        measure.Publish(argv[0], argv[1], qos);
                    else
                    {
                        bool retained = argv[3].ToLower() == "true" || argv[3] == "1";
                        measure.Publish(argv[0], argv[1], qos, retained);
                    }
                }
                catch
                {
                    measure.Publish(argv[0], argv[1]);
                }


            }
            else
            {
                measure.Publish("atopic", "avalue");
                //measure.buffer = Marshal.StringToHGlobalUni("Arg count must be 2");
            }

            return Marshal.StringToHGlobalUni("");
        }
    }
}