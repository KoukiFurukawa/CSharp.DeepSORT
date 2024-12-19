from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn
import time
import threading
import keyboard

import alert

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
    return{'state':'ok'}

#警戒レベルに応じてmp3を再生
def ring_alert():
    global level
    prev_level = None
    while not keyboard.is_pressed('q'):
        time.sleep(0.01)

        if level == prev_level:
            continue
        elif prev_level != None:
            alert.mp3_stop()

        #危険レベル1
        if level == 1:
            alert.caution()
            
        #危険レベル2
        elif level == 2:    
            alert.warning()

        prev_level = level

#ring_alertを実行するスレッド
thread1=threading.Thread(target=ring_alert)
print("Long press q to quit thread1")
thread1.start()

if __name__ == '__main__':
    uvicorn.run(app, host="0.0.0.0", port=8000)
    alert.pm.init()