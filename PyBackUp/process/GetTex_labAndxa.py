import pymeshlab
import xatlas
import trimesh
import numpy as np
import os
import argparse
import tempfile
import time

def process(input_ply):
    input_dir = os.path.dirname(input_ply)
    base_name = os.path.splitext(os.path.basename(input_ply))[0]

    texture_filename = f"{base_name}_texture.png"
    output_obj_path = os.path.join(input_dir, f"{base_name}.obj").replace("\\", "/")
    mtl_path = os.path.join(input_dir, f"{base_name}.mtl").replace("\\", "/")

    print(f"[1/6] Loading input mesh: {input_ply}")
    print(f"Output OBJ: {output_obj_path}")
    print(f"Output texture: {texture_filename}")
    start_time = time.time()

    print("[2/6] Simplifying mesh using PyMeshLab...")
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(input_ply)
    ms.meshing_decimation_quadric_edge_collapse(targetfacenum=100000)

    with tempfile.NamedTemporaryFile(suffix=".ply", delete=False) as tmp:
        simplified_ply_path = tmp.name
    ms.save_current_mesh(simplified_ply_path)

    print("[3/6] Loading simplified mesh and generating UVs with xatlas...")
    mesh = trimesh.load(simplified_ply_path, process=False)
    vertices = mesh.vertices.astype(np.float32)
    faces = mesh.faces.astype(np.int32)
    colors = mesh.visual.vertex_colors[:, :3] if hasattr(mesh.visual, 'vertex_colors') else np.full((len(vertices), 3), 255)

    print("[4/6] Generating UVs using xatlas...")
    vmapping, indices, uvs = xatlas.parametrize(vertices, faces)
    vertices_uv = vertices[vmapping]
    colors_uv = colors[vmapping]
    mesh_uv = trimesh.Trimesh(vertices=vertices_uv, faces=indices, vertex_colors=colors_uv, process=False)
    mesh_uv.visual.uv = uvs

    # Save as .obj instead of .ply to preserve UVs in a way PyMeshLab accepts
    with tempfile.NamedTemporaryFile(suffix=".obj", delete=False) as tmp:
        uv_obj_path = tmp.name
    mesh_uv.export(uv_obj_path)

    print("[5/6] Baking texture using PyMeshLab...")
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(uv_obj_path)

    textdim = 8192
    try:
        ms.transfer_attributes_to_texture_per_vertex(
            textw=textdim,
            texth=textdim,
            textname=texture_filename
        )
        # Save mesh with baked texture (also outputs .png)
        ms.save_current_mesh(output_obj_path)
    except Exception as e:
        print(f"[BAKE ERROR] {e}")
        return

    print("[6/6] Fixing material references in OBJ...")
    try:
        with open(output_obj_path, "r") as f:
            lines = f.readlines()
        with open(output_obj_path, "w") as f:
            f.write(f"mtllib {os.path.basename(mtl_path)}\n")
            usemtl_written = False
            for line in lines:
                if line.startswith("usemtl"):
                    if not usemtl_written:
                        f.write("usemtl material\n")
                        usemtl_written = True
                else:
                    f.write(line)
    except Exception as e:
        print(f"[SAVE OBJ FIX ERROR] {e}")

    print("✅ Export complete:", output_obj_path)
    print(f"⏱️ Total time: {time.time() - start_time:.2f} seconds")

    for temp_path in [simplified_ply_path, uv_obj_path]:
        if os.path.exists(temp_path):
            os.remove(temp_path)

def main():
    parser = argparse.ArgumentParser(description="Convert PLY to textured OBJ using xatlas UVs + PyMeshLab bake")
    parser.add_argument("-input", required=True, help="Input PLY file path")
    args = parser.parse_args()
    process(args.input)

if __name__ == "__main__":
    main()
