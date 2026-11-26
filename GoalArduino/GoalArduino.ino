#include <Arduino_MKRIoTCarrier.h>
#include "config.h"
#include "display.h"
#include <SPI.h>
#include <WiFiNINA.h>
#include <ArduinoBLE.h>

MKRIoTCarrier carrier;

char ssid[] = "Arduino_Link";
char pass[] = "SignalTest123";

unsigned long lastCheck = 0;
bool advertising = false;


void setup() {
  Serial.begin(9600);

  if (!carrier.begin()) {
    Serial.println("Failed to initialize Carrier");
    while (1);
  }

  showLedAll(carrier, 1);
  //showScreenText(carrier, "Player 1", "has won", "the game", ST77XX_RED);
  

  //Serial.println("Starting WiFi Access Point...");

  //int status = WiFi.beginAP(ssid, pass);

  //if (status != WL_AP_LISTENING) {
  //  Serial.println("Failed to start Access Point!");
  //  showLedAll(carrier, 2);
  //  while (true);
  //}

  //Serial.println("Access Point started!");
  //Serial.print("Network name: ");
  //Serial.println(ssid);
  //Serial.print("IP address: ");
  //Serial.println(WiFi.localIP());
  //showLedAll(carrier, 0);

  if (!BLE.begin()) {
    Serial.println("BLE failed!");
    while (1);
  }

  // Set the Bluetooth device name
  BLE.setLocalName("RedLightGoal");

  // Set the advertised device name
  BLE.setDeviceName("RedLightGoal");
  BLE.setConnectable(true);
  //BLE.setAdvertisingInterval(160);


  if (BLE.advertise()) {
    Serial.println("Goal ready");
    showLedAll(carrier, 0);
    showScreenText(carrier, "Goal", "has started", "", ST77XX_WHITE);
  } else {
    Serial.println("Advertising failed");
    showLedAll(carrier, 2);
    showScreenText(carrier, "Goal", "failed to", "start", ST77XX_WHITE);
  }
}



void loop() {
  BLE.poll();
}