#include <Arduino.h>
#include <Arduino_MKRIoTCarrier.h>
#include <WiFiNINA.h>
#include <ArduinoMqttClient.h>
#include <ArduinoJson.h>
#include <vector>
#include <SPI.h>

#include "signalStrength.h"
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

int strenghtAverage = 200;

void setup() {
  Serial.begin(9600);

  carrier.begin();

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");
  }

  beginMqttConnection();

  // showLedAll(carrier, 1);

  // while (WiFi.begin(STRENGTH_SSID, STRENGTH_PASS) != WL_CONNECTED) {
  //   Serial.println("Connection failed. Retrying...");
  //   delay(3000);
  // }

  // showLedAll(carrier, 0);
  // Serial.println("Connected!");

  //showScreenText(carrier, "Player 1", "has won", "the game", ST77XX_RED);
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

  // averageStrength(millis(), strenghtAverage);

  //   if (strenghtAverage < 48) {
  //     showLedAll(carrier, 0);
  //   }
  //   else {
  //     showLedAll(carrier, 2);
  //   }

  // for ready up button with 02
  carrier.Buttons.update();

  if (carrier.Buttons.onTouchDown(TOUCH2) && !gameHasStarted && !playerEliminated) {
    onReadyPressed();
  }

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