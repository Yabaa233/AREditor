import open3d as o3d
import numpy as np
import os

# 输入路径
input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\prehand\fuse_post_prehand.ply"
output_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\prehand\truePly.ply"

# 读取 mesh
mesh = o3d.io.read_triangle_mesh(input_path)

# 确保有顶点颜色
if not mesh.has_vertex_colors():
    raise RuntimeError("❌ 输入模型不包含顶点色！")

# 将 mesh 转为点云，只保留顶点与颜色
pcd = o3d.geometry.PointCloud()
pcd.points = mesh.vertices
pcd.colors = mesh.vertex_colors

# 保存为新的点云 PLY 文件（无面信息）
o3d.io.write_point_cloud(output_path, pcd)
print(f"✅ 点云已保存: {output_path}")
