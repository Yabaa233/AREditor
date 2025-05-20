import open3d as o3d
import numpy as np
import os

# === 用户输入路径（支持 ply, obj, fbx 等） ===
input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\fuse_post.obj"

# === 读取为点云 ===
print(f"Loading: {input_path}")
if input_path.endswith(".ply") or input_path.endswith(".pcd"):
    pcd = o3d.io.read_point_cloud(input_path)
else:
    mesh = o3d.io.read_triangle_mesh(input_path)
    mesh.compute_vertex_normals()
    if not mesh.has_triangles():
        raise ValueError("Mesh has no triangles.")
    pcd = mesh.sample_points_uniformly(number_of_points=100000)
print(f"Loaded point cloud with {len(pcd.points)} points.")

# === Step 1: 提取大平面（如墙、地面） ===
plane_boxes = []
rest = pcd
for _ in range(10):  # 最多10个大结构
    plane_model, inliers = rest.segment_plane(distance_threshold=0.01,
                                               ransac_n=3,
                                               num_iterations=1000)
    inlier_cloud = rest.select_by_index(inliers)
    rest = rest.select_by_index(inliers, invert=True)

    if len(inlier_cloud.points) < 1000:
        continue  # 排除太小的平面

    aabb = inlier_cloud.get_axis_aligned_bounding_box()
    if max(aabb.get_extent()) < 0.3:
        continue  # 跳过太小的面

    aabb.color = (1, 0, 0)  # 红色：结构
    plane_boxes.append(aabb)

# === Step 2: 聚类剩余点云，提取物体 ===
labels = np.array(rest.cluster_dbscan(eps=0.02, min_points=30, print_progress=True))
max_label = labels.max()
print(f"DBSCAN found {max_label + 1} clusters.")

object_boxes = []
for i in range(max_label + 1):
    indices = np.where(labels == i)[0]
    cluster = rest.select_by_index(indices)

    if len(cluster.points) < 50:
        continue  # 排除太小的簇

    aabb = cluster.get_axis_aligned_bounding_box()
    size = aabb.get_extent()

    if any(s > 4.0 for s in size):  # 排除过大box
        continue
    if any(s < 0.05 for s in size):  # 排除过小box
        continue

    aabb.color = (0, 1, 0)  # 绿色：家具类
    object_boxes.append(aabb)

print(f"Final object boxes: {len(object_boxes)}")

# === Step 3: 可视化 ===
show_pointcloud = True  # 是否显示原始点云
geometries = []
if show_pointcloud:
    geometries.append(pcd)
geometries += plane_boxes + object_boxes

o3d.visualization.draw_geometries(geometries)
