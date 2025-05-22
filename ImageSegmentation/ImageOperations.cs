using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageTemplate
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }
    }

  
    public class Graph2D
    {
        public static (
            List<(int, int, double)>[][] RedGraph,
            List<(int, int, double)>[][] GreenGraph,
            List<(int, int, double)>[][] BlueGraph)
            Build2DGraph(RGBPixel[,] ImageMatrix)
        {
            int height = ImageMatrix.GetLength(0);
            int width = ImageMatrix.GetLength(1);

            //Neighbors
            var redGraph = new List<(int, int, double)>[height][];
            var greenGraph = new List<(int, int, double)>[height][];
            var blueGraph = new List<(int, int, double)>[height][];

            //Initialize heights --> weights --> neighbors
            for (int i = 0; i < height; i++)
            {
                redGraph[i] = new List<(int, int, double)>[width];
                greenGraph[i] = new List<(int, int, double)>[width];
                blueGraph[i] = new List<(int, int, double)>[width];

                for (int j = 0; j < width; j++)
                {
                    redGraph[i][j] = new List<(int, int, double)>(4);
                    greenGraph[i][j] = new List<(int, int, double)>(4);
                    blueGraph[i][j] = new List<(int, int, double)>(4);
                }
            }

            Parallel.For(0, height, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    RGBPixel p1 = ImageMatrix[i, j];

                    void add(int x, int y) =>
                        AddEdge(p1, ImageMatrix[x, y], x, y, redGraph[i][j], greenGraph[i][j], blueGraph[i][j]);

                    // Right
                    if (j + 1 < width) add(i, j + 1);
                    // Down
                    if (i + 1 < height) add(i + 1, j);
                    // down-right
                    if (i + 1 < height && j + 1 < width)
                        add(i + 1, j + 1);
                    // down-left
                    if (i + 1 < height && j - 1 >= 0)
                        add(i + 1, j - 1);
                }
            });
            return (redGraph, greenGraph, blueGraph);
        }
        private static void AddEdge(RGBPixel p1, RGBPixel p2, int x2, int y2,
            List<(int, int, double)> redNeighbors,
            List<(int, int, double)> greenNeighbors,
            List<(int, int, double)> blueNeighbors)
        {
            redNeighbors.Add((x2, y2, Math.Abs(p1.red - p2.red)));
            greenNeighbors.Add((x2, y2, Math.Abs(p1.green - p2.green)));
            blueNeighbors.Add((x2, y2, Math.Abs(p1.blue - p2.blue)));
        }
    }

   
    public class Segmentation
    {
        static int[] parent;
        static int[] rank;
        static int[] componentSize;
        static double[] internaldifference;



        public static int[] Kruskal(
      List<(int, int, double)>[][] vertices, RGBPixel[,] ImageMatrix, double k)
        {
            int height = ImageOperations.GetHeight(ImageMatrix);
            int width = ImageOperations.GetWidth(ImageMatrix);
            // int size = height * width;

            parent = new int[height * width];
            rank = new int[height * width];
            componentSize = new int[height * width];
            internaldifference = new double[height * width];
            for (int i = 0; i < height * width; i++)
            {
                make_set(i, i);
                componentSize[i] = 1;
                internaldifference[i] = 0;
            }

            int maxedgesize = height * width * 2;
            var edges = new List<(int, int, double)>(maxedgesize);
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int v1 = i * width + j;
                    foreach ((int a, int b, double c) in vertices[i][j])
                    {
                        int v2 = a * width + b;

                        //var edge = (Math.Min(v1, v2), Math.Max(v1, v2));
                        if (v1 < v2)
                        {
                            edges.Add((v1, v2, c));
                        }
                    }
                }

            }


            edges.Sort((a, b) => a.Item3.CompareTo(b.Item3));


            foreach (var (v1, v2, weight) in edges)
            {
                var root1 = find_set(v1);
                var root2 = find_set(v2);

                if (root1 != root2)
                {
                    double t1 = k / componentSize[root1];
                    double t2 = k / componentSize[root2];

                    double int1 = internaldifference[root1];
                    double int2 = internaldifference[root2];

                    if (weight <= Math.Min(int1 + t1, int2 + t2))
                    {
                        union(root1, root2);
                        var newRoot = find_set(root2);
                        componentSize[newRoot] = componentSize[root1] + componentSize[root2];
                        internaldifference[newRoot] = Math.Max(Math.Max(int1, int2), weight);
                    }
                }
            }



            var rootToId = new int[height * width];
            for (int i = 0; i < rootToId.Length; i++)
            {
                rootToId[i] = -1;
            }

            int currentId = 0;
            var pixelToComponent = new int[height * width];

            for (int idx = 0; idx < height * width; idx++)
            {
                int root = find_set(idx);
                if (rootToId[root] == -1)
                {
                    rootToId[root] = currentId++;
                }
                pixelToComponent[idx] = rootToId[root];
            }


            return pixelToComponent;
        }


        private static int find_set(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }
        public static void make_set(int x, int index)
        {
            parent[index] = x;
            rank[index] = 0;
        }
        public static void union(int u, int v)
        {
            if (rank[u] > rank[v])
                parent[v] = u;
            else
            {
                parent[u] = v;
                if (rank[u] == rank[v])
                    rank[v]++;
            }
        }


        public static int[] Split(int[] map, int height, int width)
        {
            int[] result = new int[height * width];
            bool[] Rchecked = new bool[height * width];

            // neighbors
            int[] Rrows = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] Rcols = { -1, 0, 1, -1, 1, -1, 0, 1 };

            int newId = 0;

            Queue<int> q = new Queue<int>();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int idx = i * width + j;
                    if (Rchecked[idx]) continue;

                    int label = map[idx];
                    q.Enqueue(idx);
                    Rchecked[idx] = true;

                    while (q.Count > 0)
                    {
                        int cur = q.Dequeue();
                        int x = cur / width;
                        int y = cur % width;
                        result[cur] = newId;

                        for (int d = 0; d < 8; d++)
                        {
                            int Rx = x + Rrows[d];
                            int Ry = y + Rcols[d];
                            if (Rx < 0 || Ry < 0 || Rx >= height || Ry >= width) continue;

                            int nidx = Rx * width + Ry;
                            if (!Rchecked[nidx] && map[nidx] == label)
                            {
                                q.Enqueue(nidx);
                                Rchecked[nidx] = true;
                            }
                        }
                    }

                    newId++;
                }
            }

            return result;
        }




        public static void WriteSegmentInfoToFile(int[] segments, string path)
        {
            int maxLabel = segments.Max();
            int[] counts = new int[maxLabel + 1];

            foreach (var seg in segments)
            {
                counts[seg]++;
            }

            var sortedCounts = counts.Where(c => c > 0).OrderByDescending(x => x).ToList();

            var w = new StreamWriter(path);
            w.WriteLine(sortedCounts.Count);
            foreach (var c in sortedCounts)
                w.WriteLine(c);
        }

        public static (int[] intersected, int numSegments) IntersectSeg(int[] rLabels, int[] gLabels, int[] bLabels, int width, int height)
        {
            int size = width * height;
            int[] intersects = new int[size];
            var segments = new Dictionary<(int, int, int), int>();
            int segNum = 0;


            for (int i = 0; i < size; i++)
            {
                var key = (rLabels[i], gLabels[i], bLabels[i]);

                if (!segments.ContainsKey(key))
                {
                    segments[key] = segNum++;
                }

                intersects[i] = segments[key];
            }

            return (intersects, segNum);
        }
    }
    public static class Visualize
    {

        public static RGBPixel[,] Coloring(RGBPixel[,] image, int[] segments)
        {
            int height = image.GetLength(0);
            int width = image.GetLength(1);
            var result = new RGBPixel[height, width];
            var segmentColors = new RGBPixel[segments.Max() + 1];
            Random rand = new Random();
            for (int i = 0; i < segmentColors.Length; i++)
            {
                segmentColors[i] = new RGBPixel
                {
                    red = (byte)rand.Next(256),
                    green = (byte)rand.Next(256),
                    blue = (byte)rand.Next(256)
                };
            }

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int Rx = i * width + j;
                    int segId = segments[Rx];
                    result[i, j] = segmentColors[segId];
                }
            }

            return result;
        }

        public static RGBPixel[,] ColoringMerged(
        RGBPixel[,] originalImage,int[] segments, int Root, int width,int height)
        {
            var output = new RGBPixel[height, width];

            
            RGBPixel white = new RGBPixel { red = 255, green = 255, blue = 255 };
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    output[i, j] = white;

            if (Root < 0 || Root >= segments.Length)
                return output;

            int Id = segments[Root];

            for (int Rx = 0; Rx < segments.Length; Rx++)
            {
                if (segments[Rx] == Id)
                {
                    int x = Rx / width;
                    int y = Rx % width;
                    output[x, y] = originalImage[x, y];
                }
            }

            return output;
        }

    }

}