from flask import Flask, request, jsonify
import requests
import os

app = Flask(__name__)

UPLOAD_DIR = "D:/SFTP/upload"  # 视频上传目录
RESULT_DIR = "D:/SFTP/result"  # 处理结果目录
COMPUTE_SERVER_URL = "http://localhost:5001/process"  # 本地计算端地址

# 接收 Unity 上传的视频文件
@app.route('/upload', methods=['POST'])
def upload_file():
    if 'file' not in request.files:
        return jsonify({"status": "error", "message": "No file part"}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({"status": "error", "message": "No selected file"}), 400

    # 保存上传文件到 UPLOAD_DIR
    upload_path = os.path.join(UPLOAD_DIR, file.filename)
    file.save(upload_path)
    print(f"File uploaded: {upload_path}")

    return jsonify({"status": "success", "file_path": upload_path})

# 启动计算任务
@app.route('/process', methods=['POST'])
def process_video():
    data = request.json
    video_path = data.get("video_path")
    if not video_path or not os.path.exists(video_path):
        return jsonify({"status": "error", "message": "Video file not found"}), 404

    # 将任务转发到本地计算端
    response = requests.post(COMPUTE_SERVER_URL, json={"video_path": video_path})
    if response.status_code == 200:
        result_path = response.json().get("result_path")
        return jsonify({"status": "success", "result_path": result_path})
    else:
        return jsonify({"status": "error", "message": "Compute server error"}), 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
