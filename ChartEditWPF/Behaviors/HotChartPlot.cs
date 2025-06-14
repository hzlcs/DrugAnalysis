using ChartEditLibrary;
using ChartEditLibrary.Entitys;
using ChartEditLibrary.Model;
using ScottPlot;
using ScottPlot.Plottables;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using static ChartEditLibrary.Model.HotChartManager;
using Color = System.Drawing.Color;

namespace ChartEditWPF.Behaviors
{
    internal sealed class HotChartPlot : ScottPlot.WPF.WpfPlot, IDisposable
    {
        static TwoDConfig Config => TwoDConfig.Instance;
        static ScottPlot.Color[][] colors;
        static HotChartPlot()
        {
            ResetColor();
        }

        private HotChartDataDetail detail = null!;
        public HotChartDataDetail Detail
        {
            get => detail;
            set
            {
                detail = value;
                DataChanged(value);
            }
        }

        public (DescData[], Dictionary<string, Dictionary<int, DescData[]>>) Detail2
        {
            set
            {
                DrawCenter(value.Item1);
                DrawDetail(value.Item2);
            }
        }

        public HotChartPlot()
        {
            Plot.Axes.SquareUnits();
            if (!Debugger.IsAttached)
                Plot.Axes.Frameless();
            if (ColorCount != Config.ColorCount)
            {
                ColorCount = Config.ColorCount;
                ResetColor();
            }
            timer = new(200);
            timer.Elapsed += TimerElapsed;
            //timer.Start();


            //markTxt.IsVisible = false;
        }
        private Text markTxt = null!;
        private DateTime moveTime = DateTime.UtcNow;
        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (markTxt.IsVisible)
                return;
            if (DateTime.UtcNow - moveTime > TimeSpan.FromSeconds(0.5))
            {
                var point = Plot.GetCoordinates(new ScottPlot.Pixel((float)mousePoint.X * DisplayScale, (float)mousePoint.Y * DisplayScale));
                var r = point.Distance(default);
                Debug.WriteLine(r);
                if (r <= DetailStart || r >= axe - 1)
                    return;
                var sampleIndex = (int)((r - DetailStart) / DetailInterval);

                var radian = Math.Atan2(point.Y, point.X) + Math.PI;
                var descIndex = (int)(radian / unitRadian);
                var sampleData = sampleDatas.Values.ElementAtOrDefault(descIndex);
                if (sampleData is null)
                    return;
                markTxt.IsVisible = true;
                markTxt.LabelText = sampleData[sampleIndex].value.GetValueOrDefault().ToString("F2");
                markTxt.Location = new Coordinates(point.X, point.Y + 0.05);
                Plot.PlotControl!.Refresh();
            }
        }

        [MemberNotNull(nameof(colors))]
        private static void ResetColor()
        {
            Color[] targetColors = [Color.Green, Color.Blue, Color.Red, Color.Purple, Color.Gold];
            var temp = targetColors.Select(v => GetSingleColorList(Color.White, v, ColorCount + 1)).ToArray();
            colors = temp.Select(v => v.Select(x => Extension.ToScottColor(x)).ToArray()).ToArray();
        }

        private System.Timers.Timer timer;



        private void DataChanged(HotChartDataDetail data)
        {
            DrawData(data);
            markTxt = Plot.Add.Text("init", -2.5, -0.5);
            markTxt.Alignment = Alignment.MiddleCenter;
            markTxt.LabelAlignment = Alignment.MiddleCenter;
            markTxt.LabelFontSize = Config.DescFontSize;
            markTxt.LabelFontName = "Times New Roman";
            markTxt.IsVisible = false;
        }

        System.Windows.Point mousePoint;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            moveTime = DateTime.UtcNow;
            mousePoint = e.GetPosition(this);
            if (markTxt.IsVisible)
            {
                markTxt.IsVisible = false;
                Plot.PlotControl!.Refresh();
            }

        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            timer.Stop();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            timer.Start();

        }

        private static readonly double DetailStart = 2;
        private static readonly double DetailInterval = 0.5;
        private static uint ColorCount = 5;
        private static readonly float lineWidth = 1.1f;
        private static readonly ScottPlot.Color lineColor = Extension.ToScottColor(Color.Black);

        private double axe;
        private void DrawData(HotChartDataDetail detail)
        {

            DrawMain(detail);

        }

        string[] samples = null!;
        Dictionary<string, SampleData[]> sampleDatas = null!;
        double unitRadian;
        static readonly double rotateRadian = Math.PI;
        static readonly int maxCount = 10;

        private void DrawMain(HotChartDataDetail detail)
        {
            double totalRadian = detail.Scale * (1 - Config.Gap * 0.01) * Math.PI * 2;


            //画线
            var datas = detail.Datas.SelectMany(v => v.Value).ToArray();
            var sum = datas.Sum(v => v.Value.GetValueOrDefault());
            var dict = datas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.Sum(x => x.Value.GetValueOrDefault()));
            double currentRadin = rotateRadian;
            double innerR = DetailStart;
            double outerR = DetailStart + DetailInterval * detail.Datas.Count;

            List<SampleData> sampleDatas = [];
            foreach (var kv in detail.Datas)
                foreach (var i in kv.Value)
                    sampleDatas.Add(new SampleData(kv.Key, i.Description, i.Value));
            var sampleGroup = sampleDatas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.ToArray());
            int colorIndex = DescriptionManager.ComDescription.GetDescriptionStart(int.Parse(detail.Description))[0] - 'a';
            var lineColors = colors[colorIndex];
            //lineColors = GetMutiColorList(Color.FromArgb(0, 255, 0), Color.Red, ColorCount);
            double baseValue = sampleDatas.Select(v => v.value.GetValueOrDefault()).Max();
            double x, y;
            samples = detail.Datas.Keys.ToArray();
            this.sampleDatas = sampleGroup;
            unitRadian = 1.0 / dict.Count * totalRadian;
            foreach (var kv in dict)
            {
                //double radian = kv.Value / sum * totalRadian;
                double radian = 1.0 / dict.Count * totalRadian;
                (x, y) = GetSin(currentRadin + radian);
                Angle start = Angle.FromRadians(currentRadin);
                Angle end = Angle.FromRadians(radian);
                var sampleInnerR = DetailStart;
                foreach (var sample in sampleGroup[kv.Key])
                {
                    var rate = sample.value.GetValueOrDefault() / baseValue;
                    int index = (int)Math.Ceiling(rate * ColorCount);
                    var color = lineColors[index];
                    var fill = Plot.Add.AnnularSector(default, sampleInnerR + DetailInterval, sampleInnerR, default, end, start);
                    fill.FillColor = color;
                    fill.LineWidth = 0;

                    sampleInnerR += DetailInterval;

                }
                SetLineStyle(Plot.Add.Line(innerR * x, innerR * y, outerR * x, outerR * y));

                double lableR = outerR + 0.1;
                (x, y) = GetSin(currentRadin + radian / 2);
                var txt = SetDescLabelStyle(Plot.Add.Text(kv.Key, lableR * x, lableR * y));
                txt.LabelRotation = -(float)Angle.FromRadians(currentRadin - rotateRadian + radian / 2).Degrees + 180;


                currentRadin += radian;
            }
            (x, y) = GetSin(rotateRadian);
            SetLineStyle(Plot.Add.Line(innerR * x, innerR * y, outerR * x, outerR * y));

            double r = DetailStart - DetailInterval;
            //画圆
            if (Utility.ToleranceEqual(totalRadian, Math.PI * 2))
            {
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}", 0, 0);
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelAlignment = Alignment.MiddleCenter;
                txt.LabelFontSize = (int)(18 * (1 - (detail.Datas.Count / maxCount) * 0.2));
                SetLineStyle(Plot.Add.Circle(default, 1));
                foreach (var key in detail.Datas.Keys.Prepend(""))
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Circle(default, r));
                    SetSampleLabelStyle(Plot.Add.Text(key, -(r - DetailInterval * 0.5), 0)).LabelRotation = 90;
                }
            }
            else
            {
                Angle start = Angle.FromRadians(0);
                Angle end = Angle.FromRadians(totalRadian);
                Angle rotate = Angle.FromDegrees(180);
                SetLineStyle(Plot.Add.AnnularSector(default, 1, 0, start, end, rotate));
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}",
                    0.5 * Math.Cos(totalRadian / 2 + rotateRadian), 0.5 * Math.Sin(totalRadian / 2 + rotateRadian));
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelAlignment = Alignment.MiddleCenter;
                txt.LabelFontSize = (int)(18 * (1 - (detail.Datas.Count / maxCount) * 0.2));
                foreach (var key in detail.Datas.Keys.Prepend(""))
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Arc(default, r, rotate, end));
                    SetSampleLabelStyle(Plot.Add.Text(key, -(r - DetailInterval * 0.5), 0.05)).LabelRotation = 270;
                }
            }
            axe = r + 1 + (detail.Datas.Count / maxCount * DetailInterval);
            //ColorBarOut(lineColors);
            ColorBarRound(lineColors, totalRadian);
            AutoScale();
        }

        private void ColorBarRound(ScottPlot.Color[] lineColors, double totalRadian)
        {
            double rate = 1.0 / lineColors.Length;
            double currentRadian = rotateRadian;
            double radian = totalRadian * rate;
            Angle end = Angle.FromRadians(radian);
            double innerR = 1;
            double outerR = 1 + 0.5;
            foreach (var color in lineColors)
            {
                Angle rotate = Angle.FromRadians(currentRadian);
                var fill = Plot.Add.AnnularSector(default, outerR, innerR, default, end, rotate);
                fill.FillColor = color;
                fill.LineWidth = 0;
                currentRadian += radian;
            }
            (var x, var y) = GetSin(rotateRadian + radian / 2);
            var txt = Plot.Add.Text("0", outerR * x, outerR * y);
            SetDescLabelStyle(txt);
            txt.LabelRotation = -(float)Angle.FromRadians(0 + radian / 2).Degrees + 180;

            (x, y) = GetSin(rotateRadian + totalRadian - radian / 2);
            txt = Plot.Add.Text("1", outerR * x, outerR * y);
            SetDescLabelStyle(txt);
            txt.LabelRotation = -(float)Angle.FromRadians(totalRadian - radian / 2 + radian / 2).Degrees + 180;
        }

        private void ColorBarOut(ScottPlot.Color[] lineColors)
        {
            float length = 3.09f;
            float a = length / (lineColors.Length - 1);
            float lineY = -(float)(axe - DetailInterval * 0.5);
            int barIndex = 0;
            float degree = 1.0f / (lineColors.Length - 1);
            float startX = -length / 2;
            foreach (var color in lineColors)
            {
                var t = Plot.Add.Rectangle(startX, startX + a, lineY, lineY - a);
                t.FillColor = color;
                t.LineWidth = 0;
                t.LineColor = default;
                if (barIndex % 2 == 0)
                {
                    var txt = Plot.Add.Text((degree * barIndex).ToString("0.#"), startX + a / 2, lineY + a / 2);
                    txt.Alignment = Alignment.MiddleCenter;
                    txt.LabelFontSize = 16;
                }
                startX += a;
                ++barIndex;
            }
        }

        private static (double x, double y) GetSin(double radian)
        {
            return (Math.Cos(radian), Math.Sin(radian));
        }

        private void DrawMain_backup(HotChartDataDetail detail)
        {
            double totalRadian = detail.Scale;


            //画线
            var datas = detail.Datas.SelectMany(v => v.Value).ToArray();
            var sum = datas.Sum(v => v.Value.GetValueOrDefault());
            var dict = datas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.Sum(x => x.Value.GetValueOrDefault()));
            double currentRadin = 0;
            double innerR = DetailStart;
            double outerR = DetailStart + DetailInterval * detail.Datas.Count;

            List<SampleData> sampleDatas = [];
            foreach (var kv in detail.Datas)
                foreach (var i in kv.Value)
                    sampleDatas.Add(new SampleData(kv.Key, i.Description, i.Value));
            var sampleGroup = sampleDatas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.ToArray());
            int colorIndex = DescriptionManager.ComDescription.GetDescriptionStart(int.Parse(detail.Description))[0] - 'a';
            var lineColors = colors[colorIndex];
            double baseValue = sampleDatas.Select(v => v.value.GetValueOrDefault()).Max();

            foreach (var kv in dict)
            {
                double radin = kv.Value / sum * Math.PI * 2 * totalRadian;
                radin = 1.0 / dict.Count * Math.PI * 2 * totalRadian;
                var x = Math.Cos(currentRadin + radin);
                var y = Math.Sin(currentRadin + radin);
                Angle start = Angle.FromRadians(currentRadin);
                Angle end = Angle.FromRadians(radin);
                var sampleInnerR = DetailStart;
                foreach (var sample in sampleGroup[kv.Key])
                {
                    var rate = sample.value.GetValueOrDefault() / baseValue;
                    int index = (int)Math.Ceiling(rate * ColorCount);
                    var color = lineColors[index];
                    var fill = Plot.Add.AnnularSector(default, sampleInnerR + DetailInterval, sampleInnerR, default, end, start);
                    fill.FillColor = color;
                    fill.LineWidth = 0;
                    sampleInnerR += DetailInterval;
                }
                SetLineStyle(Plot.Add.Line(innerR * x, innerR * y, outerR * x, outerR * y));
                currentRadin += radin;
            }
            SetLineStyle(Plot.Add.Line(innerR, 0, outerR, 0));

            double r = DetailStart - DetailInterval;
            //画圆
            if (Utility.ToleranceEqual(totalRadian, 1))
            {
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}", 0, 0);
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelFontSize = 18;
                SetLineStyle(Plot.Add.Circle(default, 1));
                for (int i = 0; i <= detail.Datas.Count; ++i)
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Circle(default, r));
                }
            }
            else
            {
                Angle start = Angle.FromRadians(0);
                Angle end = Angle.FromRadians(totalRadian * Math.PI * 2);
                SetLineStyle(Plot.Add.AnnularSector(default, 1, 0, start, end));
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}", 0.5 * Math.Cos(totalRadian * Math.PI), 0.5 * Math.Sin(totalRadian * Math.PI));
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelFontSize = 18;
                for (int i = 0; i <= detail.Datas.Count; ++i)
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Arc(default, r, start, end));
                }
            }
            r += 0.5;
            //Plot.Axes.Left.Max = r;
            //Plot.Axes.Left.Min = -r;
            Plot.Axes.SetLimits(-r, r, -r, r);
        }

        private static void SetLineStyle(LinePlot line)
        {
            line.Color = lineColor;
            line.LineWidth = lineWidth;
        }

        private static void SetLineStyle(Ellipse ellipse)
        {
            ellipse.LineColor = lineColor;
            ellipse.LineWidth = lineWidth;
        }

        private static Text SetDescLabelStyle(Text text)
        {
            text.Alignment = Alignment.MiddleLeft;
            text.LabelAlignment = Alignment.MiddleLeft;
            text.LabelFontSize = Config.DescFontSize;
            text.LabelFontName = "Times New Roman";
            return text;
        }

        private static Text SetSampleLabelStyle(Text text)
        {
            text.Alignment = Alignment.MiddleLeft;
            text.LabelAlignment = Alignment.MiddleLeft;
            text.LabelFontSize = Config.SampleFontSize;
            text.LabelFontName = "Times New Roman";
            return text;
        }

        record SampleData(string SampleName, string Description, double? value);

        private static Color[] GetSingleColorList(Color srcColor, Color desColor, uint count)
        {
            List<Color> colorFactorList = new List<Color>();
            int redSpan = desColor.R - srcColor.R;
            int greenSpan = desColor.G - srcColor.G;
            int blueSpan = desColor.B - srcColor.B;
            for (int i = 0; i < count; i++)
            {
                Color color = Color.FromArgb(255,
                    srcColor.R + (int)((double)i / count * redSpan),
                    srcColor.G + (int)((double)i / count * greenSpan),
                    srcColor.B + (int)((double)i / count * blueSpan)
                );
                colorFactorList.Add(color);
            }
            return colorFactorList.ToArray();
        }

        private static ScottPlot.Color[] GetMutiColorList(Color start, Color end, uint count)
        {
            Color[] colors = new Color[count * 2 + 1];
            var startColors = GetSingleColorList(start, Color.White, count + 1);
            var endColors = GetSingleColorList(Color.White, end, count + 2);
            Array.Copy(startColors, 0, colors, 0, count);
            Array.Copy(endColors, 1, colors, count, count + 1);
            return colors.Select(v => Extension.ToScottColor(v)).ToArray();
        }
        #region Obsolete
        readonly Dictionary<char, double> centerRadin = [];
        private void DrawCenter(DescData[] datas)
        {
            Array.Sort(datas, MainComparison);
            double sum = datas.Select(v => v.Value.GetValueOrDefault()).Sum();
            Plot.Add.Circle(0, 0, 1);
            Coordinates start = new Coordinates(0, 0);
            double currentRadin = 0;
            foreach (var degree in datas)
            {
                var radin = degree.Value.GetValueOrDefault() / sum * Math.PI * 2;
                var d = DescriptionManager.ComDescription.GetDescriptionStart(int.Parse(degree.Description))[0];
                centerRadin[d] = radin;
                Coordinates end = new Coordinates(Math.Cos(currentRadin + radin), Math.Sin(currentRadin + radin));
                Plot.Add.Line(start.X, start.Y, end.X, end.Y);
                Coordinates labelEnd = new Coordinates(Math.Cos(currentRadin + radin / 2) * 0.5, Math.Sin(currentRadin + radin / 2) * 0.5);
                Plot.Add.Text("dp" + degree.Description, labelEnd.X, labelEnd.Y);
                currentRadin += radin;
            }
        }


        private void DrawDetail(Dictionary<string, Dictionary<int, DescData[]>> datas)
        {

            var allDatas = datas.SelectMany(v => v.Value).SelectMany(v => v.Value).GroupBy(v => v.Description[0])
                .ToDictionary(v => v.Key, v => v.ToArray());
            double curRadin = 0;
            double innerRadiu = DetailStart;
            double outerRadiu = DetailStart + DetailInterval * datas.Count;
            foreach (var kv in allDatas)
            {
                char d = kv.Key;
                var values = kv.Value;
                Array.Sort(values, DescriptionComparer);
                var sum = values.Sum(v => v.Value.GetValueOrDefault());
                var color = colors[d - 'a'][5];
                foreach (var data in values)
                {
                    double radin = data.Value.GetValueOrDefault() / sum * centerRadin[d];
                    radin = 1.0 / values.Length * centerRadin[d];
                    curRadin += radin;
                    double x = Math.Cos(curRadin);
                    double y = Math.Sin(curRadin);
                    Coordinates start = new Coordinates(x * innerRadiu, y * innerRadiu);
                    Coordinates end = new Coordinates(x * outerRadiu, y * outerRadiu);
                    Plot.Add.Line(start, end).Color = color;
                }
            }


            Plot.Add.Circle(0, 0, innerRadiu);
            double radiu = innerRadiu;
            foreach (var kv in datas)
            {
                radiu += DetailInterval;
                Plot.Add.Circle(0, 0, radiu);
            }


        }
        #endregion
        static IComparer<DescData> DescriptionComparer = Comparer<DescData>.Create(DescriptionComparison);

        private static int DescriptionComparison(DescData x, DescData y)
        {
            if (x.Description[0] != y.Description[0])
                return x.Description[0] - y.Description[0];
            return int.Parse(x.Description[1..]) - int.Parse(y.Description[1..]);
        }

        private static int MainComparison(DescData x, DescData y)
        {
            return int.Parse(x.Description) - int.Parse(y.Description);
        }

        internal void AutoScale()
        {
            Plot.Axes.SetLimits(-axe, axe, -axe, axe);
            Plot.PlotControl!.Refresh();
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }

}
