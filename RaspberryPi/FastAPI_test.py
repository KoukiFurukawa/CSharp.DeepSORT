from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn
import RPi.GPIO as GPIO
import time
import threading
import keyboard

import alert

#ポート番号の定義
Led_yellow = 18
Led_red = 19

#GPIOの設定
GPIO.setmode(GPIO.BCM)              
GPIO.setup(Led_yellow, GPIO.OUT)
GPIO.setup(Led_red, GPIO.OUT) 

#警戒レベル
level = None

app = FastAPI()

# リクエストbodyを定義
class Approach(BaseModel):
    level: int

#警戒レベルの受信
@app.post("/")
def create_level(approach: Approach):
    global level
    level = approach.level
    print(level)
    return{'state':'ok','level':level}

#警戒レベルに応じてmp3を再生
def ring_alert():
    global Led_yellow ,Led_red, level
    prev_level = None
    try:
        while not keyboard.is_pressed('q'):
            time.sleep(0.01)

            if level == prev_level:
                continue
            elif prev_level != None:
                alert.mp3_stop()
                GPIO.output(Led_yellow, GPIO.LOW)
                GPIO.output(Led_red, GPIO.LOW)      

            #危険レベル1
            if level == 1:
                alert.caution()
                GPIO.output(Led_yellow, GPIO.HIGH)

            #危険レベル2
            elif level == 2:    
                alert.warning()
                GPIO.output(Led_red, GPIO.HIGH)

            prev_level = level
    
    #終了時処理
    finally:
        GPIO.output(Led_yellow, GPIO.LOW)
        GPIO.output(Led_red, GPIO.LOW)
        GPIO.cleanup()
        print('shut down')

#ring_alertを実行するスレッド
thread1=threading.Thread(target=ring_alert)
print("Long press q to quit thread1")
thread1.start()

if __name__ == '__main__':
    uvicorn.run(app, host="0.0.0.0", port=8000)
    alert.pm.init()