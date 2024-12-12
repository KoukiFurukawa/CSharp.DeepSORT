import pygame.mixer as pm
import time


#pmの初期化
pm.init()
#mp3ファイルの読み込み
pm.music.load("./RaspberryPi/warning.mp3")
#mp3再生
pm.music.play(-1)
time.sleep(5)
#mp3停止
pm.music.stop()
