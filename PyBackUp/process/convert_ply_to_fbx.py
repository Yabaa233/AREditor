import bpy
import os
import sys

def main(input_ply_path):
    # 清除场景
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # 导入PLY（兼容所有Blender 4.x版本）
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

    # 确保顶点颜色可用
    mesh = obj.data
    if not mesh.vertex_colors:
        print("警告：网格没有顶点颜色数据")
    else:
        # 激活第一个顶点颜色层
        mesh.vertex_colors.active = mesh.vertex_colors[0]

    # 导出FBX（Blender 4.3.2专用参数）
    output_fbx_path = os.path.splitext(input_ply_path)[0] + ".fbx"
    
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

    # Blender 4.3.2顶点颜色参数特殊处理
    if bpy.app.version >= (4, 3, 0):
        export_kwargs['colors_type'] = 'SRGB'  # 新版本参数
    else:
        export_kwargs['use_vertex_colors'] = True  # 旧版参数

    bpy.ops.export_scene.fbx(**export_kwargs)
    print(f"成功导出FBX到: {output_fbx_path}")

if __name__ == "__main__":
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        if args:
            main(args[0])
        else:
            print("错误：未提供PLY文件路径")
    else:
        print("请通过Blender运行: blender --python script.py -- input.ply")