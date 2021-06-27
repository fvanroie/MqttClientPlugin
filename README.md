# MqttPlugin for Rainmeter

[![GitHub release](https://img.shields.io/github/v/release/fvanroie/MqttClientPlugin?include_prereleases)](https://github.com/fvanroie/MqttClientPlugin/releases)
[![GitHub](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/fvanroie/MqttClientPlugin/blob/master/LICENSE)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](#Contributing)
[![GitHub issues](https://img.shields.io/github/issues/fvanroie/MqttClientPlugin.svg)](http://github.com/fvanroie/MqttClientPlugin/issues)
[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/fvanroie/MqttClientPlugin/Build%20Plugin?label=Build%20Plugin&logo=github&logoColor=%23dddddd)](https://github.com/fvanroie/MqttClientPlugin/actions?query=workflow%3A%22Build+Plugin%22)
[![Discord](https://img.shields.io/discord/538814618106331137?color=%237289DA&label=support&logo=discord&logoColor=white)][6]

Make your desktop interact with your IOT devices, like smart lights, power meters, temperature and humidity sensors, etc...
Monitor sensors and create buttons to trigger an action or scene on your HomeAutomation system.

## About

MqttClient Plugin allows you to use data from an MQTT broker within [Rainmeter](http://www.rainmeter.net).
You can subscribe to multiple topics and use their values in your measures and meters.
Publishing messages to MQTT topics is also supported.


## Components

This project consists of 2 folders:

- API : The Rainmeter API files as published in the [Rainmeter Plugin SDK][1]
- MqttClientPlugin : The actual glue that binds the Rainmeter API and the MQTTnet Client.

The MqttClientPlugin is based on the [PluginParentChild example](https://github.com/rainmeter/rainmeter-plugin-sdk/tree/master/C%23/PluginParentChild) from the SDK.

## Compilation

There is a dependency on 3 NuGet packages: [MQTTnet][3], [Newtonsoft.Json][4] and [Costura.Fody][5].
These are automatically downloaded and included when you open the project for the first time.

The solution (.sln) file can be built using the free [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/vs/community/).
If needed, right-click the solution item and run 'Restore NuGet packages' to install all dependencies.
Then Build the Solution. 

## Installation

The plugin is now completely self-contained. Just copy the `MqttClient.dll` file from bin\x86 or bin\x64 into your %appdata%\Rainmeter\Plugins directory.
Optionally, also copy the examples folder to Documents\Rainmeter\Skins.

> **_NOTE:_** If you previously installed v0.0.1 or v0.0.2, you can **remove** `M2Mqtt.dll` from your Rainmeter directory and **remove** `MqttPlugin.dll` from your Plugins directory. These aren't needed anymore.

An `.rmskin` package is also available on the [releases](https://github.com/fvanroie/MqttClientPlugin/releases) page.

## Usage

See the examples folder for how to use the Measures. Also check out the [documentation](https://fvanroie.github.io/MqttClientPlugin).

## Contributing

You are welcome to contribute to the development of this plugin:
- Share examples on how to use the MqttPlugin in Rainmeter skins
- File a Bug Report
- Feature requests

## Support

For support using MqttClient Plugin, please join the [#openHASP][6] on Discord.

[1]:https://github.com/rainmeter/rainmeter-plugin-sdk
[2]:https://github.com/eclipse/paho.mqtt.m2mqtt
[3]:https://github.com/chkr1011/MQTTnet
[4]:https://github.com/JamesNK/Newtonsoft.Json
[5]:https://github.com/Fody/Costura
[6]: https://discord.gg/VCWyuhF
