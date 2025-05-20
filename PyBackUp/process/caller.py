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


# # 风格化
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\stylize.py"
# colmap_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_colmap\bigDesk"
# output_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_stylized"
# checkpoint_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_2dgs\bigDesk\chkpnt30000.pth"
# style_img_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\mc.png"

# # 自动提取最后一级文件夹名（如 loop）
# scene_name = os.path.basename(os.path.normpath(colmap_path))

# # 提取 style 图像的文件名（无扩展名）
# style_name = os.path.splitext(os.path.basename(style_img_path))[0]  # 得到 "monochrome"

# # 构造完整输出路径：output_2dgs/loop_monochrome
# output_path = os.path.join(output_base, f"{scene_name}_{style_name}")
# os.makedirs(output_path, exist_ok=True)

# # 构建命令
# cmd = [
#     "python", script_path,
#     "--source_path", colmap_path,
#     "--model_path", output_path,
#     "--start_checkpoint", checkpoint_path,
#     "--style_img", style_img_path,
#     "--iterations", "45000",
#     "--resolution", "1"
# ]

# 风格化 Structure Change
script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\stylize_structure.py"
colmap_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_colmap\bigDesk"
output_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_stylized"
checkpoint_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_2dgs\bigDesk\chkpnt30000.pth"
style_img_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\mc.png"

# 自动提取最后一级文件夹名（如 loop）
scene_name = os.path.basename(os.path.normpath(colmap_path))

# 提取 style 图像的文件名（无扩展名）
style_name = os.path.splitext(os.path.basename(style_img_path))[0]  # 得到 "monochrome"

# 构造完整输出路径：output_2dgs/loop_monochrome
output_path = os.path.join(output_base, f"{scene_name}_{style_name}")
os.makedirs(output_path, exist_ok=True)

# 构建命令
cmd = [
    "python", script_path,
    "--source_path", colmap_path,
    "--model_path", output_path,
    "--start_checkpoint", checkpoint_path,
    "--style_img", style_img_path,
    "--iterations", "34400",
    "--resolution", "1",
    "--optimize_iteration", "7000",
]

# # tsdf
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\render.py"
# input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_stylized\bigDesk_mc"
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
#     "--iteration", "36000"
# ]

# # 用blender直接转fbx
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\process\GetTex_meshlab.py"
# input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\prehand\fuse_post_prehand.ply"

# # 构建命令（注意参数格式）
# cmd = [
#     "conda", "run", "-n", "gaussian_splatting", "python", script_path,
#     "-input", input_path,
# ]

# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\process\convert_ply_to_fbx.py"
# input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk_256\train\ours_40000\fuse_post_prehand.ply"
# blender_path = r"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"

# # 使用 Blender 运行脚本，并传递参数
# cmd = [
#     blender_path,
#     "--background",  # 无界面模式
#     "--python", script_path,
#     "--",  # Blender 参数结束，后面的参数传给脚本
#     input_path,  # 传递给脚本的第一个参数
# ]

# # 直接风格化贴图
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\stylize_Tex.py"
# Tex_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_Tex\Boom.png"
# style_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\mario.png"
# output_path_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_Tex"

# # 自动提取 scene_name 和拼接输出文件夹路径
# scene_name = os.path.splitext(os.path.basename(Tex_path))[0]  # Boom
# output_dir = os.path.join(output_path_base, scene_name)
# os.makedirs(output_dir, exist_ok=True)

# # 输出完整路径 = output_dir/Boom.png
# output_image_path = os.path.join(output_dir, scene_name + ".png")

# cmd = [
#     "python", script_path,
#     "--content", Tex_path,         # 内容图
#     "--style", style_path,      # 风格图
#     "--output", output_image_path, # 输出图像的完整路径，含.png后缀
#     "--use_gram",  
#     "--use_mean_std"
# ]

# # 只做色彩倾向更改
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\stylize_Tex_simple.py"
# Tex_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\input_Tex\Boom.png"
# style_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\mario.png"
# output_path_base = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_Tex"

# # 自动提取输出文件夹和输出文件名
# scene_name = os.path.splitext(os.path.basename(Tex_path))[0]  # Boom
# output_dir = os.path.join(output_path_base, scene_name)
# os.makedirs(output_dir, exist_ok=True)
# output_image_path = os.path.join(output_dir, scene_name + ".png")

# # 构造命令行调用
# cmd = [
#     "python", script_path,
#     Tex_path,
#     style_path,
#     output_image_path
# ]

# # 直接色彩迁移ply
# # 路径配置
# script_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\process\reinhard_color_transfer_lab.py"
# input_ply_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk_256\train\ours_40000\fuse_post.ply"
# style_image_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\stylize_sourceImage\ghiblies1.jpg"
# output_base_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_colorTransfer"

# # 自动提取文件名并构造输出路径
# scene_name = os.path.splitext(os.path.basename(input_ply_path))[0]  # original_scene
# output_dir = os.path.join(output_base_path, scene_name)
# os.makedirs(output_dir, exist_ok=True)
# output_ply_path = os.path.join(output_dir, scene_name + "_stylized.ply")

# # 构造命令行调用
# cmd = [
#     "python", script_path,
#     input_ply_path,
#     style_image_path,
#     output_ply_path
# ]

try:
    subprocess.run(cmd, check=True)
    print("over")
except subprocess.CalledProcessError as e:
    print(f"failed: {e}")