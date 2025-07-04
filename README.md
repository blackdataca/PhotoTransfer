# PhotoTransfer
Orgainze Google Photos Takeout files into directory trees

target_dir / yyyy / mm / yyyy-mm-dd / files

## Usages:

1. Extrat the Google Photos Takeout files from the zip file to an extrated directory.

2. Transfer files from extrated directory to target directory trees :

>PhotoTransfer.exe 
	source_dir The extrated directory
	target_dir The target directory (target directory can have exiting photos)
	[-move true(default)|false (true = delete the source file, false = copy the source file)] 
	[-imageSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB), default = unlimited] 
	[-videoSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB), default = unlimited]

3. (Optional) Organize files in the same drive
>PhotoTransfer.exe source_dir source_dir -move true|false(true=delete the source file, false=copy the source file) [-imageSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)] [-videoSize >n|<n (only process files in certain sizes. e.g. >1GB<4GB)]

source_dir The source directory 
target_dir The target directory. Can be empty or already contains data.
-move If this flag is true, files will be deleted from source_dir. If this flag is false, files will remain in source_dir.
-imageSize Define minimum and maximum image file sizes. Supported image files: ".jpg", ".heic", ".jpeg", ".png" 
-videoSize Define minimum and maximum video file sizes. Supported video files: ".mp4", ".mov", ".m4v", ".flv", ".mts", ".avi"


## Features:
1. Use meta data to create directory.
2. Keep original file creation time.
3. Fix media file orientation by removing side_data from meta.