#include "Adafruit_ST77xx.h"
int ledIndex = 0;
int cycleColorIndex = 0;
unsigned long lastFrame = 0;
const uint32_t colors[] = {
  0x004000, 0x404000, 0x400000
};

void updateShow(MKRIoTCarrier &carrier, unsigned long now) {
  if (now - lastFrame < 160) return;
  lastFrame = now;
  carrier.leds.clear();
  carrier.leds.setPixelColor(
  ledIndex, colors[cycleColorIndex]);
  carrier.leds.show();
  ledIndex++;
  if (ledIndex >= 5) {
    ledIndex = 0;
    cycleColorIndex = (cycleColorIndex + 1) % 3;
  }
}

void showLedAll(MKRIoTCarrier &carrier, int colorIndex) {
  carrier.leds.clear();

  for (int i = 0; i < 5; i++) {
    carrier.leds.setPixelColor(
    i, colors[colorIndex]);
  }

  carrier.leds.show();
}

void printLine(MKRIoTCarrier &carrier, String text, int y) {
  text = text.substring(0, 14);
  int16_t x1, y1;
  uint16_t w, h;
  carrier.display.getTextBounds(text, 0, y, &x1, &y1, &w, &h);
  carrier.display.setCursor((240 - (int)w) / 2 - x1, y);
  carrier.display.print(text);
}

void showScreenText(MKRIoTCarrier &carrier, String title, String row1, String row2, uint32_t screenColor) {
  carrier.display.fillScreen(screenColor);
  carrier.display.setRotation(0);
  carrier.display.setTextSize(4);
  carrier.display.setTextColor(ST77XX_BLACK);
  printLine(carrier, title, 60);
  carrier.display.setTextSize(3);
  printLine(carrier, row1, 120);
  printLine(carrier, row2, 160);
}