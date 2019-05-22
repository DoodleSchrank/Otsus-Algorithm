using System;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Otsus_Algorithm
{

    class Program
    {
        private static bool logEverything = false;
        private static int lightmax = 256;
        private static double[] lightlevels = new double[lightmax];
        private static string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string filename;

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (args.Length > 0)
            {
                // Preprocessing
                filename = args[0];
                Bitmap img = new Bitmap(@"" + path + "\\" + filename);
                int[,] imgarray = preprocess(img);
                Console.WriteLine(imgarray.Length + " Pixel");
                foreach (int val in imgarray)
                {
                    lightlevels[val] += 1 / (double)(imgarray.Length);
                }

                // Histogram
                Bitmap histogram = new Bitmap(1024, 400);
                for (int y = 0; y < histogram.Height; y++)
                {
                    for (int x = 0; x < histogram.Width; x++)
                    {
                        histogram.SetPixel(x, y, Color.White);
                    }
                }
                // Searches highest % for scaling in histogram
                int ll = 0;
                double maxp = 0.0;
                foreach(double val in lightlevels)
                {
                    if (val > maxp) maxp = val;
                }
                Console.WriteLine("Maxp: " + maxp);

                // Creates Histogram
                foreach (double val in lightlevels)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = histogram.Height - 1; i > histogram.Height - (int)((val / maxp) * 400.0); i--)
                        {
                            histogram.SetPixel(ll + j, i, Color.Black);
                        }
                    }
                    ll += 4;
                }

                // Otsu, maximizes effectiveness
                int bestThreshhold = 0;
                double bestEffectiveness = 0;
                double eff;
                for (int i = 1; i < lightmax - 1; i++)
                {
                    eff = calcEffectiveness(i);
                    if (eff > bestEffectiveness)
                    {
                        bestEffectiveness = eff;
                        bestThreshhold = i;
                    }
                }

                int c;

                // Fix indexed Pixels
                Bitmap otsufied = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics gr = Graphics.FromImage(otsufied))
                {
                    gr.DrawImage(img, new Rectangle(0, 0, otsufied.Width, otsufied.Height));
                }

                // Create binary image
                for (int y = 0; y < otsufied.Height; y++)
                {
                    for (int x = 0; x < otsufied.Width; x++)
                    {
                        c = imgarray[x, y];
                        if (c > bestThreshhold)
                        {
                            otsufied.SetPixel(x, y, Color.FromArgb(255, 255, 255));
                        }
                        else
                        {
                            otsufied.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                        }
                    }
                }
                otsufied.Save(path + "\\" + Path.GetFileNameWithoutExtension(filename) + "-otsufied" + Path.GetExtension(filename));

                // Threshhold -> Histogram
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < histogram.Height; i++)
                    {

                        if(histogram.GetPixel(4 * bestThreshhold+j,i) == Color.FromArgb(0,0,0))
                        {
                            histogram.SetPixel(4 * bestThreshhold + j, i, Color.White);
                        } else
                        {
                            histogram.SetPixel(4 * bestThreshhold + j, i, Color.Black);
                        }
                    }
                }
                histogram.Save(path + "\\" + Path.GetFileNameWithoutExtension(filename) + "-histogram" + Path.GetExtension(filename));
                Console.WriteLine("Best Threshhold: " + bestThreshhold);

            }
            else
            {
                Console.WriteLine("Error. Kein Bild nicht angegeben.");
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }

        // Preprocesses Image, returns 2D Array with grayscale(0-255) values for each pixel
        private static int[,] preprocess(Bitmap img)
        {
            int rgb;
            Color c;
            int[,] grayscaleArray = new int[img.Width, img.Height];
            Bitmap grayscale = new Bitmap(img.Width, img.Height);

            // Pixel for pixel, saves grayscale image
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    c = img.GetPixel(x, y);
                    rgb = (int)Math.Round(.299 * c.R + .587 * c.G + .114 * c.B);
                    grayscaleArray[x, y] = rgb;
                    grayscale.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            grayscale.Save(path + "\\" + Path.GetFileNameWithoutExtension(filename) + "-grayscale" + Path.GetExtension(filename));
            return grayscaleArray;
        }

        // Calculates Effectiveness of given Threshhold
        private static double calcEffectiveness(int threshhold)
        {
            double pk1, pk2, meank1, meank2, meang;
            pk1 = pk2 = meank1 = meank2 = meang = 0;
            for (int i = 0; i <= threshhold; i++)
            {
                pk1 += lightlevels[i];
                meank1 += i * lightlevels[i];
            }
            for (int i = threshhold; i < lightmax; i++)
            {
                pk2 += lightlevels[i];
                meank2 += i * lightlevels[i];
            }
            for (int i = 0; i < lightmax; i++)
            {
                meang += i * lightlevels[i];
            }

            meank1 /= pk1;
            meank2 /= pk2;

            //oldstuff
            //pk2 = 1 - pk1;
            //meank2 = (meang - (pk1 * meank1)) / pk2;
            double effectiveness = pk1 * pk2 * Math.Pow(meank1 - meank2, 2);
            Console.WriteLine("Mean1(K):" + meank1.ToString("G"));
            Console.WriteLine("Mean2(k):" + meank2.ToString("G"));

            // Logs every Calculation, if wanted
            if (logEverything)
            {
                Console.WriteLine("P(k):" + pk1.ToString("G"));
                Console.WriteLine("MeanG:" + meang.ToString("G"));
                Console.WriteLine("Effectiveness:" + effectiveness.ToString("G"));
            }
            if (!Double.IsNaN(effectiveness) && !Double.IsInfinity(effectiveness))
            {
                return effectiveness;
            }
            return 0;
        }
    }
}

