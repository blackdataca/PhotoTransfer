# PhotoTransfer
Orgainze Google Photos Takeout files

target_dir / yyyy / mm / yyyy-mm-dd / files

## Usages:
1. Copy files from Google Drive app flat directory to directory trees as:
PhotoTransfer.exe source_dir target_dir true|false(true=delete the source file, false=copy the source file)

2. Organize files in the same drive
PhotoTransfer.exe source_dir source_dir true|false(true=delete the source file, false=copy the source file)

## Requirements:
"C:\Program Files\ffmpeg\bin\ffmpeg.exe"
"C:\Program Files\ffmpeg\bin\ffprobe.exe"

## Features:
1. Supported file types: .jpg, .heic, .png, .jpeg, .mp4, .mov, .m4v, .flv, .mts, .avi.
2. Use meta data to create directory.
3. Keep original file creation time.
4. Fix media file orientation by removing side_data from meta.
