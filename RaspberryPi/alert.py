import pygame.mixer as pm

def warning():
    #pmの初期化
    pm.init()
    #mp3ファイルの読み込み
    pm.music.load("warning.mp3")
    #mp3再生
    pm.music.play(-1)

def caution():
    #pmの初期化
    pm.init()
    #mp3ファイルの読み込み
    pm.music.load("caution.mp3")
    #mp3再生
    pm.music.play(-1)

def mp3_stop():
    pm.music.stop()
