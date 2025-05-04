import os
import shutil
import logging
from argparse import ArgumentParser

# --- 参数解析 ---
parser = ArgumentParser("Colmap converter")
parser.add_argument("--image_path", required=True, help="原始图像所在的文件夹")
parser.add_argument("--output_base", required=True, help="输出的基础路径，将自动在其下创建与输入文件夹同名的子目录")
parser.add_argument("--no_gpu", action='store_true')
parser.add_argument("--skip_matching", action='store_true')
parser.add_argument("--camera", default="OPENCV", type=str)
parser.add_argument("--colmap_executable", default="", type=str)
parser.add_argument("--resize", action="store_true")
parser.add_argument("--magick_executable", default="", type=str)
args = parser.parse_args()

colmap_command = f'"{args.colmap_executable}"' if args.colmap_executable else "colmap"
magick_command = f'"{args.magick_executable}"' if args.magick_executable else "magick"
use_gpu = 0 if args.no_gpu else 1

# --- 输出路径构建 ---
image_path = os.path.abspath(args.image_path)
folder_name = os.path.basename(os.path.normpath(image_path))
output_path = os.path.join(os.path.abspath(args.output_base), folder_name)
input_path = os.path.join(output_path, "input")
os.makedirs(output_path, exist_ok=True)
os.makedirs(input_path, exist_ok=True)

# --- 复制图像到 output/input ---
for f in os.listdir(image_path):
    if f.lower().endswith(('.jpg', '.jpeg', '.png')):
        shutil.copy2(os.path.join(image_path, f), os.path.join(input_path, f))
print(f"✅ 正在从 {image_path} 复制图像到 {input_path}")
print("📂 源图像目录内容：", os.listdir(image_path))
print("📂 input 目录内容：", os.listdir(input_path))


# --- 特征提取和匹配 ---
if not args.skip_matching:
    os.makedirs(os.path.join(output_path, "distorted", "sparse"), exist_ok=True)

    # Feature extraction
    feat_extract_cmd = (
        f"{colmap_command} feature_extractor "
        f"--database_path \"{output_path}/distorted/database.db\" "
        f"--image_path \"{input_path}\" "
        f"--ImageReader.single_camera 1 "
        f"--ImageReader.camera_model {args.camera} "
        f"--SiftExtraction.use_gpu {use_gpu}"
    )
    if os.system(feat_extract_cmd) != 0:
        logging.error("❌ Feature extraction failed.")
        exit(1)

    # Feature matching
    match_cmd = (
        f"{colmap_command} exhaustive_matcher "
        f"--database_path \"{output_path}/distorted/database.db\" "
        f"--SiftMatching.use_gpu {use_gpu}"
    )
    if os.system(match_cmd) != 0:
        logging.error("❌ Feature matching failed.")
        exit(1)

    # Mapping
    map_cmd = (
        f"{colmap_command} mapper "
        f"--database_path \"{output_path}/distorted/database.db\" "
        f"--image_path \"{input_path}\" "
        f"--output_path \"{output_path}/distorted/sparse\" "
        f"--Mapper.ba_global_function_tolerance=0.000001"
    )
    if os.system(map_cmd) != 0:
        logging.error("❌ Mapping failed.")
        exit(1)

# --- 图像Undistort ---
undist_cmd = (
    f"{colmap_command} image_undistorter "
    f"--image_path \"{input_path}\" "
    f"--input_path \"{output_path}/distorted/sparse/0\" "
    f"--output_path \"{output_path}\" "
    f"--output_type COLMAP"
)
if os.system(undist_cmd) != 0:
    logging.error("❌ Undistortion failed.")
    exit(1)

# --- 合并 sparse 到 sparse/0 ---
sparse_dir = os.path.join(output_path, "sparse")
os.makedirs(os.path.join(sparse_dir, "0"), exist_ok=True)
for file in os.listdir(sparse_dir):
    if file != "0":
        shutil.move(os.path.join(sparse_dir, file), os.path.join(sparse_dir, "0", file))

# --- 缩放图像（可选） ---
if args.resize:
    print("🔄 Resizing images...")
    base_img_dir = os.path.join(output_path, "images")
    for scale, percent in [("2", 50), ("4", 25), ("8", 12.5)]:
        dst_dir = os.path.join(output_path, f"images_{scale}")
        os.makedirs(dst_dir, exist_ok=True)
        for file in os.listdir(base_img_dir):
            src = os.path.join(base_img_dir, file)
            dst = os.path.join(dst_dir, file)
            shutil.copy2(src, dst)
            resize_cmd = f"{magick_command} mogrify -resize {percent}% \"{dst}\""
            if os.system(resize_cmd) != 0:
                logging.error(f"❌ Resize {percent}% failed for {dst}")
                exit(1)

print("✅ All done.")
