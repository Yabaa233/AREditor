from instant_texture import Converter

converter = Converter(
    texture_size=1024,
    simplify_faces=50000,  # 可选简化
    fast_mode=True         # 可选快速模式
)
converter.convert(r"D:\School\AREditor\2DGs\2d-gaussian-splatting\output\1d0701f6-5\train\ours_30000\fuse_post.obj")