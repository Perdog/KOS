using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    public class HarddiskLoader
    {

        public static void SaveHarddisk(Harddisk disk)
        {

            if (Directory.Exists(disk.Directory))
            {
                Directory.Delete(disk.Directory, true);
            }

            Directory.CreateDirectory(disk.Directory);

            foreach (File file in disk.Files)
            {
                System.IO.File.WriteAllLines(disk.Directory + "/" + file.Filename + ".kos", file.ToArray());
            }
        }

        public static Harddisk LoadHarddisk(String diskName)
        {
            String directory = HighLogic.fetch.GameSaveFolder + "/" + diskName;

            if (Directory.Exists(directory))
            {
                Harddisk hd = new Harddisk(10000);
                hd.Name = diskName;

                String[] files = Directory.GetFiles(directory);

                foreach (String file in files)
                {
                    String[] content = System.IO.File.ReadAllLines(file);
                    File hdFile = new File(file);

                    foreach (String s in content)
                    {
                        hdFile.Add(s);
                    }
                    
                    hd.Files.Add(hdFile);
                }

                return hd;
            }

            return null;
        }

        public static List<File> LoadFiles(String diskName)
        {
            String directory = HighLogic.fetch.GameSaveFolder + "/" + diskName;

            List<File> allFiles = new List<File>();

            if (Directory.Exists(directory))
            {
                String[] files = Directory.GetFiles(directory);

                foreach (String file in files)
                {
                    String[] content = System.IO.File.ReadAllLines(file);
                    File newFile = new File(file);

                    foreach (String s in content)
                    {
                        newFile.Add(s);
                    }

                    allFiles.Add(newFile);
                }

                return allFiles;
            }

            return null;
        }

        public static String[] GetAllHarddiskNames()
        {
            return Directory.GetDirectories(HighLogic.fetch.GameSaveFolder);
        }
    }
}
