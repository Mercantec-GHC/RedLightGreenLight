#pragma once

#include <Arduino.h>
#include <math.h>
#include "State.h"

extern float currentX;
extern float currentY;
extern float currentZ;
extern float previousX;
extern float previousY;
extern float previousZ;
extern float tolerance;

// Save the players position when RedLight starts from Events.h
inline void savePosition(float x, float y, float z) {
  // Keep the latest accelerometer sample available to the event handler.
  currentX = x;
  currentY = y;
  currentZ = z;

}

// logic for detecting movement
inline bool readMotion(float x, float y, float z){

  // prevents movement checks when not Redlight
   if (!gameHasStarted || !redLightActive || playerEliminated) {
    return false;
  }

  // Calculate differences 
  const float differenceX = abs(x - previousX);
  const float differenceY = abs(y - previousY);
  const float differenceZ = abs(z - previousZ);

  // Checks if any movement changed beyond tolerance
  if (differenceX > tolerance ||
      differenceY > tolerance ||
      differenceZ > tolerance) {
    onMotionDetected();
    // Serial.print("differenceX: ");
    // Serial.print(differenceX);
    // Serial.print("differenceY: ");
    // Serial.print(differenceY);
    // Serial.print("differenceZ: ");
    // Serial.print(differenceZ);
    // Serial.print("previousX: ");
    // Serial.print(previousX);
    // Serial.print("previousY: ");
    // Serial.print(previousY);
    // Serial.print("previousZ: ");
    // Serial.print(previousZ);
    // Serial.print("tolerance: ");
    // Serial.print(tolerance);
    // Serial.print("currentX: ");
    // Serial.print(currentX);
    // Serial.print("currentY: ");
    // Serial.print(currentY);
    // Serial.print("currentZ: ");
    // Serial.print(currentZ);
    return true;
  }
  return false;
}
