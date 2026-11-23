#include <Arduino_MKRIoTCarrier.h>
#include "movementDetectionAccelerometer.h"
#include "config.h"
#include "display.h"
#include <SPI.h>
#include <WiFiNINA.h>
#include <vector>
#include "signalStrength.h"

MKRIoTCarrier carrier;

int strenghtAverage = 200;


void setup() {
  Serial.begin(9600);
  //while (!Serial);

  carrier.begin();

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");
    while (1);
  }

  showLedAll(carrier, 1);

  while (WiFi.begin(STRENGTH_SSID, STRENGTH_PASS) != WL_CONNECTED) {
    Serial.println("Connection failed. Retrying...");
    delay(3000);
  }

  showLedAll(carrier, 0);
  Serial.println("Connected!");

  //showScreenText(carrier, "Player 1", "has won", "the game", ST77XX_RED);
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
      //Serial.println("Moved during red light!");
    }
    else {
      //Serial.println("No movement");
    }


    averageStrength(millis(), strenghtAverage);

    if (strenghtAverage < 48) {
      showLedAll(carrier, 0);
    }
    else {
      showLedAll(carrier, 2);
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