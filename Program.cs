using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace RadarConverter
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            if (args[0] == "comparecolors")
            {
                var color = Color.FromName(args[1]);
                var indexInt = Convert.ToInt32(args[2]);

                Console.WriteLine($"Unchanged\n R:{color.R} G:{color.G} B:{color.B}");
                Console.WriteLine($"Changed\n R:{Converter.CalculateColorIntensity(color.R, indexInt)} G:{Converter.CalculateColorIntensity(color.G, indexInt)} B:{Converter.CalculateColorIntensity(color.B, indexInt)}");
            }
            else if (args[0] == "update")
            {
                await UpdateChartsAsync();
            }
            else
            {
                var path1 = args[0];
                var path2 = args[1];
                var path3 = args[2];

                var converter = new Converter();
                converter.PrecipTypeMaps = Directory.GetFiles(path1);
                converter.PrecipMaps = Directory.GetFiles(path2);
                converter.OutputPath = path3;

                converter.ProgressChanged += (sender, value) =>
                {
                    Console.WriteLine($"{value.Message}   [{value.Value}%]\r");
                };

                await converter.ProcessAsync();
            }
        }

        private static async Task UpdateChartsAsync()
        {
            using (var httpClient = new HttpClient())
            {
                for (int i = 0; i < 49; i++)
                {
                    var simRadarUrl = $"https://dev.weercijfers.nl/static/harmonie/benelux/simradar_0{FixInt(i)}00.png";
                    await SaveImageAsync(httpClient, simRadarUrl, "simradar");

                    if (i > 0)
                    {
                        var pcpTypeUrl = $"https://dev.weercijfers.nl/static/harmonie/benelux/pcptype_0{FixInt(i)}00.png";
                        await SaveImageAsync(httpClient, pcpTypeUrl, "pcptype", $"pcptype_0{FixInt(i - 1)}00.png");
                    }
                }
            }
        }

        private static async Task SaveImageAsync(HttpClient httpClient, string url, string outputPath, string specialPath = "")
        {
            var outputFile = Path.Combine(outputPath, Path.GetFileName(url));

            if (!string.IsNullOrEmpty(specialPath))
            {
                outputFile = Path.Combine(outputPath, specialPath);
            }

            using (var stream = await httpClient.GetStreamAsync(url))
            using (var image = Image.FromStream(stream))
            {
                image.Save(outputFile);
            }
        }

        private static string FixInt(int value)
        {
            if (value < 10)
            {
                if (value == 0)
                {
                    return "00";
                }

                return $"0{value}";
            }

            return value.ToString();
        }
    }

    public class Converter
    {
        public class Progress
        {
            public string Message { get; set; }
            public double Value { get; set; }

            public Progress(string message, double value)
            {
                Message = message;
                Value = value;
            }
        }

        public event EventHandler<Progress> ProgressChanged;

        public string[] PrecipMaps { get; set; }
        public string[] PrecipTypeMaps { get; set; }
        public string OutputPath { get; set; }

        private static Color[] IntensityColors => new Color[]
        {
                Color.FromArgb(211,211,211),
                Color.FromArgb(181,191,185),
                Color.FromArgb(152,171,159),
                Color.FromArgb(122,151,133),
                Color.FromArgb(93,130,106),
                Color.FromArgb(63,110,80),
                Color.FromArgb(34,90,54),
                Color.FromArgb(4,70,28),
                Color.FromArgb(4,93,28),
                Color.FromArgb(4,117,28),
                Color.FromArgb(4,140,28),
                Color.FromArgb(4,164,28),
                Color.FromArgb(4,187,28),
                Color.FromArgb(4,211,28),
                Color.FromArgb(4,234,28),
                Color.FromArgb(252,238,4),
                Color.FromArgb(250,223,4),
                Color.FromArgb(247,207,4),
                Color.FromArgb(245,192,4),
                Color.FromArgb(243,176,4),
                Color.FromArgb(241,161,4),
                Color.FromArgb(238,145,4),
                Color.FromArgb(236,130,4),
                Color.FromArgb(244,46,4),
                Color.FromArgb(221,37,3),
                Color.FromArgb(198,28,2),
                Color.FromArgb(174,18,2),
                Color.FromArgb(151,9,1),
                Color.FromArgb(128,0,0),
                Color.FromArgb(255,0,255),
                Color.FromArgb(230,26,230),
                Color.FromArgb(204,51,204),
                Color.FromArgb(179,77,179),
                Color.FromArgb(153,102,153),
                Color.FromArgb(128,128,128),
                Color.FromArgb(145,145,145),
                Color.FromArgb(161,161,161),
                Color.FromArgb(178,178,178),
                Color.FromArgb(194,194,194),
        };

        private Color[] RainIntensityColors = new Color[]
        {
            Color.FromArgb(0,87,138),
            Color.FromArgb(0,131,203),
            Color.FromArgb(0,110,170),
            Color.FromArgb(94,167,220),
            Color.FromArgb(148,191,231),
            Color.FromArgb(190, 221, 250),
            Color.FromArgb(227, 241, 255),
            Color.FromArgb(17, 148, 211),
        }.Reverse().ToArray();

        private Color[] SnowIntensityColors = new Color[]
        {
            Color.FromArgb(206, 87, 161),
            Color.FromArgb(215, 129, 182),
            Color.FromArgb(227, 168, 204),
            Color.FromArgb(224, 188, 210),
            Color.FromArgb(240, 223, 233),
        }.Reverse().ToArray();

        private Color[] SleetIntensityColors = new Color[]
        {
            Color.FromArgb(255, 232, 0),
            Color.FromArgb(255, 235, 79),
            Color.FromArgb(255, 240, 131),
            Color.FromArgb(255, 245, 173),
            Color.FromArgb(252, 246, 202),
            Color.FromArgb(255, 253, 235),
        }.Reverse().ToArray();

        private bool HasIntensityColor(Color pixel)
        {
            return IntensityColors.Contains(pixel);
        }

        private int GetIndex(Color pixel)
        {
            if (HasIntensityColor(pixel))
            {
                for (int i = 0; i < IntensityColors.Length; i++)
                {
                    if (IntensityColors[i] == pixel)
                        return i;
                }

                return 0;
            }
            else
            {
                return 0;
            }
        }

        public static int CalculateColorIntensity(int colorValue, int index)
        {
            //int minvalue = ((colorValue / IntensityColors.Length) * index);
            //int newvalue = 75 - minvalue;

            //colorValue = colorValue - newvalue;

            if (colorValue > 0)
            {
                var newColorValue = 255 - colorValue;
                double newIndex = Math.Round((double)index / IntensityColors.Length, 2);

                //Console.WriteLine($"newColorValue = {newColorValue}\nnewIndex = {newIndex}");

                var calc = newColorValue * newIndex;

                //Console.WriteLine(calc.ToString());
                return Convert.ToInt32(255 - calc);
            }

            return 0;
        }

        private Color SetColorIntensity(Color color, int index)
        {
            int r = CalculateColorIntensity(color.R, index);
            int g = CalculateColorIntensity(color.G, index);
            int b = CalculateColorIntensity(color.B, index);

            if (r < 0) r = color.R;
            if (g < 0) g = color.G;
            if (b < 0) b = color.B;

            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;

            return Color.FromArgb(r, g, b);
        }

        private void Merge(string precipMap, string precipTypeMap, int mapIndex)
        {
            var radar = (Bitmap)Image.FromFile(precipMap);
            var type = (Bitmap)Image.FromFile(precipTypeMap);
            var lastTypeColor = Color.White;

            for (int x = 0; x < radar.Width; x++)
                for (int y = 0; y < radar.Height; y++)
                {
                    if (IntensityColors.Contains(radar.GetPixel(x, y)))
                    {
                        var radarPixel = radar.GetPixel(x, y);
                        var typePixel = type.GetPixel(x, y);

                        var index = GetIndex(radarPixel);

                        if (typePixel != Color.White)
                        {
                            if (RainIntensityColors.Contains(typePixel))
                            {
                                lastTypeColor = RainIntensityColors.Last();
                            }

                            if (SleetIntensityColors.Contains(typePixel))
                            {
                                lastTypeColor = SleetIntensityColors.Last();
                            }

                            if (SnowIntensityColors.Contains(typePixel))
                            {
                                lastTypeColor = SnowIntensityColors.Last();
                            }
                        }
                        else
                        {
                            int y2 = y;

                            do
                            {
                                typePixel = type.GetPixel(x, y2);

                                if (typePixel != Color.White)
                                {
                                    if (RainIntensityColors.Contains(typePixel))
                                    {
                                        lastTypeColor = RainIntensityColors.Last();
                                    }

                                    if (SleetIntensityColors.Contains(typePixel))
                                    {
                                        lastTypeColor = SleetIntensityColors.Last();
                                    }

                                    if (SnowIntensityColors.Contains(typePixel))
                                    {
                                        lastTypeColor = SnowIntensityColors.Last();
                                    }

                                    return;
                                }
                                else
                                {
                                    y2++;
                                }
                            } while (y2 < (radar.Height - y));
                        }

                        radar.SetPixel(x, y, SetColorIntensity(lastTypeColor, index));
                    }
                }

            radar.Save(Path.Combine(OutputPath, $"mixradar_{mapIndex}.png"));
        }

        public async Task ProcessAsync()
        {
            for (int i = 0; i < PrecipMaps.Length; i++)
            {
                var map = PrecipMaps[i];
                var typeMap = PrecipTypeMaps[i];

                ProgressChanged?.Invoke(this, new Progress($"[{DateTime.Now}]   Merging {map} and {typeMap}...", Math.Round((double)(i / PrecipMaps.Length) * 100, 1)));
                await Task.Run(() => Merge(typeMap, map, i));
            }

            ProgressChanged?.Invoke(this, new Progress($"[{DateTime.Now}]   Merging complete", 100));
        }
    }
}