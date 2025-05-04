import subprocess
import os
#conda activate surfel_splatting

# input_video = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_vedio\bigDesk.mp4"
# output_dir = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_images"
# script_path = r"d:\School\AREditor\2DGs\2d-gaussian-splatting\process\Vedio2Image.py"

# cmd = [
#     "python", script_path,
#     "--input", input_video,
#     "--output", output_dir,
#     "--max_frames", "150"
# ]


# image_path   = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_images\bigDesk"
# output_base  = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_colmap"
# script_path = r"d:\School\AREditor\2DGs\2d-gaussian-splatting\process\Image2Colmap.py"

# cmd = [
#     "python", script_path,
#     "--image_path", image_path,
#     "--output_base", output_base
# ]


# # 输入路径（COLMAP 完成后的数据）
# colmap_path  = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_colmap\bigDesk"
# # 输出路径（训练结果、checkpoint 保存路径）
# output_base  = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_2dgs"
# # 训练脚本路径
# script_path  = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\train.py"

# # 自动提取最后一级文件夹名（如 loop）
# scene_name = os.path.basename(os.path.normpath(colmap_path))

# # 构造完整输出路径：output_2dgs/loop
# output_path = os.path.join(output_base, scene_name)
# os.makedirs(output_path, exist_ok=True)

# # 构造命令
# cmd = [
#     "python", script_path,
#     "--source_path", colmap_path,
#     "--model_path", output_path,
#     "--checkpoint_iterations", "7000", "30000"
# ]


# # 参数路径（你可以根据需要修改）
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\stylize.py"
# colmap_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_colmap\bigDesk"
# output_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_stylized"
# checkpoint_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_2dgs\bigDesk\chkpnt30000.pth"
# style_img_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\mario.png"

# # 自动提取最后一级文件夹名（如 loop）
# scene_name = os.path.basename(os.path.normpath(colmap_path))

# # 构造完整输出路径：output_2dgs/loop
# output_path = os.path.join(output_base, scene_name)
# os.makedirs(output_path, exist_ok=True)

# # 构建命令
# cmd = [
#     "python", script_path,
#     "--source_path", colmap_path,
#     "--model_path", output_path,
#     "--start_checkpoint", checkpoint_path,
#     "--style_img", style_img_path,
#     "--iterations", "40000",
#     "--resolution", "1"
# ]


# # 参数路径（你可以根据需要修改）
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\render.py"
# input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_stylized\bigDesk_256"
# output_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh"

# # 自动提取最后一级文件夹名（如 loop）
# scene_name = os.path.basename(os.path.normpath(input_path))

# # 构造完整输出路径：output_2dgs/loop
# output_path = os.path.join(output_base, scene_name)
# os.makedirs(output_path, exist_ok=True)

# # 构建命令
# cmd = [
#     "python", script_path,
#     "-m", input_path,
#     "--output_dir", output_path,
#     "--mesh_res", "256",
# ]

# # 参数路径
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\process\GetTex_meshlab.py"
# input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\prehand\fuse_post_prehand.ply"

# # 构建命令（注意参数格式）
# cmd = [
#     "conda", "run", "-n", "gaussian_splatting", "python", script_path,
#     "-input", input_path,
# ]

script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\process\convert_ply_to_fbx.py"
input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk_256\train\ours_40000\fuse_post_prehand.ply"
blender_path = r"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"

# 使用 Blender 运行脚本，并传递参数
cmd = [
    blender_path,
    "--background",  # 无界面模式
    "--python", script_path,
    "--",  # Blender 参数结束，后面的参数传给脚本
    input_path,  # 传递给脚本的第一个参数
]


try:
    subprocess.run(cmd, check=True)
    print("over")
except subprocess.CalledProcessError as e:
    print(f"failed: {e}")