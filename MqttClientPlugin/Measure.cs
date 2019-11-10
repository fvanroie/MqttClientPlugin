using System;
using System.Runtime.InteropServices;

namespace NetwiZe.MqttClientPlugin {
    internal class Measure {

        //public string inputStr; //The string returned in GetString is stored here
        public IntPtr StringBuffer; //Prevent marshalAs from causing memory leaks by clearing this before assigning
        internal string Name;
        internal Rainmeter.API Rainmeter { get; set; }

        internal void ClearBuffer() {
            if (StringBuffer != IntPtr.Zero) {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

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
}
