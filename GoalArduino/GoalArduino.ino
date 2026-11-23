#include <Arduino_MKRIoTCarrier.h>
#include "config.h"
#include "display.h"
#include <SPI.h>
#include <WiFiNINA.h>

MKRIoTCarrier carrier;

char ssid[] = "Arduino_Link";
char pass[] = "SignalTest123";

void setup() {
  Serial.begin(9600);

  if (!carrier.begin()) {
    Serial.println("Failed to initialize Carrier");
    while (1);
  }

  showLedAll(carrier, 1);
  //showScreenText(carrier, "Player 1", "has won", "the game", ST77XX_RED);
  

  Serial.println("Starting WiFi Access Point...");

  int status = WiFi.beginAP(ssid, pass);

  if (status != WL_AP_LISTENING) {
    Serial.println("Failed to start Access Point!");
    showLedAll(carrier, 2);
    while (true);
  }

  Serial.println("Access Point started!");
  Serial.print("Network name: ");
  Serial.println(ssid);
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
  showLedAll(carrier, 0);
}



void loop() {
}