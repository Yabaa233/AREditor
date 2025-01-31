from flask import Flask, request, jsonify
import subprocess
import os

app = Flask(__name__)
RESULT_DIR = "D:/SFTP/result"  # 处理结果保存路径

@app.route('/process', methods=['POST'])
def process_video():
    data = request.json
    video_path = data.get("video_path")
    if not video_path or not os.path.exists(video_path):
        return jsonify({"status": "error", "message": "Video file not found"}), 404

    try:
        # Step 1: Convert video to images
        print("Step 1: Converting video to images...")
        subprocess.run([
            "python", "Vedio2Image.py",
            "--input", video_path,
            "--output", os.path.join("data", "input")
        ], check=True)

        # Step 2: Generate point cloud
        print("Step 2: Generating point cloud...")
        subprocess.run([
            "python", "Image2Point.py",
            "-s", "data",
        ], check=True)

        # # Step 3: Train (optional)
        # print("Step 3: Training...")
        # subprocess.run([
        #     "python", "train.py",
        #     "-s", "data",
        #     "-m","data/output"
        # ], check=True)

        # Step 4: Generate density map
        print("Step 4: Generating density map...")
        output_path = os.path.join(RESULT_DIR, os.path.basename(video_path).replace(".mp4", "_topView.png"))
        subprocess.run([
            "python", "GetDensityMap.py",
            "--output", output_path
        ], check=True)

        return jsonify({"status": "success", "result_path": output_path})

    except subprocess.CalledProcessError as e:
        print(f"Error during processing: {e}")
        return jsonify({"status": "error", "message": str(e)}), 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001, debug=True)
