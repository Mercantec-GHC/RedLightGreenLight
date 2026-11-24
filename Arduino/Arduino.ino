#include <Arduino.h>
#include <Arduino_MKRIoTCarrier.h>
#include <WiFiNINA.h>
#include <ArduinoMqttClient.h>
#include <ArduinoJson.h>

#include "Helpers.h"
#include "MqttConnection.h"
#include "State.h"
#include "MovementDetectionAccelerometer.h"
#include "Events.h"


MKRIoTCarrier carrier;

// MQTT objects and state: one definition each.
WiFiClient wifiClient;
MqttClient mqtt(wifiClient);
String deviceId = "";
String serverAllTopic = "";
String serverDeviceTopic = "";
String arduinoStatusTopic = "";
String arduinoDeviceTopic = "";
unsigned long lastWifiAttempt = 0;
unsigned long lastMqttAttempt = 0;
bool wifiAttempted = false;
bool mqttAttempted = false;
bool mqttWasConnected = false;

// Game state: one definition each.
bool gameHasStarted = false;
bool eliminationEnabled = false;
bool playerEliminated = false;
bool redLightActive = false;
unsigned long lastMovedSentAt = 0;
const unsigned long MOVED_INTERVAL_MS = 500;

// Accelerometer values: latest sample and Red Light baseline.
float currentX = 0.0f;
float currentY = 0.0f;
float currentZ = 0.0f;
float previousX = 0.0f;
float previousY = 0.0f;
float previousZ = 1.0f;
float tolerance = 0.15f;

void setup() {
  Serial.begin(9600);

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");
  }
  

  beginMqttConnection();
}


// if holding from opisite of 02
// forward & backwards = y
// left & right = x
// up and down = z
void loop() {

  mqttConnectionLoop();

  float x, y, z;

  // Sample the accelerometer continuously. readMotion() only acts during Red Light.
  if (carrier.IMUmodule.accelerationAvailable()) {
    float x, y, z;
    carrier.IMUmodule.readAcceleration(x, y, z);

    if (readMotion(x, y, z)) {
      Serial.println("Movement detected during Red Light!");
      // readMotion() already calls onMotionDetected(), which publishes the event.
    }
  }

  carrier.Buttons.update();

  if (carrier.Buttons.onTouchDown(TOUCH2)) {
    onReadyPressed();
  }

  // Check connection
  checkConnectionLost();

  // Connect WiFi
  if (!ensureWifi()) return;

  // Generate device ID and MQTT topics
  if (!deviceId.length()) {
    buildTopics();
  }

  // Connect MQTT
  if (!ensureMqtt()) return;

  // Process MQTT messages
  mqtt.poll();

  // prints all movements for debuggin
  // Serial.print("x ");
  // Serial.print(x);
  // Serial.print('\t');
  // Serial.print("y ");
  // Serial.print(y);
  // Serial.print('\t');
  // Serial.print("z ");
  // Serial.println(z);
}