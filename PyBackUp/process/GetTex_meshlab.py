import pymeshlab
import os
import time
import argparse
from PIL import Image, ImageFilter
import numpy as np
import cv2

def refine_texture(texture_path, texture_size):
    print(f"[STEP 5] Post-processing texture: {texture_path}")

    # 加载 BGRA 图像（确保包含透明通道）
    image_bgra = cv2.imread(texture_path, cv2.IMREAD_UNCHANGED)
    if image_bgra is None or image_bgra.shape[2] < 4:
        print("[WARNING] Invalid texture format or no alpha channel, skipping post-process.")
        return

    # Step 1: 图像修复 - 使用 alpha 通道生成 mask
    mask = (image_bgra[:, :, 3] == 0).astype(np.uint8) * 255
    image_bgr = cv2.cvtColor(image_bgra, cv2.COLOR_BGRA2BGR)
    inpainted_bgr = cv2.inpaint(image_bgr, mask, inpaintRadius=3, flags=cv2.INPAINT_TELEA)
    inpainted_bgra = cv2.cvtColor(inpainted_bgr, cv2.COLOR_BGR2BGRA)

    # Step 2: 翻转图像方向（如果需要）
    texture_buffer = inpainted_bgra[::-1]

    # Step 3: 转换为 PIL 图像进行滤波 + 降采样
    image_texture = Image.fromarray(texture_buffer)
    image_texture = image_texture.filter(ImageFilter.MedianFilter(size=3))
    image_texture = image_texture.filter(ImageFilter.GaussianBlur(radius=2))
    image_texture = image_texture.resize((texture_size, texture_size), Image.LANCZOS)

    # Step 4: 保存处理后的图像
    image_texture.save(texture_path)
    print(f"[INFO] Texture refined and saved: {texture_path}")

def process(input_ply, texture_size=1024, target_faces=200000):
    input_dir = os.path.dirname(input_ply)
    base_name = os.path.splitext(os.path.basename(input_ply))[0]
    texture_filename = f"{base_name}_texture.png"
    output_obj_path = os.path.join(input_dir, f"{base_name}.obj").replace('\\', '/')
    texture_full_path = os.path.join(input_dir, texture_filename).replace('\\', '/')

    print(f"Processing mesh: {input_ply}")
    print(f"Output OBJ: {output_obj_path}")
    print(f"Output texture: {texture_full_path}")

    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(input_ply)

    # 网格简化
    ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target_faces)

    # 网格清理与平滑
    print("[STEP] Cleaning mesh...")
    ms.apply_filter('meshing_remove_duplicate_vertices')
    ms.apply_filter('meshing_remove_duplicate_faces')
    ms.apply_filter('meshing_remove_null_faces')
    ms.apply_filter('meshing_remove_unreferenced_vertices')
    ms.apply_filter('meshing_remove_t_vertices')  # 正确的名称是这个
    ms.apply_filter('meshing_repair_non_manifold_edges')
    ms.meshing_repair_non_manifold_vertices()
    ms.meshing_snap_mismatched_borders()

    ms.apply_coord_laplacian_smoothing()

    # UV 参数化
    try:
        ms.compute_texcoord_parametrization_triangle_trivial_per_wedge(textdim=texture_size)
    except Exception as e:
        print(f"[UV ERROR] {e}")
        return

    # 烘焙贴图
    try:
        ms.transfer_attributes_to_texture_per_vertex(
            textw=texture_size,
            texth=texture_size,
            textname=texture_filename
        )
    except Exception as e:
        print(f"[BAKE ERROR] {e}")
        return

    # # 等待文件写入完成（最多等待10秒）
    # timeout = 10
    # while not os.path.exists(texture_full_path) and timeout > 0:
    #     time.sleep(0.2)
    #     timeout -= 0.2

    # # 图像后处理
    # if os.path.exists(texture_full_path):
    #     refine_texture(texture_full_path, texture_size)
    # else:
    #     print("[TEXTURE FILE MISSING] Skipping post-processing.")

    # 保存 OBJ 文件
    try:
        ms.save_current_mesh(output_obj_path)
        print("OBJ exported successfully.")
    except Exception as e:
        print(f"[SAVE OBJ ERROR] {e}")

def main():
    parser = argparse.ArgumentParser(description="Convert PLY to textured OBJ with post-processed texture")
    parser.add_argument("-input", required=True, help="Input PLY file path")
    parser.add_argument("--texture_size", type=int, default=4096, help="Final texture resolution")
    parser.add_argument("--target_faces", type=int, default=100000, help="Target face count after simplification")
    args = parser.parse_args()

    process(args.input, texture_size=args.texture_size, target_faces=args.target_faces)

if __name__ == "__main__":
    main()
