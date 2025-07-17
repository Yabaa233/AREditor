# import bpy
# import os
# import sys

# def main(input_ply_path):
#     # 清除场景
#     bpy.ops.wm.read_factory_settings(use_empty=True)

#     # 导入PLY（兼容所有Blender 4.x版本）
#     try:
#         bpy.ops.wm.ply_import(filepath=input_ply_path)
#     except AttributeError:
#         try:
#             bpy.ops.import_mesh.ply(filepath=input_ply_path)
#         except:
#             from io_mesh_ply import import_ply
#             import_ply.load_ply_mesh(input_ply_path)

#     # 验证导入
#     if not bpy.context.selected_objects:
#         raise Exception("PLY导入失败 - 可能文件格式不兼容")
    
#     obj = bpy.context.selected_objects[0]
#     if obj.type != 'MESH':
#         raise Exception("导入对象不是网格类型")

#     # 确保顶点颜色可用
#     mesh = obj.data
#     if not mesh.vertex_colors:
#         print("警告：网格没有顶点颜色数据")
#     else:
#         # 激活第一个顶点颜色层
#         mesh.vertex_colors.active = mesh.vertex_colors[0]

#     # 导出FBX（Blender 4.3.2专用参数）
#     output_fbx_path = os.path.splitext(input_ply_path)[0] + ".fbx"
    
#     export_kwargs = {
#         'filepath': output_fbx_path,
#         'use_selection': True,
#         'mesh_smooth_type': 'FACE',
#         'object_types': {'MESH'},
#         'use_active_collection': False,
#         'global_scale': 1.0,
#         'apply_unit_scale': True,
#         'apply_scale_options': 'FBX_SCALE_ALL',
#         'bake_space_transform': True,
#         'path_mode': 'AUTO'
#     }

#     # Blender 4.3.2顶点颜色参数特殊处理
#     if bpy.app.version >= (4, 3, 0):
#         export_kwargs['colors_type'] = 'SRGB'  # 新版本参数
#     else:
#         export_kwargs['use_vertex_colors'] = True  # 旧版参数

#     bpy.ops.export_scene.fbx(**export_kwargs)
#     print(f"成功导出FBX到: {output_fbx_path}")

# if __name__ == "__main__":
#     if "--" in sys.argv:
#         args = sys.argv[sys.argv.index("--") + 1:]
#         if args:
#             main(args[0])
#         else:
#             print("错误：未提供PLY文件路径")
#     else:
#         print("请通过Blender运行: blender --python script.py -- input.ply")


import bpy
import os
import sys
import argparse

def main(input_ply_path, target_faces):
    # 清除场景
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # 导入 PLY
    try:
        bpy.ops.wm.ply_import(filepath=input_ply_path)
    except AttributeError:
        try:
            bpy.ops.import_mesh.ply(filepath=input_ply_path)
        except:
            from io_mesh_ply import import_ply
            import_ply.load_ply_mesh(input_ply_path)

    # 验证导入
    if not bpy.context.selected_objects:
        raise Exception("PLY导入失败 - 可能文件格式不兼容")

    obj = bpy.context.selected_objects[0]
    if obj.type != 'MESH':
        raise Exception("导入对象不是网格类型")

    bpy.context.view_layer.objects.active = obj
    mesh = obj.data

    # 统计原始面数
    original_face_count = len(mesh.polygons)
    print(f"[INFO] 原始面数: {original_face_count}")

    if original_face_count == 0:
        raise Exception("模型没有有效面")

    # 自动计算 ratio
    ratio = min(1.0, target_faces / original_face_count)
    if ratio >= 1.0:
        print("[INFO] 当前面数已小于目标面数，跳过简化处理。")
    else:
        # 添加 Decimate Modifier
        decimate = obj.modifiers.new(name="Decimate", type='DECIMATE')
        decimate.ratio = ratio
        bpy.ops.object.modifier_apply(modifier="Decimate")
        print(f"[INFO] 面数简化已应用，保留比率: {ratio:.4f}")

    # 顶点色检查
    if not mesh.vertex_colors:
        print("[WARNING] 网格没有顶点颜色数据")
    else:
        mesh.vertex_colors.active = mesh.vertex_colors[0]

    # 导出 FBX
    output_fbx_path = os.path.splitext(input_ply_path)[0] + f"_dec{target_faces}.fbx"

    export_kwargs = {
        'filepath': output_fbx_path,
        'use_selection': True,
        'mesh_smooth_type': 'FACE',
        'object_types': {'MESH'},
        'use_active_collection': False,
        'global_scale': 1.0,
        'apply_unit_scale': True,
        'apply_scale_options': 'FBX_SCALE_ALL',
        'bake_space_transform': True,
        'path_mode': 'AUTO'
    }

    if bpy.app.version >= (4, 3, 0):
        export_kwargs['colors_type'] = 'SRGB'
    else:
        export_kwargs['use_vertex_colors'] = True

    bpy.ops.export_scene.fbx(**export_kwargs)
    print(f"[✅] 成功导出 FBX 到: {output_fbx_path}")

if __name__ == "__main__":
    if "--" in sys.argv:
        argv = sys.argv[sys.argv.index("--") + 1:]
        parser = argparse.ArgumentParser(description="PLY to FBX with face simplification and vertex color support")
        parser.add_argument("--input", required=True, help="输入的PLY路径")
        parser.add_argument("--target_faces", type=int, default=200000, help="目标最大面数（默认200000）")
        args = parser.parse_args(argv)
        main(args.input, args.target_faces)
    else:
        print("请通过Blender运行: blender --python script.py -- --input path/to.ply --target_faces 8000")
