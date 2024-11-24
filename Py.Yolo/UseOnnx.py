from ultralytics import YOLO
import onnx
import onnxruntime
import numpy as np
from onnxruntime.datasets import get_example
import cv2
from imread_from_url import imread_from_url
import torch
import torchvision.transforms as transforms
from torchvision.models import squeezenet1_1
from torchvision.models.feature_extraction import create_feature_extractor
from PIL import Image


from YOLOv11 import YOLOv11

# モデルとクラス情報のパス
MODEL_PATH = "Py.Yolo/yolo11n.onnx"
IMAGE_PATH = "Py.Yolo/zidane.jpg"

# Function to crop image based on bounding box
def crop_image(img, box):
    x1, y1, x2, y2 = map(int, box)
    return img[y1:y2, x1:x2]  # Crop the region of interest (ROI)

if __name__ == '__main__':

    yolov11_detector = YOLOv11(MODEL_PATH, conf_thres=0.2, iou_thres=0.3)
    model = squeezenet1_1(pretrained=True)
    
    feature_extractor = create_feature_extractor(model, {"classifier.1": "final_output"})
    
    # Initialize YOLOv8 object detector
    img_url = "https://live.staticflickr.com/13/19041780_d6fd803de0_3k.jpg"
    img = imread_from_url(img_url)
    img2 = cv2.imread(IMAGE_PATH)

    # boxes, scores, class_ids = yolov8_detector(img)
    boxes, scores, class_ids = yolov11_detector(img2)
    
    print("Boxes:", boxes, "Scores:", scores, "Class IDs:", class_ids)
    
    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
    ])
    
    for i, box in enumerate(boxes):
        # Crop the image based on the bounding box
        cropped_img = crop_image(img2, box)
        cv2.imshow(f"Cropped Image {i}", cropped_img)
        
        # Convert numpy.ndarray to PIL.Image
        img_pil = Image.fromarray(img2)  # Convert to PIL Image
        img_tensor = transform(img_pil).unsqueeze(0)  # Apply transform and add batch dimension
        features = feature_extractor(img_tensor)["final_output"]
        
        print(features.detach().cpu().numpy())
    
    # Draw detections
    combined_img = yolov11_detector.draw_detections(img2)
    cv2.imshow("Output", combined_img)
    cv2.waitKey(0)