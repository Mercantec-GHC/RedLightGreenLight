float previousX = 0.00;
float previousY = 0.00;
float previousZ = 0.00;

float tolerance = 0.15f;
bool redLight = true;
bool stateSaved = false;

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

  // Checks if any movement changed beyond tolerance
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