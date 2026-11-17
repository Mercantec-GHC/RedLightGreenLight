/*
  Arduino LSM6DS3 - Simple Accelerometer

  This example reads the acceleration values from the LSM6DS3
  sensor and continuously prints them to the Serial Monitor
  or Serial Plotter.

  The circuit:
  - Arduino Uno WiFi Rev 2 or Arduino Nano 33 IoT

  created 10 Jul 2019
  by Riccardo Rizzo

  This example code is in the public domain.
*/

#include <Arduino_MKRIoTCarrier.h>
MKRIoTCarrier carrier;

void setup() {
  Serial.begin(9600);
  while (!Serial);

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");

    while (1);
  }
}
// logic for detecting movement
bool readMotion(float x, float y, float z){

  if (x > 0.15f || x < -0.15f) {
    return true;
  }

  if (y > 0.15f || y < -0.15f) {
    return true;
  }

  if (z > 1.15f || z < -1.15f) {
    return true;
  }

  return false;
}
//if holding from opisite of 02 
// forward and backwards = y
// sides = x
// up and down = z
void loop() {
  float x, y, z;

  if (carrier.IMUmodule.accelerationAvailable()) {
    carrier.IMUmodule.readAcceleration(x, y, z);

    if (readMotion(x, y, z)) {
      Serial.println("Moved during red light!");
    }
    else {
      Serial.println("No movement");
    }

    // Serial.print("x ");
    // Serial.print(x);
    // Serial.print('\t');
    // Serial.print("y ");
    // Serial.print(y);
    // Serial.print('\t');
    // Serial.print("z ");
    // Serial.println(z);
  }
}