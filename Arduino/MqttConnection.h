#pragma once

#include <Arduino.h>
#include <WiFiNINA.h>
#include <ArduinoMqttClient.h>
#include <ArduinoJson.h>

#include "Config.h"


// Network clients
WiFiClient wifiClient;
MqttClient mqtt(wifiClient);


// Device information
String deviceId = "";

String eventTopic = "";
String commandTopic = "";


// Connection state
unsigned long lastWifiAttempt = 0;
unsigned long lastMqttAttempt = 0;

bool wifiAttempted = false;
bool mqttAttempted = false;
bool mqttWasConnected = false;


// Handle incoming MQTT messages
void onMqttMessage(int messageSize) {

  String topic = mqtt.messageTopic();
  String payload = "";

  while (mqtt.available()) {
    payload += (char)mqtt.read();
  }

  Serial.println("MQTT message received!");

  Serial.print("Topic: ");
  Serial.println(topic);

  Serial.print("Message: ");
  Serial.println(payload);
}


// Initialize MQTT configuration
void beginMqttConnection() {

  mqtt.setKeepAliveInterval(MQTT_KEEP_ALIVE_MS);
  mqtt.setConnectionTimeout(5000);

  mqtt.onMessage(onMqttMessage);

  Serial.println("MQTT connection initialized");
}


// Check WiFi connection and reconnect if needed
bool ensureWifi() {

  if (WiFi.status() == WL_CONNECTED) {
    return true;
  }

  unsigned long currentTime = millis();

  if (
    !wifiAttempted ||
    currentTime - lastWifiAttempt >= WIFI_RECONNECT_INTERVAL_MS
  ) {

    Serial.println("Connecting to WiFi...");

    WiFi.begin(WIFI_SSID, WIFI_PASS);

    lastWifiAttempt = currentTime;
    wifiAttempted = true;
  }

  return false;
}


// Generate a unique device ID and MQTT topics
void buildTopics() {

  byte mac[6];

  WiFi.macAddress(mac);

  char macAddress[13];

  snprintf(
    macAddress,
    sizeof(macAddress),
    "%02X%02X%02X%02X%02X%02X",
    mac[0],
    mac[1],
    mac[2],
    mac[3],
    mac[4],
    mac[5]
  );

  deviceId = "mkr-" + String(macAddress);

  eventTopic = String(MQTT_TOPIC_PREFIX) + "/" +
               deviceId + "/events";

  commandTopic = String(MQTT_TOPIC_PREFIX) + "/" +
                 deviceId + "/commands";

  // Set MQTT client ID
  mqtt.setId(deviceId);

  Serial.println("Device ID generated:");
  Serial.println(deviceId);

  Serial.println("Event topic:");
  Serial.println(eventTopic);

  Serial.println("Command topic:");
  Serial.println(commandTopic);
}


// Check if MQTT connection was lost
void checkConnectionLost() {

  bool connected = mqtt.connected();

  if (mqttWasConnected && !connected) {

    Serial.println("MQTT connection lost!");
  }

  mqttWasConnected = connected;
}


// Connect to MQTT broker
bool ensureMqtt() {

  if (mqtt.connected()) {
    return true;
  }

  unsigned long currentTime = millis();

  if (
    mqttAttempted &&
    currentTime - lastMqttAttempt < MQTT_RECONNECT_INTERVAL_MS
  ) {
    return false;
  }

  Serial.println("Connecting to MQTT broker...");

  mqttAttempted = true;
  lastMqttAttempt = currentTime;

  // Configure authentication if provided
  if (MQTT_USER[0] != '\0') {

    mqtt.setUsernamePassword(
      MQTT_USER,
      MQTT_PASS
    );
  }

  if (mqtt.connect(MQTT_HOST, MQTT_PORT)) {

    Serial.println("MQTT connected!");

    // Subscribe to commands from backend
    mqtt.subscribe(commandTopic.c_str());

    Serial.println("Subscribed to command topic");

    return true;
  }

  Serial.print("MQTT connection failed: ");
  Serial.println(mqtt.connectError());

  return false;
}


// Publish an event to backend
bool publishEvent(const char* eventName) {

  if (!mqtt.connected()) {
    Serial.println("Cannot publish: MQTT disconnected");
    return false;
  }

  // Create JSON event
  StaticJsonDocument<192> document;

  document["event"] = eventName;
  document["deviceId"] = deviceId;
  document["player"] = PLAYER_NAME;

  char payload[192];

  size_t payloadSize = serializeJson(
    document,
    payload,
    sizeof(payload)
  );

  if (payloadSize == 0) {
    return false;
  }

  // Publish message
  mqtt.beginMessage(eventTopic.c_str());

  mqtt.write(
    (const uint8_t*)payload,
    payloadSize
  );

  bool success = mqtt.endMessage();

  if (success) {

    Serial.print("Published event: ");
    Serial.println(eventName);
  }

  return success;
}


// Main MQTT connection handler
void mqttConnectionLoop() {

  // Check for lost MQTT connection
  checkConnectionLost();

  // Connect WiFi
  if (!ensureWifi()) {
    return;
  }

  // Generate device ID and topics
  if (!deviceId.length()) {
    buildTopics();
  }

  // Connect MQTT
  if (!ensureMqtt()) {
    return;
  }

  // Process MQTT messages and keep connection alive
  mqtt.poll();
}