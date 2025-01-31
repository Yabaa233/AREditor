import open3d as o3d
import numpy as np

# 1. 加载点云数据
def load_point_cloud(file_path):
    point_cloud = o3d.io.read_point_cloud(file_path)
    print("Point Cloud Loaded:")
    print(point_cloud)
    o3d.visualization.draw_geometries([point_cloud], window_name="Raw Point Cloud")
    return point_cloud

# 2. 点云预处理
def preprocess_point_cloud(point_cloud, voxel_size=0.01):
    # 统计滤波去除噪点
    print("Removing noise...")
    point_cloud, ind = point_cloud.remove_statistical_outlier(nb_neighbors=20, std_ratio=2.0)
    print(f"Number of points after noise removal: {len(point_cloud.points)}")

    # 下采样以降低点云密度
    print("Downsampling point cloud...")
    point_cloud = point_cloud.voxel_down_sample(voxel_size=voxel_size)
    print(f"Number of points after downsampling: {len(point_cloud.points)}")

    # 法线估计
    print("Estimating normals...")
    point_cloud.estimate_normals(search_param=o3d.geometry.KDTreeSearchParamHybrid(radius=0.1, max_nn=30))
    print("Normals estimated.")
    
    o3d.visualization.draw_geometries([point_cloud], window_name="Preprocessed Point Cloud")
    return point_cloud

# 3. 泊松表面重建
def poisson_surface_reconstruction(point_cloud, depth=9):
    print("Running Poisson Surface Reconstruction...")
    mesh, densities = o3d.geometry.TriangleMesh.create_from_point_cloud_poisson(point_cloud, depth=depth)
    print("Poisson reconstruction completed.")

    # 可视化网格
    print("Visualizing raw Poisson mesh...")
    o3d.visualization.draw_geometries([mesh], window_name="Poisson Mesh")
    return mesh, densities

# 4. 提取网格的最外部表面
def filter_mesh_by_density(mesh, densities, threshold_percentile=5):  # 将默认阈值改为 5%
    print("Filtering mesh based on density...")
    densities = np.asarray(densities)
    threshold = np.percentile(densities, threshold_percentile)  # 改低百分位
    vertices_to_keep = densities > threshold
    filtered_mesh = mesh.remove_vertices_by_mask(~vertices_to_keep)

    # 检查过滤后网格是否为空
    if len(filtered_mesh.vertices) == 0 or len(filtered_mesh.triangles) == 0:
        print("Filtered mesh is empty after density filtering. Try lowering the threshold.")
        return None

    print("Filtered mesh:")
    o3d.visualization.draw_geometries([filtered_mesh], window_name="Filtered Mesh")
    return filtered_mesh


# 5. Ball Pivoting 重建
def ball_pivoting_surface_reconstruction(point_cloud):
    print("Running Ball Pivoting Surface Reconstruction...")
    distances = point_cloud.compute_nearest_neighbor_distance()
    avg_dist = np.mean(distances)
    radii = [avg_dist * 1.5, avg_dist * 2, avg_dist * 2.5]

    mesh = o3d.geometry.TriangleMesh.create_from_point_cloud_ball_pivoting(
        point_cloud, o3d.utility.DoubleVector(radii)
    )
    print("Ball Pivoting reconstruction completed.")
    print(f"Generated mesh has {len(poisson_mesh.vertices)} vertices and {len(poisson_mesh.triangles)} triangles.")
    if len(poisson_mesh.vertices) == 0 or len(poisson_mesh.triangles) == 0:
     print("Poisson reconstruction failed. Please check the input point cloud or parameters.")
     exit()
    o3d.visualization.draw_geometries([mesh], window_name="Ball Pivoting Mesh")
    return mesh

# 6. 保存网格
def save_mesh(mesh, file_path):
    print(f"Saving mesh to {file_path}...")
    o3d.io.write_triangle_mesh(file_path, mesh)
    print("Mesh saved successfully.")

# 主流程
if __name__ == "__main__":
    # 输入点云文件路径（支持 PLY, PCD 等格式）
    # input_file = "data\output\point_cloud\iteration_30000\point_cloud.ply"  # 替换为你的点云文件路径
    input_file = "data\sparse\0\points3D.ply"  # 替换为你的点云文件路径

    output_file_poisson = "output_mesh_poisson.obj"
    output_file_ball_pivoting = "output_mesh_ball_pivoting.obj"

    # 加载点云
    pcd = load_point_cloud(input_file)

    # 预处理点云
    processed_pcd = preprocess_point_cloud(pcd, voxel_size=0.01)

    # 泊松表面重建
    poisson_mesh, densities = poisson_surface_reconstruction(processed_pcd, depth=8)

    # 根据密度过滤网格
    filtered_poisson_mesh = filter_mesh_by_density(poisson_mesh, densities, threshold_percentile=5)
    if filtered_poisson_mesh is not None:
        save_mesh(filtered_poisson_mesh, output_file_poisson)
    else:
        print("Skipping saving filtered mesh as it is empty.")

    # 保存泊松重建结果
    save_mesh(filtered_poisson_mesh, output_file_poisson)

    # Ball Pivoting 重建
    ball_pivoting_mesh = ball_pivoting_surface_reconstruction(processed_pcd)

    # 保存 Ball Pivoting 重建结果
    save_mesh(ball_pivoting_mesh, output_file_ball_pivoting)
