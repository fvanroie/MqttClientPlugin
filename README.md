# MqttPlugin for Rainmeter

| WARNING: Work in progress! |
| --- |


## About

MqttPlugin allow you to use data from an MQTT broker in [Rainmeter](http://www.rainmeter.net).
You can subscribe to multiple topics and use the value in your meters.

Publishing to topics is not yet supported!


## Components

This project consists of 3 parts:

- API : The Rainmeter API files as published in the [Rainmeter Plugin SDK][1]
- M2Mqtt : [MQTT Client Library for .Net][2]
- MqttPlugin : The actual glue that binds the Rainmeter API and the Paho MQTT .Net client.

[1]:https://github.com/rainmeter/rainmeter-plugin-sdk
[2]:https://github.com/eclipse/paho.mqtt.m2mqtt

The MqttPlugin folder is a modified version of the [PluginParentChild example](https://github.com/rainmeter/rainmeter-plugin-sdk/tree/master/C%23/PluginParentChild) from the SDK.

> You can copy the API and M2Mqtt folders from the original projects if you like.


## Compilation

The solution (.sln) file can be built using the free [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/vs/community/).


## Installation

Copy the DLLs from bin\x86 or bin\x64 into your Rainmeter installation directory:
- M2Mqtt.Net.dll : Place this library into your Rainmeter folder where the rainmeter.exe is located.
- MqttPlugin.dll : Copy this plugin into the Plugins subfolder of the Rainmeter directory.


## Usage

See the [examples](tree/master/examples) folder for how to use the Measures.