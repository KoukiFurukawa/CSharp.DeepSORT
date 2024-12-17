from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn
import time
import threading
import keyboard

import alert

#警戒レベル
level = 0

app = FastAPI()

# リクエストbodyを定義
class Approach(BaseModel):
    level: int

#警戒レベルの受信
@app.post("/")
def create_level(approach: Approach):
    global level
    level = approach.level
    return{'state':'ok'}

#警戒レベルに応じてmp3を再生
def ring_alert():
    global level
    state = 0
    while not keyboard.is_pressed('q'):
        #危険レベル0
        if level == 0 and state != 0:
            alert.mp3_stop()
            state = 0
        #危険レベル1
        elif level == 1 and state != 1:
            if state == 0:
                alert.caution()
            elif state == 2:
                alert.mp3_stop()
                alert.caution()
            state = 1
        #危険レベル2
        elif level == 2 and state != 2:
            if state == 0:
                alert.warning()
            elif state == 1:
                alert.mp3_stop()
                alert.warning()
            state = 2

        time.sleep(0.01)

#ring_alertを実行するスレッド
thread1=threading.Thread(target=ring_alert)
print("Long press q to quit thread1")
thread1.start()

if __name__ == '__main__':
    uvicorn.run(app, host="0.0.0.0", port=8000)
    alert.pm.init()