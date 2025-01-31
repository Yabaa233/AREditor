import os
import open3d as o3d
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path
from scipy.ndimage import sobel, gaussian_filter

# 固定输入路径
INPUT_FILE_PATH = "data/output/point_cloud/iteration_30000/point_cloud.ply"

def rotate_points_y(points, angle_deg):
    """
    绕 y 轴旋转点云
    :param points: 点云的 3D 坐标
    :param angle_deg: 旋转角度，单位为度
    :return: 旋转后的点云
    """
    angle_rad = np.deg2rad(angle_deg)
    rotation_matrix = np.array([
        [np.cos(angle_rad), 0, np.sin(angle_rad)],
        [0, 1, 0],
        [-np.sin(angle_rad), 0, np.cos(angle_rad)]
    ])
    return np.dot(points, rotation_matrix.T)

def filter_by_height(points, percentage):
    """
    按高度百分比过滤点云
    :param points: 点云的 3D 坐标
    :param percentage: 要保留的高度范围的百分比（如 0.4 表示去掉上部 40%）
    :return: 过滤后的点云
    """
    height_values = points[:, 1]
    min_height = np.percentile(height_values, 100 * percentage)
    filtered_points = points[height_values >= min_height]
    return filtered_points

def compute_density_map(points, grid_size):
    """
    根据点云生成密度图
    :param points: 点云的 2D 坐标
    :param grid_size: 网格大小
    :return: 密度图, x 和 z 的范围
    """
    xz_points = points[:, [0, 2]]
    x_min, x_max = np.min(xz_points[:, 0]), np.max(xz_points[:, 0])
    z_min, z_max = np.min(xz_points[:, 1]), np.max(xz_points[:, 1])
    x_bins = int((x_max - x_min) / grid_size)
    z_bins = int((z_max - z_min) / grid_size)
    density_map = np.zeros((x_bins, z_bins))

    for point in xz_points:
        x_idx = int((point[0] - x_min) / grid_size)
        z_idx = int((point[1] - z_min) / grid_size)
        if 0 <= x_idx < x_bins and 0 <= z_idx < z_bins:
            density_map[x_idx, z_idx] += 1

    return density_map, (x_min, x_max, z_min, z_max)

def sharpen_density_map(density_map):
    """
    对密度图进行锐化处理
    :param density_map: 输入的密度图
    :return: 锐化后的密度图
    """
    smoothed_density = gaussian_filter(density_map, sigma=1)
    sharpened_density = density_map + (density_map - smoothed_density) * 0.5
    return np.clip(sharpened_density, 0, None)

def detect_edges(density_map):
    """
    对密度图进行边缘检测
    :param density_map: 输入的密度图
    :return: 边缘检测结果
    """
    dx = sobel(density_map, axis=0)
    dz = sobel(density_map, axis=1)
    edges = np.hypot(dx, dz)
    return edges / edges.max()

def generate_density_map(input_path, output_path):
    """
    根据点云生成密度图
    :param input_path: 固定的输入点云路径
    :param output_path: 动态指定的输出路径
    """
    input_file_path = Path(input_path)

    # 检查输入文件是否存在
    if not input_file_path.exists():
        print(f"错误：输入文件 {input_file_path} 不存在。")
        return

    # 读取点云数据
    pcd = o3d.io.read_point_cloud(str(input_file_path))
    points = np.asarray(pcd.points)

    # 按高度百分比过滤点云
    height_percentage = 0.1  # 设置高度保留百分比
    filtered_points = filter_by_height(points, height_percentage)
    if filtered_points.shape[0] == 0:
        print("过滤后点云为空，请调整高度百分比。")
        return

    # 绕 y 轴旋转 45 度
    rotated_points = rotate_points_y(filtered_points, 45)

    # 生成密度图
    grid_size = 0.1
    density_map, extent = compute_density_map(rotated_points, grid_size)

    # 对数变换增强
    density_map = np.log(density_map + 1)

    # 后处理（默认使用锐化）
    processed_density = sharpen_density_map(density_map)
    # 如果需要边缘检测，将下面一行代码取消注释
    # processed_density = detect_edges(density_map)

    # 绘制密度图（不显示标尺）
    plt.imshow(processed_density.T, origin='lower', cmap='binary', aspect='equal',
               extent=extent, interpolation='nearest')
    plt.axis('off')  # 去掉坐标轴和标尺

    # 保存密度图
    plt.tight_layout(pad=0)  # 减少额外的边距
    plt.savefig(output_path, bbox_inches='tight', pad_inches=0)
    plt.close()  # 关闭绘图窗口
    print(f"密度图已保存到 {output_path}")

if __name__ == "__main__":
    import argparse
    # 解析命令行参数（仅动态指定输出路径）
    parser = argparse.ArgumentParser(description="Generate a density map from point cloud")
    parser.add_argument("--output", required=True, help="Path to save the output density map image (.png)")
    args = parser.parse_args()

    # 调用生成密度图的函数
    generate_density_map(INPUT_FILE_PATH, args.output)
