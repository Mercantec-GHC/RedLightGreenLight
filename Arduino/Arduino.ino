#include <Arduino_MKRIoTCarrier.h>
#include "movementDetectionAccelerometer.h"
#include "config.h"
#include "display.h"
#include <SPI.h>
#include <WiFiNINA.h>
#include <vector>
#include "signalStrength.h"
#include <ArduinoBLE.h>

MKRIoTCarrier carrier;

int strenghtAverage = 200;

BLEDevice goal;
unsigned long lastMeasurement = 0;



void setup() {
  Serial.begin(9600);
  //while (!Serial);

  carrier.begin();

  if (!carrier.IMUmodule.begin()) {
    Serial.println("Failed to initialize IMU!");
    while (1);
  }

  showLedAll(carrier, 1);

  //while (WiFi.begin(STRENGTH_SSID, STRENGTH_PASS) != WL_CONNECTED) {
  //  Serial.println("Connection failed. Retrying...");
  //  delay(3000);
  //}

  //showLedAll(carrier, 0);
  //Serial.println("Connected!");


  if (!BLE.begin()) {
    Serial.println("BLE failed!");
    while (1);
  }

  BLE.scanForName("RedLightGoal");
  Serial.println("Searching for goal...");

  showScreenText(carrier, "Player", "is searching", "", ST77XX_WHITE);
}


// if holding from opisite of 02 
// forward & backwards = y
// left & right = x
// up and down = z
void loop() {
  BLE.poll();

  if (!goal || !goal.connected()) {
    BLEDevice device = BLE.available();

    if (device) {
      BLE.stopScan();

      Serial.println("Connecting...");
      showScreenText(carrier, "", "Connecting...", "", ST77XX_WHITE);

      if (device.connect()) {
        goal = device;
        Serial.println("Connected!");
        showScreenText(carrier, "", "Connected!", "", ST77XX_WHITE);
      } else {
        Serial.println("Connection failed");
        showScreenText(carrier, "", "Connection failed", "", ST77XX_WHITE);
        BLE.scanForName("RedLightGoal");
      }
    }

    return;
  }

  if (millis() - lastMeasurement >= 100) {
    lastMeasurement = millis();

    int rssi = goal.rssi();

    if (rssi == 127) {
      Serial.println("Invalid RSSI");
      return;
    }

    Serial.print("RSSI: ");
    Serial.println(rssi);

    if (rssi >= -48) {
      Serial.println("GOAL REACHED!");
      showScreenText(carrier, "", "GOAL REACHED!", "", ST77XX_WHITE);
      showLedAll(carrier, 0);
    }
    else {
      showLedAll(carrier, 2);
    }
  }
  
  
  float x, y, z;

  if (carrier.IMUmodule.accelerationAvailable()) {
    carrier.IMUmodule.readAcceleration(x, y, z);

    if (readMotion(x, y, z)) {
      //Serial.println("Moved during red light!");
    }
    else {
      //Serial.println("No movement");
    }
  }


  //averageStrength(millis(), strenghtAverage);

  //if (strenghtAverage < 48) {
  //  showLedAll(carrier, 0);
  //}
  //else {
  //  showLedAll(carrier, 2);
  //}
    


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