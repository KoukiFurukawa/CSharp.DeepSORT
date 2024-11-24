from ultralytics import YOLO
import onnx
import onnxruntime
import numpy as np
from onnxruntime.datasets import get_example
import cv2
from imread_from_url import imread_from_url

from YOLOv11 import YOLOv11

# モデルとクラス情報のパス
MODEL_PATH = "Py.Yolo/yolo11n.onnx"
IMAGE_PATH = "Py.Yolo/zidane.jpg"

if __name__ == '__main__':

    yolov11_detector = YOLOv11(MODEL_PATH, conf_thres=0.2, iou_thres=0.3)
    # Initialize YOLOv8 object detector
    img_url = "https://live.staticflickr.com/13/19041780_d6fd803de0_3k.jpg"
    img = imread_from_url(img_url)
    img2 = cv2.imread(IMAGE_PATH)

    # boxes, scores, class_ids = yolov8_detector(img)
    boxes, scores, class_ids = yolov11_detector(img2)
    
    print("Boxes:", boxes, "Scores:", scores, "Class IDs:", class_ids)
    
    # Draw detections
    combined_img = yolov11_detector.draw_detections(img2)
    cv2.imshow("Output", combined_img)
    cv2.waitKey(0)