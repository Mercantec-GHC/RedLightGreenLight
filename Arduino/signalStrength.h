std::vector<int> signalStrengths;
unsigned long lastCall = 0;

void averageStrength(unsigned long now, int &strenghtAverage) {
  if (now - lastCall <= 100) return;
  lastCall = now;

  long strength = WiFi.RSSI() * -1;
  signalStrengths.push_back(strength);
  
  Serial.print(".");

  if (signalStrengths.size() == 10) {
    int sum = 0;
    for (int signalStrength : signalStrengths) {
      sum += signalStrength;
      
    }
    Serial.println();

    strenghtAverage = sum / signalStrengths.size();
    signalStrengths.clear();

    Serial.print("Average strength: ");
    Serial.println(strenghtAverage);
  }
}