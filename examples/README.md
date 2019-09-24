# MqttPlugin for Rainmeter

## Examples

- Hello World

## Server

First define your server IP ot hostname. Optionally include the username and password:

```
[mqttServer]
Measure=Plugin
Plugin=MqttPlugin.dll
Server=192.168.1.2
```

## Topics

Then you can create multiple subscribe topics. Each topics needs to reference the server using the ParentName parameter:

```
[mqttTopic1]
Measure=Plugin
Plugin=MqttPlugin.dll
ParentName=mqttServer
Topic=mytopics/hello
Qos=0
```
The Qos parameter is optional.

## Meters

You can then use the Measures in your meter, like you normally would:

```
[Text]
Meter=STRING
MeasureName=mqttServer
MeasureName2=mqttTopic1
MeasureName3=mqttTopic2
X=5
Y=5
W=300
H=120
FontColor=FFFFFF
FontSize=24
Text="mqttServer: %1#CRLF#mqttTopic1: %2#CRLF#mqttTopic2: %3"
```

## Result

![Image of Hello World example](https://raw.githubusercontent.com/fvanroie/MqttPlugin/master/examples/Hello%20World/helloworld.png)
