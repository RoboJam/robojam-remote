#include <Wire.h>

void setup() {
  // モーターAの制御用ピン設定
  pinMode(12, OUTPUT); // 回転方向 (HIGH/LOW)
  pinMode(9, OUTPUT); // ブレーキ (HIGH/LOW)
  pinMode(3, OUTPUT); // PWMによるスピード制御 (0-255)

  // モーターBの制御用ピン設定
  pinMode(13, OUTPUT); // 回転方向 (HIGH/LOW)
  pinMode(8, OUTPUT); // ブレーキ (HIGH/LOW)
  pinMode(11, OUTPUT); // PWMによるスピード制御 (0-255)
  
  Wire.begin(); // join i2c bus (address optional for master)
  Serial.begin(115200);
}

void moterA(int dir,int power)
{
    // モーターA: フルスピード正転
    digitalWrite(12, dir > 0 ? HIGH : LOW);
    digitalWrite(9, dir == 0 ? HIGH : LOW);
    analogWrite(3, power);
}
void moterB(int dir,int power)
{
    // モーターA: フルスピード正転
    digitalWrite(13, dir > 0 ? HIGH : LOW);
    digitalWrite(8, dir == 0 ? HIGH : LOW);
    analogWrite(11, power);
}

int xxx=0;
void loop(){  
  while (Serial.available())
  { 
    char c = Serial.read();
    if(c=='f')
    {
      moterA(1,255);
      moterB(1,255);
    }
    if(c=='b')
    {
      moterA(-1,255);
      moterB(-1,255);
    }
    if(c=='r')
    {
      moterA(1,255);
      moterB(-1,255);
    }
    if(c=='l')
    {
      moterA(-1,255);
      moterB(1,255);
    }
    if(c=='1'){
      moterA(1,255);
    }
    if(c=='2'){
      moterB(1,255);
    }
    if(c=='3'){
      moterA(-1,255);
    }
    if(c=='4'){
      moterB(-1,255);
    }
    delay(50);
    // stop
    analogWrite(3, 0);
    analogWrite(11, 0);
  }
  

#if 0
  // モーターA: フルスピード正転
  digitalWrite(12, HIGH);
  digitalWrite(9, LOW);
  analogWrite(3, 255);
  // モーターB: フルスピード正転
  digitalWrite(13, HIGH);
  digitalWrite(8, LOW);
  analogWrite(11, 255);
  // 2秒間上記設定で回転
  delay(500);

  // モーターA: フルスピード逆転
  digitalWrite(12, LOW);
  digitalWrite(9, LOW);
  analogWrite(3, 255);
  // モーターB: フルスピード逆転
  digitalWrite(13, LOW);
  digitalWrite(8, LOW);
  analogWrite(11, 255);
  // 2秒間上記設定で回転
  delay(500);
  
  xxx++;
  if(xxx%5==0)
  {
    // モーターA: フルスピード逆転
    digitalWrite(12, HIGH);
    digitalWrite(9, LOW);
    analogWrite(3, 255);
    // モーターB: フルスピード逆転
    digitalWrite(13, HIGH);
    digitalWrite(8, LOW);
    analogWrite(11, 255);
    // 2秒間上記設定で回転
    delay(2000);
  }
#endif
  
}


