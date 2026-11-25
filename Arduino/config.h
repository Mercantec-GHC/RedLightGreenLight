#pragma once
#include <stdint.h>

// WiFi
const char WIFI_SSID[] = "MAGS-OLC";
const char WIFI_PASS[] = "Merc1234!";

// Game configuration
const bool USE_CASE = false;

// MQTT configuration
const char MQTT_HOST[] = "192.168.1.183";
const uint16_t MQTT_PORT = 1883;
const uint32_t MQTT_KEEP_ALIVE_MS = 3000;

// Connection intervals
const uint32_t WIFI_RECONNECT_INTERVAL_MS = 10000;
const uint32_t MQTT_RECONNECT_INTERVAL_MS = 5000;

const char STRENGTH_SSID[] = "Arduino_Link";
const char STRENGTH_PASS[] = "SignalTest123";