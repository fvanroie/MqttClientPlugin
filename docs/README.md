# MqttClient Rainmeter Plugin
A plugin for Rainmeter that implements an MQTT client which allows you to publish and subscribe to topics on an MQTT broker service.
Included in this repo is an example skin that shows how to use every measure and bang.

### Current state:
The plugin is still under development, but it can be used for alpha testing and development of your own skins.
It is *not* advised to distribute this plugin in your own .rmskin files until a stable version is released.

### Functionality:
- Connect to a broker service using the Client Measure
- Can fire Bangs on Connect, Message, Disconnect and/or Reload events
- Subscribe to multiple topics using Topic Measures
- Extract a JsonPath property from Json messages
- Use Bangs to Publish, Subscribe or Unsubscribe to topics
- Use Functions to Publish, Subscribe or Unsubscribe to topics

## Measure types:

### Client Measure

The client parameters for connecting to the Mqtt broker service.

  Example:
  ```ini
  [myMqttClient]
  Type     = Plugin
  Plugin   = MqttClient
  ClientID = MyRmClient
  Server   = mqttbroker.local
  Port     = 1883
  Username = testuser
  Password = my$3cr3t
  ```

- `ClientID` *(optional)*

  The ClientID used to connect to the broker. If the ClientID is empty or omitted, a random GUID is used.

- `Server` *(optional)*

  The hostname or IP address of the Mqtt broker. Defaults to `localhost`.
  
- `Port` *(optional)*

  The Mqtt port of the broker service. Defaults to `1883`.

- `Username`, `Password` *(optional)*

  Credentials to authenticate with the broker.

- ~~`UseTls` _[ **yes**|no|true|false|0|1 ] (optional)_~~

  Connect to the broker using secure TLS protocol. Defaults to `yes`.

- ~~`CertificateCheck` _[ **yes**|no|true|false|0|1 ] (optional)_~~

  Check if the server certificate is valid. Defaults to `yes`.
  Can be useful to skip the certificate check of self-signed certificates while testing.

- `OnConnect`, `OnMessage`, `OnDisconnect`, `OnReload` *(optional)*

  One or more Bangs that will be fired when the associated event happens.

  Example:
  ```
  OnMessage = [!UpdateMeter myMeter][!Redraw]
  ```
  
- `DebugLevel`  *[ **0** - 5 ] (optional)*

  The verbosity level of the debug log messages. Ranges from 0 to 5, defaults to `0`.
  

### Topic Measures:

One or more Topic measures can be defined to subscribe to topics on the broker.
The value of these measures can be used in your meters to display the payload of those topics.

  Example:
  ```ini
  [myFirstTopic]
  Type       = Plugin
  Plugin     = MqttClient
  ParentName = myMqttClient
  Topic      = stat/mytopic/first
  Qos        = 0
  ```

- `ParentName` *(required)*

  The *section name* of the Mqtt Client measure.
  This parameter indicates which broker will receive the subscription.

- `Topic` *(required)*

  The topic to subscribe to on the broker.
  
- `Qos` [**0** - 2] *(optional)*

  The Quallity of Service level of the subscription. Defaults to `0`.
  
- `Property` *(experimental, optional)*

  The JsonPath of the property to be returned. This item can be used to retrieve only the specified property of the Json payload in the topic.
  You can create multiple Topic measures with the same Topic and different Properties, to extract multiple fields from a Json payload.

  Example:
  ```
  Property = Power.Sensors[0].Voltage
  ```

## Bangs:

- `Publish "<topic>" [,"<payload>" [, Qos [, Retain]]]`

  Publishes the &lt;payload&gt; to the specified &lt;topic&gt;. If the &lt;payload&gt; is omitted, an empty message is published.</br>

  Optionally specify the Quality of Service level for the message. The Qos defaults to `0`.

Example:
```ini
; Create a Publish button
[Button1]
Meter=Shape
Shape=Rectangle 5,125,150,50,15,15 | Fill Color 255,128,128,255
LeftMouseUpAction=[&mqttServer:Publish(cmnd/sonoffpow_f1/Power,TOGGLE)] ; Send TOGGLE payload to topic mytopics/Power
```

~~Optionally specify the Retain flag for the message. Retain defaults to `false`.~~
  
- ~~`Subscribe "<topic>"`~~

  Subcribes to the &lt;topic&gt; on the Mqtt broker.

- ~~`Unubscribe "<topic>"`~~

  Unsubcribes from the &lt;topic&gt; on the Mqtt broker.