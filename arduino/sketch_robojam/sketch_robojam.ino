#include <ServoShield.h>

ServoShield servos;

// シリアルポート通信速度
enum {
  SERIAL_BPS = 115200,
  
  PIN_A_DIRECTION = 12,
  PIN_A_BREAK = 9,
  PIN_A_PWM = 3,
  
  PIN_B_DIRECTION = 13,
  PIN_B_BREAK = 8,
  PIN_B_PWM = 11,
  
  MOTOR_SPEED = 255,

  DIRECTION_FORWARD = 1,
  DIRECTION_REVERSE = -1,

  CODE_FORWARD = 'f',
  CODE_LEFT= 'l',
  CODE_RIGHT = 'r',
  CODE_BACK = 'b',
  
  CODE_CAMERA_CENTER = '0',
  CODE_CAMERA_LEFT = '1',
  CODE_CAMERA_RIGHT = '2',
  CODE_CAMERA_UP = '3',
  CODE_CAMERA_DOWN = '4',

  CODE_CMD_START        = '$',
  CODE_CMD_SET_CAM_DIR  = 'R',
  CODE_CMD_SEP_MARK     = ',',
  CODE_CMD_END_MARK     = ';',

  DELAY_AFTER_CODE_MILLIS = 50,

  SERVOS_COUNT = 16,
  SERVO_DEG_MIN = 550,
  SERVO_DEG_MAX = 1950,
  SERVO_DEG_CENTER = 1250,
  
  SERVO_HORIZON = 0,
  SERVO_VERTICAL = 1,
  
  SERVO_DEG_UNIT = 30,
};

int cameraHorizonPos = SERVO_DEG_CENTER;
int cameraVerticalPos = SERVO_DEG_CENTER;

void setup() {
  // モーターAの制御用ピン設定
  pinMode(PIN_A_DIRECTION, OUTPUT); // 回転方向 (HIGH/LOW)
  pinMode(PIN_A_BREAK, OUTPUT); // ブレーキ (HIGH/LOW)
  pinMode(PIN_A_PWM, OUTPUT); // PWMによるスピード制御 (0-255)

  // モーターBの制御用ピン設定
  pinMode(PIN_B_DIRECTION, OUTPUT); // 回転方向 (HIGH/LOW)
  pinMode(PIN_B_BREAK, OUTPUT); // ブレーキ (HIGH/LOW)
  pinMode(PIN_B_PWM, OUTPUT); // PWMによるスピード制御 (0-255)
  
  Serial.begin(SERIAL_BPS);
  
  for (int servo = 0; servo < SERVOS_COUNT; ++servo)
  {
    servos.setbounds(servo, SERVO_DEG_MIN, SERVO_DEG_MAX);
    servos.setposition(servo, SERVO_DEG_CENTER);
  }
  
  servos.start();
}

void moterA(int dir,int power)
{
    // モーターA: フルスピード正転
    digitalWrite(PIN_A_DIRECTION, dir > 0 ? HIGH : LOW);
    digitalWrite(PIN_A_BREAK, dir == 0 ? HIGH : LOW);
    analogWrite(PIN_A_PWM, power);
}

void moterB(int dir,int power)
{
    // モーターA: フルスピード正転
    digitalWrite(PIN_B_DIRECTION, dir > 0 ? HIGH : LOW);
    digitalWrite(PIN_B_BREAK, dir == 0 ? HIGH : LOW);
    analogWrite(PIN_B_PWM, power);
}

void cameraCenter()
{
  cameraHorizonPos = SERVO_DEG_CENTER;
  cameraVerticalPos = SERVO_DEG_CENTER;
  servos.setposition(SERVO_HORIZON, cameraHorizonPos);
  servos.setposition(SERVO_VERTICAL, cameraVerticalPos);
}

void cameraHorizon(int deg)
{
  cameraHorizonPos += deg;
  if(SERVO_DEG_MIN <= cameraHorizonPos && cameraHorizonPos <= SERVO_DEG_MAX) {
    servos.setposition(SERVO_HORIZON, cameraHorizonPos);
  } else {
    cameraHorizonPos -= deg;
  }
}

void cameraVertical(int deg)
{
  cameraVerticalPos += deg;
  if(SERVO_DEG_MIN <= cameraVerticalPos && cameraVerticalPos <= SERVO_DEG_MAX) {
    servos.setposition(SERVO_VERTICAL, cameraVerticalPos);
  } else {
    cameraVerticalPos -= deg;
  }
}

void cameraDirect1000(int v1000,int h1000)
{
  if(v1000>1000)v1000=1000;
  if(h1000>1000)h1000=1000;
  if(v1000<0)v1000=0;
  if(h1000<0)h1000=0;
  servos.setposition(SERVO_VERTICAL, (SERVO_DEG_MAX - SERVO_DEG_MIN)*v1000 / 1000);
  servos.setposition(SERVO_HORIZON,  (SERVO_DEG_MAX - SERVO_DEG_MIN)*h1000 / 1000);
}

bool cmdReadNumber(int& number)
{
  number=0;
  int sign = 1;
  int pos=0;
  for(;;){
      char c = Serial.read();
      if(0==pos){
          if('-'==c){ sign = -1; pos++; continue; }
          if('+'==c){ sign = +1; pos++; continue; }
      }
      if(c>='0' &&　c<='9'){
          number *= 10;
          number += (int)(c-'9');
          pos++;
      }
      else{
          return false;
      }
      if(CODE_CMD_SEP_MARK == c||CODE_CMD_SEP_MARK == c)break;
  }
  return true;
}

void motorStop()
{
  analogWrite(PIN_A_PWM, 0);
  analogWrite(PIN_B_PWM, 0);
}

void loop(){  
  while (Serial.available())
  { 
    switch(Serial.read()) {
      case CODE_FORWARD:
        moterA(DIRECTION_FORWARD, 255);
        moterB(DIRECTION_FORWARD, 255);
        break;
      case CODE_BACK:
        moterA(DIRECTION_REVERSE, 255);
        moterB(DIRECTION_REVERSE, 255);
        break;
      case CODE_RIGHT:
        moterA(DIRECTION_FORWARD, 255);
        moterB(DIRECTION_REVERSE, 255);
        break;
      case CODE_LEFT:
        moterA(DIRECTION_REVERSE, 255);
        moterB(DIRECTION_FORWARD, 255);
        break;
      case CODE_CAMERA_CENTER:
        cameraCenter();
        break;
      case CODE_CAMERA_RIGHT:
        cameraHorizon(SERVO_DEG_UNIT);
        break;
      case CODE_CAMERA_LEFT:
        cameraHorizon(-SERVO_DEG_UNIT);
        break;
      case CODE_CAMERA_UP:
        cameraVertical(SERVO_DEG_UNIT);
        break;
      case CODE_CAMERA_DOWN:
        cameraVertical(-SERVO_DEG_UNIT);
        break;
      case CODE_CMD_START:
        switch(Serial.read())
        {
          // カメラの角度を直にセット
          case CODE_CMD_SET_CAM_DIR:
            {
              int v1000=500, h1000=500;
              if(false==cmdReadNumber(v1000)){
                break;
              }
              if(false==cmdReadNumber(h1000)){
                break;
              }
              cameraDirect1000(v1000,h1000);
            }
            break;
        }
        break;
      default:
        motorStop();
        break;
    }
    
    delay(DELAY_AFTER_CODE_MILLIS);
    motorStop();
  }  
}


