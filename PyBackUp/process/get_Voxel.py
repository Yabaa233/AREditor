import open3d as o3d
import numpy as np

# === Load mesh ===
mesh = o3d.io.read_triangle_mesh(r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\fuse_post.obj")
mesh.compute_vertex_normals()

# === Scale to unit cube for测试 ===
mesh.scale(1 / np.max(mesh.get_max_bound() - mesh.get_min_bound()), center=mesh.get_center())

# === Sample point cloud ===
pcd = mesh.sample_points_poisson_disk(number_of_points=30000)
if not pcd.has_colors():
    pcd.paint_uniform_color([0.8, 0.6, 0.4])

# === 自定义 voxel 对齐原点 ===
voxel_size = 0.02

# step 1: 获取中心对齐边界
center = pcd.get_center()
min_bound = pcd.get_min_bound()
max_bound = pcd.get_max_bound()

# step 2: 手动计算新的 voxel grid 原点，使其从中心对齐开始
voxel_origin = center - np.floor((center - min_bound) / voxel_size) * voxel_size

# step 3: 强制指定 voxel grid 边界
voxel_grid = o3d.geometry.VoxelGrid.create_from_point_cloud_within_bounds(
    pcd,
    voxel_size=voxel_size,
    min_bound=voxel_origin,
    max_bound=max_bound
)

# === 可视化对比 ===
o3d.visualization.draw_geometries([voxel_grid])
