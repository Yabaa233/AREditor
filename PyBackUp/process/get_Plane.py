import open3d as o3d
import numpy as np

# === 输入你的 mesh 文件路径 ===
input_path = r"D:\School\AREditor\2DGs\2d-gaussian-splatting\all_data\output_mesh\bigDesk\train\ours_40000\fuse_post.obj"  # ← 替换成你自己的文件路径

# === Step 1: 读取 mesh 并转为点云 ===
mesh = o3d.io.read_triangle_mesh(input_path)
mesh.compute_vertex_normals()
pcd = mesh.sample_points_uniformly(number_of_points=100000)
pcd_down = pcd.voxel_down_sample(voxel_size=0.03)
pcd_down.estimate_normals()

# === Step 2: PCA 对齐点云 ===
def align_point_cloud_with_pca(pcd):
    points = np.asarray(pcd.points)
    center = points.mean(axis=0)
    points_centered = points - center

    cov = np.cov(points_centered.T)
    eigvals, eigvecs = np.linalg.eigh(cov)
    order = eigvals.argsort()[::-1]
    eigvecs = eigvecs[:, order]
    R = eigvecs.T  # Open3D uses right-mult rotation

    pcd_aligned = pcd.translate(-center)
    pcd_aligned = pcd_aligned.rotate(R, center=(0, 0, 0))
    return pcd_aligned, R, center

pcd_aligned, R, center = align_point_cloud_with_pca(pcd_down)

# === Step 3: 多次平面提取 + 水平筛选（z轴）===
platform_planes = []
platform_boxes = []
rest = pcd_aligned

for _ in range(10):
    try:
        plane_model, inliers = rest.segment_plane(
            distance_threshold=0.015,
            ransac_n=3,
            num_iterations=1000
        )
    except:
        break

    inlier_cloud = rest.select_by_index(inliers)
    rest = rest.select_by_index(inliers, invert=True)

    # 法线方向筛选（z方向占比高，即“近水平”）
    normal = np.array(plane_model[:3])
    normal = normal / np.linalg.norm(normal)

    if abs(normal[2]) > 0.9 and len(inlier_cloud.points) > 100:
        # 可选：还原坐标
        # inlier_cloud = inlier_cloud.rotate(R.T, center=(0, 0, 0))
        # inlier_cloud = inlier_cloud.translate(center)

        platform_planes.append(inlier_cloud)

        box = inlier_cloud.get_axis_aligned_bounding_box()
        box.color = (0, 1, 0)
        platform_boxes.append(box)

# === Step 4: 可视化 ===
o3d.visualization.draw_geometries(platform_planes + platform_boxes)
