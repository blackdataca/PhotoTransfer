// See https://aka.ms/new-console-template for more information
using Microsoft.VisualBasic.FileIO;
using System.Reflection.Metadata.Ecma335;

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
    foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*.*", System.IO.SearchOption.AllDirectories))
    {
        Thread.Sleep(10);
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
            try
            {
                FileSystem.CopyFile(sourceFile, targetFile, UIOption.AllDialogs, UICancelOption.ThrowException);
            }
            catch(System.OperationCanceledException)
            {
                if (File.Exists(targetFile))
                    FileSystem.DeleteFile(targetFile);
                Console.WriteLine("Cancelled");
                return;
            }
            File.SetCreationTime(targetFile, creation);
            File.SetLastWriteTime(targetFile, lastWrite);

            Console.WriteLine("copied");
        }
        else
        {
            Console.WriteLine("exist");
        }
        
    }

}
else
{
    Console.WriteLine("Arguments: PhotoTransfer.exe source_dir target_dir");
}