WiFiClient net;
MqttClient mqtt(net);

bool ensureWifi() {
  if (WiFi.status() == WL_CONNECTED) return true;

  if (millis() - lastWifiAttempt >= 10000) {
    lastWifiAttempt = millis();

    Serial.println("Connecting to WiFi...");

    if (WIFI_PASS[0])
      WiFi.begin(WIFI_SSID, WIFI_PASS);
    else
      WiFi.begin(WIFI_SSID);
  }

  return false;
}

void buildTopics() {
  byte mac[6];
  WiFi.macAddress(mac);
  char id[13];
  snprintf(id, sizeof(id), "%02X%02X%02X%02X%02X%02X", mac[5], mac[4], mac[3], mac[2], mac[1], mac[0]);
  deviceId = id;
}

bool ensureMqtt() {
  if (mqtt.connected()) return true;

  if (millis() - lastMqttAttempt < 3000) return false;

  lastMqttAttempt = millis();

  mqtt.setId((deviceId).c_str());

  mqtt.setKeepAliveInterval(MQTT_KEEP_ALIVE_MS);

  if (MQTT_USER[0])
    mqtt.setUsernamePassword(MQTT_USER, MQTT_PASS);

  mqtt.beginWill(topicStatus.c_str(), true, 1);
  mqtt.print("{\"online\":false}");
  mqtt.endWill();

  Serial.print("Connecting to MQTT... ");

  if (!mqtt.connect(MQTT_HOST, MQTT_PORT)) {
    Serial.println(mqtt.connectError());
    return false;
  }

  Serial.println("connected");
}

void onMqttMessage(int size) {
  String body;

  while (mqtt.available())
    body += (char)mqtt.read();

  JsonDocument doc;

  if (deserializeJson(doc, body)) return;

  const char *event = doc["event"] | "";

  // Handle event...
}

void publishEvent(const char *event) {
  char ts[24] = "";

  if (baseEpoch)
    isoNow(ts, sizeof(ts));

  mqtt.beginMessage(topicInput.c_str());

  mqtt.print("{\"event\":\"");
  mqtt.print(event);

  if (ts[0]) {
    mqtt.print("\",\"timestamp\":\"");
    mqtt.print(ts);
  }

  mqtt.print("\"}");

  mqtt.endMessage();
}