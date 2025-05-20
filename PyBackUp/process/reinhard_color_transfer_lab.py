import numpy as np
import cv2
import trimesh
import sys

def reinhard_color_transfer_lab(src_colors, ref_image_path):
    # 1. 归一化并转 LAB（OpenCV 需要 0~255 uint8）
    src_colors = np.clip(src_colors * 255.0, 0, 255).astype(np.uint8)
    lab_src = cv2.cvtColor(src_colors.reshape(-1, 1, 3), cv2.COLOR_RGB2LAB).reshape(-1, 3)

    # 2. 加载参考图并转 LAB
    ref_img = cv2.imread(ref_image_path)
    ref_img = cv2.cvtColor(ref_img, cv2.COLOR_BGR2LAB)
    ref_lab = ref_img.reshape(-1, 3)

    # 3. 计算均值标准差
    src_mean, src_std = lab_src.mean(axis=0), lab_src.std(axis=0)
    ref_mean, ref_std = ref_lab.mean(axis=0), ref_lab.std(axis=0)

    # 4. Reinhard 颜色匹配
    lab_transformed = ((lab_src - src_mean) * (ref_std / src_std)) + ref_mean
    lab_transformed = np.clip(lab_transformed, 0, 255).astype(np.uint8)

    # 5. 转回 RGB 并归一化
    rgb_result = cv2.cvtColor(lab_transformed.reshape(-1, 1, 3), cv2.COLOR_LAB2RGB).reshape(-1, 3)
    return (rgb_result / 255.0).astype(np.float32)

def process_ply(input_ply_path, style_img_path, output_ply_path):
    # 加载 PLY 模型
    mesh = trimesh.load(input_ply_path, process=False)

    if not hasattr(mesh.visual, 'vertex_colors') or mesh.visual.vertex_colors is None:
        print("❌ 输入 PLY 文件不包含顶点颜色")
        return

    # 提取原始 RGB（忽略 Alpha）
    old_colors = mesh.visual.vertex_colors
    rgb = old_colors[:, :3] / 255.0

    # 应用色彩风格迁移
    new_rgb = reinhard_color_transfer_lab(rgb, style_img_path)
    new_rgb_uint8 = (new_rgb * 255).astype(np.uint8)

    # 保留 Alpha（如果有）
    if old_colors.shape[1] == 4:
        alpha = old_colors[:, 3].reshape(-1, 1)
        new_colors = np.hstack((new_rgb_uint8, alpha))
    else:
        new_colors = new_rgb_uint8

    # 构建新 Mesh，确保颜色写入成功
    new_mesh = trimesh.Trimesh(vertices=mesh.vertices,
                               faces=mesh.faces,
                               vertex_colors=new_colors,
                               process=False)

    # 保存为 PLY
    new_mesh.export(output_ply_path)
    print(f"✅ 顶点色风格迁移完成并保存到：{output_ply_path}")

if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("用法: python reinhard_color_transfer_lab.py 输入.ply 参考图.jpg 输出.ply")
        sys.exit(1)

    input_ply = sys.argv[1]
    style_img = sys.argv[2]
    output_ply = sys.argv[3]

    process_ply(input_ply, style_img, output_ply)
