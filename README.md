# PhotoTransfer
Orgainze Google Photos Takeout files

target_dir / yyyy / mm / yyyy-mm-dd / files

## Usages:
1. Copy files from Google Drive app flat directory to directory trees as:

>PhotoTransfer.exe source_dir target_dir -move true|false(true=delete the source file, false=copy the source file) [-imageSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)] [-videoSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)]

3. Organize files in the same drive
>PhotoTransfer.exe source_dir source_dir -move true|false(true=delete the source file, false=copy the source file) [-imageSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)] [-videoSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)]

source_dir The source directory 
target_dir The target directory. Can be empty or already contains data.
-move If this flag is true, files will be deleted from source_dir. If this flag is false, files will remain in source_dir.
-imageSize Define minimum and maximum image file sizes. Supported image files: ".jpg", ".heic", ".jpeg", ".png" 
-videoSize Define minimum and maximum video file sizes. Supported video files: ".mp4", ".mov", ".m4v", ".flv", ".mts", ".avi"


## Requirements:

>"C:\Program Files\ffmpeg\bin\ffmpeg.exe"

and
>"C:\Program Files\ffmpeg\bin\ffprobe.exe"


## Features:
1. Use meta data to create directory.
2. Keep original file creation time.
3. Fix media file orientation by removing side_data from meta.