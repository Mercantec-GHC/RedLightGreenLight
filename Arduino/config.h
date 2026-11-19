#pragma once

#include <stdint.h>

// WiFi
const char WIFI_SSID[] = "MAGS-OLC";
const char WIFI_PASS[] = "Merc1234!";

// Game configuration
const bool USE_CASE = false;
const char PLAYER_NAME[] = "Arduino N";

// Server configuration (HTTPS)
const char SERVER_HOST[] = "YOUR_SERVER_HOST";
const uint16_t SERVER_PORT = 443;

// MQTT configuration
const char MQTT_HOST[] = "YOUR_MQTT_BROKER";
const uint16_t MQTT_PORT = 1883;
const char MQTT_USER[] = "";
const char MQTT_PASS[] = "";

const uint32_t MQTT_KEEP_ALIVE_MS = 3000;

// Connection intervals
const uint32_t WIFI_RECONNECT_INTERVAL_MS = 10000;
const uint32_t MQTT_RECONNECT_INTERVAL_MS = 5000;

// MQTT topics
const char MQTT_TOPIC_PREFIX[] = "devices";