using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WF2oWFN.Compiler.Frontend;

namespace WF2oWFN
{
    /// <summary>
    /// Helper Class
    /// </summary>
    class Helper
    {
        /// <summary>
        /// Changes the file extension of all files in folder and subfolders.
        /// </summary>
        /// <param name="dir">The root folder</param>
        /// <param name="oldExtension">The old extension</param>
        /// <param name="newextension">The new extension</param>
        public static void ChangeFileExtension(DirectoryInfo dir, string oldExtension, string newextension)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Extension.Equals("." + oldExtension))
                {
                    string ourPath = file.FullName;
                    string newPath = Path.ChangeExtension(ourPath, newextension);
                    File.Move(ourPath, newPath);
                }
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                ChangeFileExtension(subDir, oldExtension, newextension);
            }
        }

        /// <summary>
        /// Computes the compression ratio of the Screener for all files in the given directory. 
        /// </summary>
        /// <param name="dir">The folder containing xaml(x) files</param>
        /// <returns>The compression ratio after using the Screener</returns>
        public static double ScreenerStatistics(DirectoryInfo dir)
        {
            IList<double> ratios = new List<double>();
            double sumRatios = 0.0;
            string tempDir = System.IO.Path.GetTempPath();
 
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Extension.Equals(".xaml") || file.Extension.Equals(".xamlx"))
                {
                    // Compute Filesize (1 Byte = 1 Literal)
                    long sizeBefore = file.Length;
                    // Do Screener
                    string outputPath = tempDir + file.Name + ".temp";

                    Screener screener = new Screener(file.FullName, outputPath); // TODO FilePath In Output
                    screener.ScreenXaml();
                    // Compute Filesize
                    FileInfo outputFile = new FileInfo(outputPath);
                    long sizeAfter = outputFile.Length;
                    // Delete Tempfile
                    outputFile.Delete();
                    // Add Ratio
                    double ratio = (double) sizeAfter / sizeBefore;
                    ratios.Add(ratio);
                }
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                double subratio = ScreenerStatistics(subDir);

                if (subratio > 0)
                {
                    ratios.Add(ScreenerStatistics(subDir));
                }
            }

            // Compute Average Ratio
            foreach(double ratio in ratios) 
            {
                sumRatios += ratio;
            }

            if (ratios.Count > 0)
            {
                return (double)sumRatios / ratios.Count;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
