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
                int ll = 0;
                double maxp = 0.0;
                foreach(double val in lightlevels)
                {
                    if (val > maxp) maxp = val;
                }
                Console.WriteLine("Maxp: " + maxp);
                // Rework!!
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

                // Otsu
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
                    //Console.WriteLine();
                }

                // Image Save
                int c;
                Bitmap otsufied = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics gr = Graphics.FromImage(otsufied))
                {
                    gr.DrawImage(img, new Rectangle(0, 0, otsufied.Width, otsufied.Height));
                }
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
                otsufied.Save(path + "\\" + Path.GetFileNameWithoutExtension(filename) + "-otsufied" + Path.GetExtension(filename));
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

        private static int[,] preprocess(Bitmap img)
        {
            int rgb;
            Color c;
            int[,] imgarray = new int[img.Width, img.Height];
            Bitmap save = new Bitmap(img.Width, img.Height);

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    c = img.GetPixel(x, y);
                    rgb = (int)Math.Round(.299 * c.R + .587 * c.G + .114 * c.B);
                    imgarray[x, y] = rgb;
                    save.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            save.Save(path + "\\" + Path.GetFileNameWithoutExtension(filename) + "-grayscale" + Path.GetExtension(filename));
            return imgarray;
        }

        // Calculates Effectiveness of given Threshhold
        private static double calcEffectiveness(int threshhold)
        {
            double pk1, pk2, meank1, meank2, meank, meang;
            pk1 = meank1 = meang = 0;
            for (int i = 0; i <= threshhold; i++)
            {
                pk1 += lightlevels[i];
                meank1 += i * lightlevels[i];
            }
            pk2 = 1 - pk1;
            meank = meank1;
            meank1 /= pk1;
            //Console.WriteLine("P(k):" + pk1.ToString("G"));
            //Console.WriteLine("Mean1(K):" + meank1.ToString("G"));
            //Console.WriteLine("Mean(k):" + meank.ToString("G"));

            for (int i = 0; i < lightmax; i++)
            {
                meang += i * lightlevels[i];
            }
            //Console.WriteLine("MeanG:" + meang.ToString("G"));
            meank2 = (meang - (pk1 * meank1)) / pk2;
            //Console.WriteLine("Mean2(k):" + meank2.ToString("G"));
            //Console.WriteLine("Proof of Stuff:" + (pk1 * meank1 + pk2 * meank2).ToString("G"));
            double effectiveness = pk1 * pk2 * Math.Pow(meank1 - meank2, 2);
            //Console.WriteLine("Effectiveness:" + effectiveness.ToString("G"));
            if (!Double.IsNaN(effectiveness) && !Double.IsInfinity(effectiveness))
            {
                return effectiveness;
            }
            return 0;
        }
    }
}

