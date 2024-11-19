from ultralytics import YOLO

# Load a model
model = YOLO("./yolo11n.pt")

# Evaluate model performance on the validation set
# metrics = model.val()

# Perform object detection on an image
# results = model("ultralytics/assets/zidane.jpg")
# results[0].show()

# Export the model to ONNX format
path = model.export(format="onnx")  # return path to exported model