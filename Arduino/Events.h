#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <string.h>
#include "State.h"
#include "MovementDetectionAccelerometer.h"

// Implemented in MqttConnection.h.
inline bool publishEvent(const char* eventName);

inline void sendEvent(const char* event) {
  if (event == nullptr || event[0] == '\0') {
    return;
  }

  publishEvent(event);
}

inline void handleEvent(const char* event){

  if (event == nullptr) {
    return;
  }

  if (strcmp(event, "GameStarted") == 0) {
    onGameStarted();
  }
  else if (strcmp(event, "GameOver") == 0) {
    onGameOver();
  }
  else if (strcmp(event, "PlayerWon") == 0) {
    onGameOver();
  }
  else if (strcmp(event, "RedLight") == 0) {
    // Use the most recent IMU sample as the Red Light starting position.
    savePosition(currentX, currentY, currentZ);
    redLightActive = true;
    eliminationEnabled = gameHasStarted && !playerEliminated;
  }
  else if (strcmp(event, "YellowLight") == 0) {
    redLightActive = false;
    eliminationEnabled = false;
  }
  else if (strcmp(event, "GreenLight") == 0) {
    onGreenLight();
  }

}

// Events which carry JSON data, such as PlayerWon and PlayerEliminated.
inline void handleEvent(const char* event, JsonVariant data) {
  if (event == nullptr) {
    return;
  }

  if (strcmp(event, "PlayerWon") == 0) {
    onPlayerWon(data);
  }
  else if (strcmp(event, "PlayerEliminated") == 0) {
    onPlayerEliminated(data);
  }
  else {
    handleEvent(event);
  }
}