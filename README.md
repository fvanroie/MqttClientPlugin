# MqttPlugin for Rainmeter

| WARNING: Work in progress! |
| --- |


## About

MqttPlugin allows you to use data from an MQTT broker within [Rainmeter](http://www.rainmeter.net).
You can subscribe to multiple topics and use their values in your measures and meters.
Publishing messages to MQTT topics is also supported.


## Components

This project consists of 2 folders:

- API : The Rainmeter API files as published in the [Rainmeter Plugin SDK][1]
- MqttPlugin : The actual glue that binds the Rainmeter API and the MQTTnet Client.

[1]:https://github.com/rainmeter/rainmeter-plugin-sdk
[2]:https://github.com/eclipse/paho.mqtt.m2mqtt

The MqttPlugin is based on the [PluginParentChild example](https://github.com/rainmeter/rainmeter-plugin-sdk/tree/master/C%23/PluginParentChild) from the SDK.

## Compilation

There is a dependency on 3 NuGet packages: MQTTnet, Newtonsoft.Json and Costura.Fody.
These are automatically downloaded and included when you open the project for the first time.

The solution (.sln) file can be built using the free [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/vs/community/).
If needed, right-click the solution item and run 'Restore NuGet packages' to install all dependencies.
Then Build the Solution. 

## Installation

The plugin is now completely self-contained. Just copy the `MqttPlugin.dll` file from bin\x86 or bin\x64 into your %appdata%\Rainmeter\Plugins directory.
Optionally, also copy the examples folder to Documents\Rainmeter\Skins.

_Note:_ If you previously installed v0.0.1 or v0.0.2, you can **remove** `M2Mqtt.dll` from your Rainmeter directory and **remove** `MqttPlugin.dll` from your Plugins directory.

An `.rmskin` package is not yet available, but will be released once the MqttPlugin is ready for beta testing.

## Usage

See the examples folder for how to use the Measures.