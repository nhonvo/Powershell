import os
import sys
import subprocess
import argparse

def compress_video(input_path, output_path=None, resolution="720p", quality="medium", codec="libx264"):
    """
    Compresses a video file using FFmpeg.
    """
    if not os.path.exists(input_path):
        print(f"Error: File '{input_path}' not found.")
        return

    if not output_path:
        base, ext = os.path.splitext(input_path)
        output_path = f"{base}_compressed{ext}"

    crf_map = {
        "high": 21,
        "medium": 26,
        "low": 30
    }
    crf = crf_map.get(quality, 26)

    scale_map = {
        "1080p": "scale=-2:1080",
        "720p": "scale=-2:720",
        "480p": "scale=-2:480",
        "original": None
    }
    scale_filter = scale_map.get(resolution)

    cmd = ["ffmpeg", "-y", "-i", input_path]
    if scale_filter:
        cmd.extend(["-vf", scale_filter])
    
    cmd.extend([
        "-c:v", codec,
        "-crf", str(crf),
        "-preset", "faster",
        "-c:a", "aac",
        "-b:a", "128k",
        output_path
    ])

    orig_size = os.path.getsize(input_path) / (1024 * 1024)
    print(f"Compressing '{input_path}' ({orig_size:.2f} MB)...")

    try:
        subprocess.run(cmd, check=True)
        new_size = os.path.getsize(output_path) / (1024 * 1024)
        saved = (1 - (new_size / orig_size)) * 100
        print(f"Success! Compressed file saved to '{output_path}'")
        print(f"New Size: {new_size:.2f} MB (Reduced by {saved:.1f}%)")
    except subprocess.CalledProcessError:
        print("FFmpeg compression failed. Ensure FFmpeg is installed and in PATH.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Downsize and compress video files using FFmpeg.")
    parser.add_argument("-i", "--input", required=True, help="Input video file path")
    parser.add_argument("-r", "--resolution", choices=["1080p", "720p", "480p", "original"], default="720p", help="Target resolution")
    parser.add_argument("-q", "--quality", choices=["high", "medium", "low"], default="medium", help="Quality level")
    
    args = parser.parse_args()
    compress_video(args.input, resolution=args.resolution, quality=args.quality)
