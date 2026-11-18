#include <Arduino_MKRIoTCarrier.h>
#include "movementDetectionAccelerometer.h"
#include "config.h"

MKRIoTCarrier carrier;


void setup() {
  Serial.begin(9600);
  while (!Serial);

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");
    while (1);
  }
}


// if holding from opisite of 02 
// forward & backwards = y
// left & right = x
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

    // prints all movements for debuggin
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