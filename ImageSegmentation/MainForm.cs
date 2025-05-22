using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;
        RGBPixel[,] OriginalImageMatrix;
        int[] LastSegments;
        List<int> SelectedRoots = new List<int>();

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }


        private async void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            try
            {
                double sigma = double.Parse(txtGaussSigma.Text);
                int maskSize = (int)nudMaskSize.Value;
                int k = (int)Kvalue.Value;
                OriginalImageMatrix = (RGBPixel[,])ImageMatrix.Clone();
                ImageMatrix = await Task.Run(() =>
                    ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma)
                );

                int height = ImageMatrix.GetLength(0);
                int width = ImageMatrix.GetLength(1);
                //------------------------------------
                Stopwatch t = Stopwatch.StartNew();
                var (redGraph, greenGraph, blueGraph) = await Task.Run(() =>
                    Graph2D.Build2DGraph(ImageMatrix)
                );

                var rSegments = await Task.Run(() =>
                    Segmentation.Kruskal(redGraph, ImageMatrix, k)
                );

                var gSegments = await Task.Run(() =>
                    Segmentation.Kruskal(greenGraph, ImageMatrix, k)
                );

                var bSegments = await Task.Run(() =>
                    Segmentation.Kruskal(blueGraph, ImageMatrix, k)
                );
                var (intersected, _) = await Task.Run(() =>
                    Segmentation.IntersectSeg(rSegments, gSegments, bSegments, width, height)
                );
                LastSegments = await Task.Run(() =>
                    Segmentation.Split(intersected, height, width)
                );

                ImageMatrix = await Task.Run(() =>
                    Visualize.Coloring(ImageMatrix, LastSegments)
                );
                //------------------------------------------
                t.Stop();
                textBox1.Text = t.ElapsedMilliseconds.ToString();
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);

                SelectedRoots.Clear();
                string outputPath = @"C:\Users\User\OneDrive\Desktop\myOutput.txt";
                await Task.Run(() =>
                    Segmentation.WriteSegmentInfoToFile(LastSegments, outputPath)
                );


                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "\"bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    pictureBox2.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }


        }

    
        //BONUS
        private void button1_Click(object sender, EventArgs e)
        {
            if (LastSegments == null || SelectedRoots.Count < 1)
            {
                MessageBox.Show("You must first segment the image and pick at least one root.");
                return;
            }

            int targetRoot = SelectedRoots[0]; 
            int targetId = LastSegments[targetRoot];

            int[] mergedMap = (int[])LastSegments.Clone();

            for (int i = 1; i < SelectedRoots.Count; i++)
            {
                int r = SelectedRoots[i];
                int otherId = LastSegments[r];

                for (int idx = 0; idx < mergedMap.Length; idx++)
                {
                    if (mergedMap[idx] == otherId)
                        mergedMap[idx] = targetId;
                }
            }

            int width = ImageMatrix.GetLength(1);
            int height = ImageMatrix.GetLength(0);

            var mergedImage = Visualize.ColoringMerged(OriginalImageMatrix, mergedMap, targetRoot, width, height);
            ImageOperations.DisplayImage(mergedImage, pictureBox2);
        }


        private void pictureBox2_DoubleClick(object sender, MouseEventArgs e)
        {
            if (ImageMatrix == null || LastSegments == null)
                return;

            int imgHeight = ImageMatrix.GetLength(0);
            int imgWidth = ImageMatrix.GetLength(1);
            int picWidth = pictureBox2.Width;
            int picHeight = pictureBox2.Height;

            int x = e.X * imgWidth / picWidth;
            int y = e.Y * imgHeight / picHeight;

            if (x < 0 || x >= imgWidth || y < 0 || y >= imgHeight)
                return;

            int pixelIndex = y * imgWidth + x;

            if (pixelIndex >= 0 && pixelIndex < LastSegments.Length)
            {
                SelectedRoots.Add(pixelIndex);
                int segmentId = LastSegments[pixelIndex];
                MessageBox.Show($"Picked pixel ({x},{y}) in segment {segmentId}");
            }
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
        }

       
        

    }
}
