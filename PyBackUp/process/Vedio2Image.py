import subprocess
import shutil
import math
import os
import argparse

# 获取视频的时长（以秒为单位）
def get_video_duration(file_path):
    """
    使用 ffprobe 获取视频的时长
    """
    try:
        result = subprocess.run(
            ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", file_path],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        duration = float(result.stdout.strip())
        return duration
    except Exception as e:
        print(f"无法获取视频时长: {e}")
        return None

# 使用 ffmpeg 提取视频帧
def extract_frames(file_path, output_folder, target_fps):
    """
    使用 ffmpeg 提取视频帧
    """
    # 清空内容
    #clear_folder(os.path.join("data"))
    clear_folder(output_folder)  # ✨ 改成只清空 output

    # 确保输出文件夹存在
    os.makedirs(output_folder, exist_ok=True)
    
    # 设置输出路径格式
    output_pattern = os.path.join(output_folder, "frame_%04d.jpg")
    
    # 调用 ffmpeg 提取帧
    try:
        subprocess.run(
            ["ffmpeg", "-i", file_path, "-vf", f"fps={target_fps}", output_pattern],
            check=True
        )
        print(f"帧提取完成，保存到 {output_folder}")
    except Exception as e:
        print(f"提取帧失败: {e}")

def clear_folder(folder_path):
    if os.path.exists(folder_path):
        for filename in os.listdir(folder_path):
            file_path = os.path.join(folder_path, filename)
            try:
                if os.path.isfile(file_path) or os.path.islink(file_path):
                    os.unlink(file_path)  # 删除文件或符号链接
                elif os.path.isdir(file_path):
                    shutil.rmtree(file_path)  # 删除文件夹
            except Exception as e:
                print(f"无法删除 {file_path}: {e}")

# 主函数
def main():
    # 解析命令行参数
    parser = argparse.ArgumentParser(description="从视频中提取帧并保存为图片")
    parser.add_argument("--input", required=True, help="输入视频文件路径")
    parser.add_argument("--output", required=True, help="输出图片文件夹路径")
    parser.add_argument("--max_frames", type=int, default=200, help="最大帧数限制 (默认: 200)")

    args = parser.parse_args()

    input_file = args.input
    output_base_folder = args.output
    max_frames = args.max_frames

    # 检查输入文件是否存在
    if not os.path.exists(input_file):
        print(f"输入文件不存在: {input_file}")
        return
    
    # 以视频文件名创建子文件夹
    video_name = os.path.splitext(os.path.basename(input_file))[0]
    output_folder = os.path.join(output_base_folder, video_name)

    # 获取视频时长
    duration = get_video_duration(input_file)
    if duration is None:
        return
    
    print(f"视频时长: {duration:.2f} 秒")
    
    # 计算目标帧率
    target_fps = max_frames / duration
    target_fps = math.ceil(target_fps)  # 向上取整，确保帧数不会超过 max_frames
    print(f"目标帧率: {target_fps} fps")
    
    # 提取视频帧
    extract_frames(input_file, output_folder, target_fps)

if __name__ == "__main__":
    main()
