#include <Arduino_MKRIoTCarrier.h>
MKRIoTCarrier carrier;

float previousX = 0.00;
float previousY = 0.00;
float previousZ = 0.00;

float tolerance = 0.15f;
bool redLight = true;
bool stateSaved = false;

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

  // Saves the initial state ONCE
  if (!stateSaved) {
    previousX = x;
    previousY = y;
    previousZ = z;

    stateSaved = true;
    return false;
  }


  // Calculate differences 
  float differenceX = abs(x - previousX);
  float differenceY = abs(y - previousY);
  float differenceZ = abs(z - previousZ);

  // Checks if any axis changed beyond tolerance
  if (differenceX > tolerance ||
      differenceY > tolerance ||
      differenceZ > tolerance) {

    return true;
  }
  return false;
}

void startRedLight() {
  stateSaved = false;
  redLight = true;
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