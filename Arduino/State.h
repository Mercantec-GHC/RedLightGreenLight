#include "Adafruit_ST77xx.h"
#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <string.h>

#include "display.h"

extern bool gameHasStarted;
extern bool eliminationEnabled;
extern bool playerEliminated;
extern bool redLightActive;


// PlayerMoved is throttled so the broker isn't flooded while the player keeps moving
extern unsigned long lastMovedSentAt;
extern const unsigned long MOVED_INTERVAL_MS;
extern String deviceId;

inline void sendEvent(const char* event);

inline void eliminate() {
  playerEliminated = true;
  eliminationEnabled = false;
  redLightActive = false;
  showScreenText("you moved", "during red", "gameover", ST77XX_WHITE);
}

inline void onGameStarted() {
  gameHasStarted = true;
  eliminationEnabled = false;
  playerEliminated = false;
  redLightActive = false;
  showScreenText("game", "has started", "", ST77XX_WHITE);
}

inline void onGameOver() {
  gameHasStarted = false;
  eliminationEnabled = false;
  redLightActive = false;
  showScreenText("game", "over", "", ST77XX_WHITE);
}

inline void onPlayerWon(JsonVariant data) {
  gameHasStarted = false;
  eliminationEnabled = false;
  redLightActive = false;


  // The server sends the winning Player object, but a plain name is accepted too
  const char *winnerName = data.is<JsonObject>() ? (data["name"] | "Unknown") : (data | "Unknown");
  const char *winnerId = data.is<JsonObject>() ? (data["id"] | "") : "";

  bool isMe = deviceId == winnerId;

  if (isMe) {
    Serial.println("You won the game!");
    showScreenText("You", "won", "", ST77XX_WHITE);
  } else {
    Serial.print("Game winner: ");
    Serial.println(winnerName);
    showScreenText(winnerName, "has won", "", ST77XX_WHITE);
  }
}

inline void onGreenLight() {
  if (!playerEliminated){
  eliminationEnabled = false;
  redLightActive = false;
  showLedAll(0); // green
  showScreenText("go", "", "", ST77XX_WHITE);
  }
}

inline void onYellowLight() {
  if (!playerEliminated){
  redLightActive = false;
  eliminationEnabled = false;
  showLedAll(1); // yellow
  showScreenText("", "get ready", "", ST77XX_WHITE);
  }
}

inline void onRedLight(){
  if (!playerEliminated){
  redLightActive = true;
  eliminationEnabled = gameHasStarted && !playerEliminated;
  showLedAll(2); // red
  showScreenText("", "", "STOP", ST77XX_WHITE);
  }
}

inline void onPlayerEliminated(JsonVariant data) {
  // Events on this device's own topic have no data, broadcasts contain the eliminated device id
  const char *eliminatedId = data | "";

  if (strlen(eliminatedId) > 0 && deviceId != eliminatedId) {
    return;
  }

  if (playerEliminated) {
    return;
  }

  eliminate();
}

// Call when the 02 button is pressed
inline void onReadyPressed() {
  if (!gameHasStarted || !playerEliminated) {
  sendEvent("PlayerReady");
  showScreenText("you", "are", "ready", ST77XX_WHITE);
  } 
  
}


// Call whenever readMotion() detects movement
inline void onMotionDetected() {
  if (!gameHasStarted || playerEliminated) {
    return;
  }
  const unsigned long now = millis();

  if (now - lastMovedSentAt >= MOVED_INTERVAL_MS) {
    lastMovedSentAt = now;

    sendEvent("PlayerMoved");
  }

  if (!eliminationEnabled || playerEliminated) {
    return;
  }

  sendEvent("PlayerEliminated");
  eliminate();
}


