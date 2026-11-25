#pragma once

#include <Arduino.h>
#include <WiFiNINA.h>
#include <ArduinoMqttClient.h>
#include <ArduinoJson.h>
#include <time.h>
#include "Helpers.h"
#include "Config.h"

// The game event handler is defined in Events.h.
inline void handleEvent(const char* event, JsonVariant data);

extern WiFiClient wifiClient;
extern MqttClient mqtt;
extern String deviceId;
extern String serverAllTopic;
extern String serverDeviceTopic;
extern String arduinoStatusTopic;
extern String arduinoDeviceTopic;
extern unsigned long lastWifiAttempt;
extern unsigned long lastMqttAttempt;
extern bool wifiAttempted;
extern bool mqttAttempted;
extern bool mqttWasConnected;

// Handle arduino MQTT messages
void onMqttMessage(int messageSize) {

  String topic = mqtt.messageTopic();
  String payload;
  if (messageSize > 0) {
    payload.reserve((unsigned int)messageSize);
  }

  while (mqtt.available()) {
    payload += (char)mqtt.read();
  }

  Serial.println("MQTT message received!");

  Serial.print("Topic: ");
  Serial.println(topic);

  Serial.print("Message: ");
  Serial.println(payload);

  StaticJsonDocument<512> document;
  DeserializationError error = deserializeJson(document, payload);
  if (error) {
    Serial.print("Invalid MQTT JSON: ");
    Serial.println(error.c_str());
    return;
  }

  const char* eventName = document["event"] | "";
  if (eventName[0] == '\0') {
    Serial.println("MQTT message has no event name");
    return;
  }

  JsonVariant data = document["data"].as<JsonVariant>();
  handleEvent(eventName, data);
}


// Initialize MQTT configuration
inline void beginMqttConnection() {

  mqtt.setKeepAliveInterval(MQTT_KEEP_ALIVE_MS);
  mqtt.setConnectionTimeout(5000);

  mqtt.onMessage(onMqttMessage);

  Serial.println("MQTT connection initialized");
}


// Check WiFi connection and reconnect if needed
inline bool ensureWifi() {

  if (WiFi.status() == WL_CONNECTED) {
    return true;
  }

  const unsigned long currentTime = millis();

  if (
    !wifiAttempted || currentTime - lastWifiAttempt >= WIFI_RECONNECT_INTERVAL_MS) {

    Serial.println("Connecting to WiFi...");

    WiFi.begin(WIFI_SSID, WIFI_PASS);

    lastWifiAttempt = currentTime;
    wifiAttempted = true;
  }

  return false;
}


// Generate a unique device ID and MQTT topics
inline void buildTopics() {

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
    mac[5]);

  deviceId = "mkr-" + String(macAddress);

  serverAllTopic = "server/all/events";
  serverDeviceTopic = "server/" + deviceId + "/events";
  arduinoStatusTopic = "arduino/" + deviceId + "/status";
  arduinoDeviceTopic = "arduino/" + deviceId + "/events";

  // Set MQTT client ID
  mqtt.setId(deviceId);

  Serial.println("Device ID generated:");
  Serial.println(deviceId);

  Serial.println("All server Event topic:");
  Serial.println(serverAllTopic);

  Serial.println("Device server Event topic:");
  Serial.println(serverDeviceTopic);

  Serial.println("Status arduino topic:");
  Serial.println(arduinoStatusTopic);

  Serial.println("Status arduino event topic:");
  Serial.println(arduinoDeviceTopic);
}


// Check if MQTT connection was lost
inline void checkConnectionLost() {

  bool connected = mqtt.connected();

  if (mqttWasConnected && !connected) {

    Serial.println("MQTT connection lost!");
  }

  mqttWasConnected = connected;
}


// Connect to MQTT broker
inline bool ensureMqtt() {

  if (mqtt.connected()) {
    return true;
  }

  const unsigned long currentTime = millis();

  if (mqttAttempted && currentTime - lastMqttAttempt < MQTT_RECONNECT_INTERVAL_MS) {
    return false;
  }

  Serial.println("Connecting to MQTT broker...");

  mqttAttempted = true;
  lastMqttAttempt = currentTime;

  mqtt.setKeepAliveInterval(5 * 1000);  // ms, how often the broker expects a sign of life

  mqtt.beginWill(arduinoStatusTopic, true, 1);  // topic, retain, QoS
  mqtt.print("offline");
  mqtt.endWill();

  if (mqtt.connect(MQTT_HOST, MQTT_PORT)) {

    Serial.println("MQTT connected!");

    // Subscribe to commands from backend
    mqtt.subscribe(serverAllTopic.c_str());
    mqtt.subscribe(serverDeviceTopic.c_str());
    Serial.println("Subscribed to event topics");

    mqtt.beginMessage(arduinoStatusTopic, true, 1);
    mqtt.print("online");
    mqtt.endMessage();

    return true;
  }

  Serial.print("MQTT connection failed: ");
  Serial.println(mqtt.connectError());

  return false;
}


// Publish an event to backend
inline bool publishEvent(const char* eventName) {

  if (!mqtt.connected()) {
    Serial.println("Cannot publish: MQTT disconnected");
    return false;
  }

  // Create JSON event
  StaticJsonDocument<192> document;

  document["event"] = eventName;
  document["date"] = getUTCTimestamp();

  char payload[192];

  size_t payloadSize = serializeJson(
    document,
    payload,
    sizeof(payload));

  if (payloadSize == 0) {
    return false;
  }

  // Publish message
  mqtt.beginMessage(arduinoDeviceTopic.c_str());

  mqtt.write(
    (const uint8_t*)payload,
    payloadSize);

  bool success = mqtt.endMessage();

  if (success) {

    Serial.print("Published event: ");
    Serial.println(eventName);
  }

  return success;
}



// Main MQTT connection handler
inline void mqttConnectionLoop() {

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