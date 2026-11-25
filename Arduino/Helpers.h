#pragma once

#include <Arduino.h>
#include <WiFiNINA.h>
#include <time.h>

inline String getUTCTimestamp() {
  const time_t now = WiFi.getTime();
  if (now == 0) {
    return "";
  }

  struct tm* utc = gmtime(&now);
  if (utc == nullptr) {
    return "";
  }

  char buffer[21];
  strftime(buffer, sizeof(buffer), "%Y-%m-%dT%H:%M:%SZ", utc);
  return String(buffer);
}