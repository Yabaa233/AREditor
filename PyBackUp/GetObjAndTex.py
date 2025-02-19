import pymeshlab
import cv2
import numpy as np

# 1. 创建 MeshSet 并加载 PLY 网格
ms = pymeshlab.MeshSet()
ms.load_new_mesh('fuse_post.ply')

# 2. 进行网格简化，减少面数
ms.meshing_decimation_quadric_edge_collapse(targetfacenum=100000)

# 3. 计算纹理坐标参数化
try:
    textdim = 8192  # 纹理分辨率
    ms.compute_texcoord_parametrization_triangle_trivial_per_wedge(textdim=textdim)
except Exception as e:
    print(f"Error in compute_texcoord_parametrization_triangle_trivial_per_wedge: {e}")

# 4. 转移顶点颜色到纹理
impath = 'yourpath_to_save_texture.png'
try:
    ms.transfer_attributes_to_texture_per_vertex(textw=textdim, texth=textdim, textname=impath)
except Exception as e:
    print(f"Error in transfer_attributes_to_texture_per_vertex: {e}")

# 5. 进行 Gamma 校正
def adjust_gamma(image_path, gamma=2.2):
    """
    调整图像的 Gamma 值，避免颜色变暗
    :param image_path: 纹理图像路径
    :param gamma: 伽马值，通常在 1.8 ~ 2.2 之间
    :return: 处理后的图像
    """
    image = cv2.imread(image_path)
    if image is None:
        print(f"Error: Unable to read image at {image_path}")
        return None

    inv_gamma = 1.0 / gamma
    table = np.array([(i / 255.0) ** inv_gamma * 255 for i in np.arange(0, 256)]).astype("uint8")
    corrected_image = cv2.LUT(image, table)
    return corrected_image

# 处理并保存修正后的纹理
corrected_texture = adjust_gamma(impath, gamma=2.2)
if corrected_texture is not None:
    corrected_texture_path = "your_corrected_texture.png"
    cv2.imwrite(corrected_texture_path, corrected_texture)
    print(f"Gamma corrected texture saved to: {corrected_texture_path}")

# 6. 尝试保存 OBJ 文件
try:
    ms.save_current_mesh('yourpath_to_save_obj.obj')
except Exception as e:
    print(f"Error in save_current_mesh: {e}")
    # 尝试手动获取纹理
    try:
        MeshSet = ms[0]
        text = MeshSet.textures()
        if impath in text:
            text[impath].save(impath)
    except Exception as e2:
        print(f"Error in manual texture saving: {e2}")
