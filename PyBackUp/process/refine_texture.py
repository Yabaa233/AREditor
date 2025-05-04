# refine_texture.py
import argparse
import os
import numpy as np
import cv2
from PIL import Image, ImageFilter

def refine_texture(texture_path, texture_size):
    print(f"[STEP] Post-processing texture: {texture_path}")

    image_bgra = cv2.imread(texture_path, cv2.IMREAD_UNCHANGED)
    if image_bgra is None or image_bgra.shape[2] < 4:
        print("[WARNING] Invalid texture format or no alpha channel, skipping.")
        return

    mask = (image_bgra[:, :, 3] == 0).astype(np.uint8) * 255
    image_bgr = cv2.cvtColor(image_bgra, cv2.COLOR_BGRA2BGR)
    inpainted_bgr = cv2.inpaint(image_bgr, mask, inpaintRadius=3, flags=cv2.INPAINT_TELEA)
    inpainted_bgra = cv2.cvtColor(inpainted_bgr, cv2.COLOR_BGR2BGRA)
    texture_buffer = inpainted_bgra[::-1]

    image_texture = Image.fromarray(texture_buffer)
    image_texture = image_texture.filter(ImageFilter.MedianFilter(size=3))
    # image_texture = image_texture.filter(ImageFilter.GaussianBlur(radius=1))
    # image_texture = image_texture.resize((texture_size, texture_size), Image.LANCZOS)

    image_texture.save(texture_path)
    print(f"[INFO] Texture refined and saved: {texture_path}")

def main():
    parser = argparse.ArgumentParser(description="Refine baked texture image")
    parser.add_argument("-input", required=True, help="Path to the input texture image")
    parser.add_argument("--texture_size", type=int, default=8192, help="Final texture resolution")
    args = parser.parse_args()

    refine_texture(args.input, args.texture_size)

if __name__ == "__main__":
    main()
