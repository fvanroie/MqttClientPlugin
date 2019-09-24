Rainmeter Mqtt Plugin

> Work in Progress !!

This project consists of 3 parts:

- API : The Rainmeter API files as published in the [Rainmeter Plugin SDK][1]
- M2Mqtt : [MQTT Client Library for .Net][2]
- MqttPlugin : The actual glue that binds the Rainmeter API and the Paho MQTT .Net client.

[1]:https://github.com/rainmeter/rainmeter-plugin-sdk
[2]:https://github.com/eclipse/paho.mqtt.m2mqtt

The MqttPlugin is a modified version of the ParentChild Plugin exaple from the SDK.