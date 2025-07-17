import open3d as o3d
import numpy as np
from sklearn.cluster import DBSCAN
import matplotlib.pyplot as plt

# === 参数设置 ===
input_mesh_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\fuse_post.obj"  # 替换为你的模型路径
angle_threshold_deg = 30
eps_distance = 0.2
min_samples = 5
area_threshold = 0.0005

# === Step 0: 读取 mesh 并计算法线 ===
mesh = o3d.io.read_triangle_mesh(input_mesh_path)
mesh.compute_triangle_normals()

# === Step 1: 法线平均方向对齐 Z 轴 ===
def align_by_average_normal(mesh):
    normals = np.asarray(mesh.triangle_normals)
    avg_normal = normals.mean(axis=0)
    avg_normal = avg_normal / np.linalg.norm(avg_normal)

    target = np.array([0, 0, 1])  # 希望对齐到 Z 轴
    axis = np.cross(avg_normal, target)
    angle = np.arccos(np.clip(np.dot(avg_normal, target), -1.0, 1.0))

    if np.linalg.norm(axis) < 1e-6:
        R = np.eye(3)
    else:
        axis = axis / np.linalg.norm(axis)
        R = o3d.geometry.get_rotation_matrix_from_axis_angle(axis * angle)

    center = np.mean(np.asarray(mesh.vertices), axis=0)
    mesh_aligned = mesh.translate(-center)
    mesh_aligned = mesh_aligned.rotate(R, center=(0, 0, 0))
    return mesh_aligned, R, center

mesh_aligned, R, center = align_by_average_normal(mesh)
vertices_aligned = np.asarray(mesh_aligned.vertices)
triangles = np.asarray(mesh_aligned.triangles)
triangle_normals = np.asarray(mesh_aligned.triangle_normals)

# === Step 2: 找“近似水平”的三角形 + 面积过滤 ===
dot_z = triangle_normals @ np.array([0, 0, 1])
angles = np.degrees(np.arccos(np.clip(dot_z, -1.0, 1.0)))
horizontal_mask = angles < angle_threshold_deg
horizontal_triangle_indices = np.where(horizontal_mask)[0]

def triangle_area(a, b, c):
    return 0.5 * np.linalg.norm(np.cross(b - a, c - a))

filtered_indices = []
for i in horizontal_triangle_indices:
    a, b, c = vertices_aligned[triangles[i]]
    if triangle_area(a, b, c) > area_threshold:
        filtered_indices.append(i)
horizontal_triangle_indices = np.array(filtered_indices)

# === Step 3: 三角形重心聚类 ===
centers = np.array([
    (vertices_aligned[t[0]] + vertices_aligned[t[1]] + vertices_aligned[t[2]]) / 3.0
    for t in triangles[horizontal_triangle_indices]
])

if len(centers) == 0:
    print("❌ 没有符合条件的三角形")
else:
    clustering = DBSCAN(eps=eps_distance, min_samples=min_samples).fit(centers)
    labels = clustering.labels_
    num_clusters = len(set(labels)) - (1 if -1 in labels else 0)
    print(f"✅ 聚类结果：{num_clusters} 个水平区域")

    colors = plt.get_cmap('tab20').colors
    geometries = []

    for i in range(num_clusters):
        cluster_mask = labels == i
        cluster_tri_ids = horizontal_triangle_indices[cluster_mask]
        if len(cluster_tri_ids) == 0:
            continue

        cluster_tris = triangles[cluster_tri_ids]
        vertex_ids = np.unique(cluster_tris)
        if len(vertex_ids) < 3:
            continue

        id_map = {vid: idx for idx, vid in enumerate(vertex_ids)}  # ← 用的是大括号 {}
        remapped_tris = np.array([[id_map[v] for v in tri] for tri in cluster_tris])
        new_mesh = o3d.geometry.TriangleMesh()
        new_mesh.vertices = o3d.utility.Vector3dVector(vertices_aligned[vertex_ids])
        new_mesh.triangles = o3d.utility.Vector3iVector(remapped_tris)
        new_mesh.paint_uniform_color(colors[i % len(colors)])

        # 可选：还原坐标系
        new_mesh.rotate(R.T, center=(0, 0, 0))
        new_mesh.translate(center)

        geometries.append(new_mesh)

    if geometries:
        o3d.visualization.draw_geometries(geometries)
    else:
        print("❌ 虽然聚类成功，但可视化为空")
