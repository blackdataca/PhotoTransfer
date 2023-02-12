// See https://aka.ms/new-console-template for more information

if (args.Length == 2)
{
    string sourceRoot = args[0];
    string targetRoot = args[1];
    if (!targetRoot.EndsWith(Path.DirectorySeparatorChar))
        targetRoot+= Path.DirectorySeparatorChar;
    if (!Directory.Exists(targetRoot))
    {
        Console.WriteLine($"target_dir does not exist: {targetRoot}");
        return;
    }
    Console.WriteLine($"PhotoTransfer from {sourceRoot} to {targetRoot}");

    int n = 0;
    int total = 0;
    long sessionBytes = 0;
    foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*.*", System.IO.SearchOption.AllDirectories))
    {
        Thread.Sleep(1);
        n++;
        DateTime creation = File.GetCreationTime(sourceFile);
        DateTime lastWrite = File.GetLastWriteTime(sourceFile);
        string y = creation.ToString("yyyy");
        string m = creation.ToString("MM");
        string d = creation.ToString("yyyy-MM-dd");
        string targetFileNameOnly = Path.GetFileName(sourceFile);
        string targetDir = targetRoot + y + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + d;
        string targetFile = Path.Combine(targetDir , targetFileNameOnly);
        
        Console.Write($"Copying {n}: {sourceFile} to {targetFile} ... ");
        
        if (targetFileNameOnly.StartsWith("."))
        {
            Console.WriteLine("skip");
            continue;
        }

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        if (!File.Exists(targetFile))
        {
            File.Copy(sourceFile, targetFile, false);

            File.SetCreationTime(targetFile, creation);
            File.SetLastWriteTime(targetFile, lastWrite);
            long len = new FileInfo(targetFile).Length;
            sessionBytes += len;
            Console.WriteLine($"copied {BytesToString(len)} session: {BytesToString(sessionBytes)}");
            total++;
        }
        else
        {
            Console.WriteLine("exist");
        }
        
    }
    Console.WriteLine($"Total {total:N0} files, {BytesToString(sessionBytes)} transferred");

}
else
{
    Console.WriteLine("Arguments: PhotoTransfer.exe source_dir target_dir");
}


static String BytesToString(long byteCount)
{
    string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
    if (byteCount == 0)
        return "0" + suf[0];
    long bytes = Math.Abs(byteCount);
    int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
    double num = Math.Round(bytes / Math.Pow(1024, place), 1);
    return (Math.Sign(byteCount) * num).ToString() + suf[place];
}