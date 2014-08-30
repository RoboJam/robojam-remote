#include <Wire.h>
#include <ServoShield.h>

ServoShield servos; //Create a ServoShield object

const int PWM_MIN = 500;
const int PWM_MAX = 2000;
const int PWM_CENTER = 1250;
void setup()
{
  for (int servo = 0; servo < 16; servo++)//Initialize all 16 servos
  {
    servos.setbounds(servo, PWM_MIN, PWM_MAX); //Set the minimum and maximum pulse duration
    servos.setposition(servo, PWM_CENTER); //Set the initial position of the servo
  }
  servos.start(); //Start the servo shield
}

void loop()
{
  for(int pos = PWM_MIN; pos < PWM_MAX; pos++) //Move the servos from 0 degrees to 180 degrees
  { //in steps of 1 degree
    for (int i = 0; i < 16; i++) //for all 16 servos
      servos.setposition(i, pos); //Tell servo to go to position in variable 'pos'
    delay(1);
  }
  for(int pos = PWM_MAX; pos >= PWM_MIN; pos--)//Move the servos from 180 degrees to 0 degrees
  {
    for (int i = 0; i < 16; i++) //all 16 servos
      servos.setposition(i, pos); //Tell servo to go to position in variable 'pos'
    delay(1);
  }
}
