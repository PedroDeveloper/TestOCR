using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using System.Windows.Forms;
using System.Drawing.Printing;
using Windows.Media.Ocr;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Data;




namespace TestOCR.Main
{
    public class TestEngine
    {
        
        string pathScreen = @"C:\Users\Pedro\Desktop\testes\screen\";

            public void screenShots()
        {
            Thread trd = new Thread(new ThreadStart ( print)) ;


            trd.Start();


        }

        public void print()
        {

            int j = 0;

            // while (aux != false)
            //{
           // for (int i = 0; i < 10; i++)
            {


                //if (j == 10)
                //{
                //    j = 0;
                //  }

                //  j++;
                
                System.Drawing.Bitmap printscreen = new System.Drawing.Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
                System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(printscreen as System.Drawing.Image);
                graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);

                byte[] byteArray = new byte[0];
                using (MemoryStream stream = new MemoryStream())
                {
                    printscreen.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Close();

                    byteArray = stream.ToArray();
                }

                //take screenshot every 5seg
                Thread.Sleep(5000);

                //open or create file and name as datatime now O.S
                FileStream fs = new FileStream(pathScreen + j + ".png", FileMode.Create);

                fs.Write(byteArray, 0, byteArray.Length);
                fs.Close();

           // }
        }
        }


        public void imageProcess()
        {
            Image<Gray, Byte> imgBi;



            DirectoryInfo d = new DirectoryInfo(@"C:\Users\Pedro\Desktop\testes\screen\");
            FileInfo[] Files = d.GetFiles("*.png");
            foreach (FileInfo file in Files)
            {

                byte[] byteArray = new byte[0];
                using (MemoryStream stream = new MemoryStream())
                {
                    Image<Bgr, Byte> img1 = new Image<Bgr, Byte>(file.FullName);
                    Image<Gray, Byte> grayImage = img1.Convert<Gray, Byte>();
                    imgBi = new Image<Gray, byte>(grayImage.Width, grayImage.Height, new Gray(0));

                    CvInvoke.Threshold(grayImage, imgBi, 50, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

                    imgBi.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    stream.Close();

                    byteArray = stream.ToArray();
                }

                FileStream fs = new FileStream(@"C:\Users\Pedro\Desktop\testes\screnbinary\" + file.Name, FileMode.Create);

                fs.Write(byteArray, 0, byteArray.Length);
                fs.Close();

                // print picture in gray scale
                //picturebox2.image = imgoutput.bitmap;
                //using (memorystream stream = new memorystream()




            }
        }

        public void Process()
        {
            string path = @"C:\Users\Pedro\Desktop\testes\screnbinary\";

            List<string> files;

            List<string> currentRecognicions;
            List<string> currentText;
            string currentFile;
            string currentPlate;

            StringBuilder outCSV;

            try
            {
                outCSV = new StringBuilder();
                outCSV.AppendLine($"PLACA;ENCONTRADO;RECONHECIDO;");

                files = Directory.GetFiles(path).ToList();

                path += "\\Placas.csv";
                files.Remove(path);

                foreach (string file in files)
                {
                    currentRecognicions = ProcessImage(file).Result;
                    currentText = RegexProcess(currentRecognicions);

                    currentFile = Path.GetFileNameWithoutExtension(file);
                    currentPlate = currentFile.Substring(0, 7);

                    outCSV.AppendLine($"{currentFile};{string.Join(" | ", currentText)};{(currentText.Any(x => x.Contains(currentPlate)) ? "SIM" : "NÃO")};");
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.WriteAllText(path, outCSV.ToString());
            }
            catch (Exception ex)
            {
                throw (ex);
            }


            
        }

        public async Task<List<string>> ProcessImage(string image)
        {
            List<string> result = new List<string>();

            BitmapDecoder bmpDecoder;
            SoftwareBitmap softwareBmp;

            OcrEngine ocrEngine;
            OcrResult ocrResult;

            try
            {
                await using (var fileStream = File.OpenRead(image))
                {
                    bmpDecoder = await BitmapDecoder.CreateAsync(fileStream.AsRandomAccessStream());
                    softwareBmp = await bmpDecoder.GetSoftwareBitmapAsync();

                    ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                    ocrResult = await ocrEngine.RecognizeAsync(softwareBmp);

                    foreach (var line in ocrResult.Lines)
                    {
                        result.Add(line.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }

            return result;
        }

        private const string regexPlate = @"([a-zA-Z\d]{3})([a-zA-Z\d]{4})";
        private const string regexClear = @"[^0-9a-zA-Z]+";
        public List<string> RegexProcess(List<string> recognizedValues)
        {
            string processed;
            List<string> result;
            List<string> possibilities;

            try
            {
                result = new List<string>();

                foreach (string value in recognizedValues)
                {
                    processed = Regex.Replace(value, regexClear, "");

                    if (!string.IsNullOrEmpty(processed))
                    {
                        var matches = Regex.Matches(processed, regexPlate);
                        if (matches.Count > 0)
                        {
                            foreach (Match m in matches)
                            {
                                possibilities = GetAlike(m.Value);

                                foreach (string p in possibilities.Distinct())
                                {
                                    result.Add(p);
                                }
                            }
                        }
                    }
                }

                if (result.Count == 0 && recognizedValues.Count > 0)
                {
                    result.Add(string.Join(" | ", recognizedValues));
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }

            return result;
        }


        public List<string> GetAlike(string value)
        {
            List<string> possibilities;
            string begin;
            string end;

            try
            {
                possibilities = new List<string>();
                value = value.ToUpper();

                if (value != null && value.Length >= 7)
                {
                    begin = value.Substring(0, 3);
                    end = value.Replace(begin, "");

                    possibilities.Add(begin + end);

                    begin = begin.Replace("BR", "");
                    begin = begin.Replace("0", "O");
                    begin = begin.Replace("O", "D");
                    begin = begin.Replace("1", "J");
                    begin = begin.Replace("7", "J");
                    begin = begin.Replace("H", "M");
                    begin = begin.Replace("1", "J");

                    end = end.Replace("O", "0");
                    end = end.Replace("D", "0");
                    end = end.Replace("J", "1");
                    end = end.Replace("I", "1");
                    end = end.Replace("B", "8");
                    end = end.Replace("S", "5");
                    end = end.Replace("S", "6");

                    possibilities.Add(begin + end);

                    if (end[1] == '0')
                    {
                        end = end[0] + "O" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                        end = end[0] + "D" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                    }
                    if (end[1] == '1')
                    {
                        end = end[0] + "J" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                        end = end[0] + "I" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                    }
                    if (end[1] == '7')
                    {
                        end = end[0] + "J" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                    }
                    if (end[1] == '8')
                    {
                        end = end[0] + "B" + end.Substring(2, 2);
                        possibilities.Add(begin + end);
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return possibilities;
        }
    }
}
