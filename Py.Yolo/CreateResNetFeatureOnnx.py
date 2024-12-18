import torch
import torchvision.models as models

# ResNet-18モデルのロード
resnet18 = models.resnet18(pretrained=True)

# 特定の層（conv5）を出力するカスタムモデル
class Conv5Extractor(torch.nn.Module):
    def __init__(self, original_model):
        super(Conv5Extractor, self).__init__()
        self.features = torch.nn.Sequential(
            original_model.conv1,
            original_model.bn1,
            original_model.relu,
            original_model.maxpool,
            original_model.layer1,
            original_model.layer2,
            original_model.layer3,
            original_model.layer4[0]  # conv5に該当するブロック
        )

    def forward(self, x):
        x = self.features(x)
        return x  # conv5の出力

# カスタムモデルを作成
conv5_extractor = Conv5Extractor(resnet18)
conv5_extractor.eval()

# ダミー入力
dummy_input = torch.randn(1, 3, 224, 224)

# ONNX形式でエクスポート
torch.onnx.export(
    conv5_extractor,                # カスタムモデル
    dummy_input,                    # ダミー入力
    "resnet18_conv5.onnx",          # 出力ONNXファイル名
    opset_version=11,               # ONNXのバージョン
    input_names=['input'],          # 入力名
    output_names=['conv5_features'] # 出力名
)
print("ONNXモデルをエクスポートしました！")