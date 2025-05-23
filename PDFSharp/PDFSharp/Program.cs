using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Diagnostics;
using System.IO;
namespace WpfApp3Ds
{
    public class ChineseFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        {
            string fontPath = @"C:\Windows\Fonts\simhei.ttf";
            return new FontResolverInfo(fontPath);
        }

        public byte[] GetFont(string faceName)
        {
            return File.ReadAllBytes(@"C:\Windows\Fonts\simhei.ttf");
        }
    }

    public class Program
    {
        public static void Main()
        {
            GlobalFontSettings.FontResolver = new ChineseFontResolver();

            PdfDocument document = new PdfDocument();
            document.Info.Title = "安防稳定平台数据分析报告";

            PdfPage page = document.AddPage();
            page.Orientation = PageOrientation.Portrait;
            page.Size = PageSize.A4;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont titleFont = new XFont("SimHei", 24);
            XSolidBrush titleBrush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));

            // 绘制标题
            gfx.DrawString("安防稳定平台数据分析报告", titleFont, titleBrush,
                new XRect(0, -300, page.Width.Point, page.Height.Point - 50), XStringFormats.Center);

            XFont tableFont = new XFont("SimHei", 12);
            XSolidBrush tableBrush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));

            string[,] tableData = new string[12, 3]
            {
            { "报告生成日期", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),  DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")},
            { "报告流水号", GenerateRandomSerialNumber(), "RH20250523001" },
            { "船体平均滚转偏移角度", "0.51°", "0.32°" },
            { "船体平均俯仰偏移角度", "0.83°", "0.21°"},
            { "声呐平均滚转偏移角度", "0.42°", "0.13°"},
            { "声呐平均俯仰偏移角度", "0.31°", "0.63°" },
            { "船体最大滚转偏移角度", "2.1°", "1.21°"},
            { "船体最大滚转偏移角度", "1.7°", "1.11°"},
            { "声呐最大俯仰偏移角度", "2.3°", "1.27°"},
            { "声呐最大俯仰偏移角度", "1.3°", "0.21°"},
             { "声呐滚转精度", "1.3°", "1.27°"},
            { "声呐俯仰精度", "1.3°", "1.21°"}
            };

            double[] columnWidths = { 150, 150, 150 };

            double tableX = 40;
            double tableY = 120;
            double tableRowHeight = 25;

            // 绘制表头
            for (int col = 0; col < 3; col++)
            {
                double cellWidth = columnWidths[col];
                double cellHeight = tableRowHeight;

                XRect cellRect = new XRect(tableX + col * cellWidth, tableY, cellWidth, cellHeight);
                gfx.DrawRectangle(XPens.Black, cellRect);

                string headerText = col == 0 ? "检测项" :
                          col == 1 ? "近1分钟" :
                          col == 2 ? "近5分钟" :
                          col == 3 ? "指标" :
                          string.Empty; // 默认情况处理
                gfx.DrawString(headerText, tableFont, tableBrush, cellRect, XStringFormats.Center);
            }

            // 绘制表格内容
            for (int row = 0; row < tableData.GetLength(0); row++)
            {
                tableY += tableRowHeight;

                for (int col = 0; col < tableData.GetLength(1); col++)
                {
                    double cellWidth = columnWidths[col];
                    double cellHeight = tableRowHeight;

                    XRect cellRect = new XRect(tableX + col * cellWidth, tableY, cellWidth, cellHeight);
                    gfx.DrawRectangle(XPens.Black, cellRect);

                    string cellText = tableData[row, col];
                    gfx.DrawString(cellText, tableFont, tableBrush, cellRect, XStringFormats.Center);
                }
            }

            // 添加多个水印
            AddWatermark(gfx, page);
            // 生成包含时间戳的文件名
            string filename = $"安防稳定平台数据分析报告{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf"; // 你可以根据需求
            document.Save(filename);
            // 使用默认浏览器打开文件
            Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        }

        static void AddWatermark(XGraphics gfx, PdfPage page)
        {
            XFont watermarkFont = new XFont("SimHei", 36);
            XSolidBrush watermarkBrush = new XSolidBrush(XColor.FromArgb(20, 0, 0, 255)); // 设置水印颜色为更浅的灰色

            string watermarkText = "安防稳定平台";
            double textWidth = gfx.MeasureString(watermarkText, watermarkFont).Width;
            double textHeight = gfx.MeasureString(watermarkText, watermarkFont).Height;

            double angle = -45; // 设置水印旋转角度

            gfx.Save();
            gfx.RotateTransform(angle); // 先旋转画布，使文本沿对角线排列

            double startX = -page.Width.Point / 2; // 从页面中心偏移开始绘制，确保覆盖整个页面
            double startY = -page.Height.Point / 2;

            double stepX = textWidth * 1.5; // 增大水平步长，避免水印重叠
            double stepY = textHeight * 1.5; // 增大垂直步长，避免水印重叠

            int rows = (int)((page.Height.Point * 1.5) / stepY) + 2;
            int cols = (int)((page.Width.Point * 1.5) / stepX) + 2;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double x = startX + j * stepX;
                    double y = startY + i * stepY;

                    // 确保水印文本在页面范围内
                    if (x + textWidth / Math.Sqrt(2) < page.Width.Point && y + textHeight / Math.Sqrt(2) < page.Height.Point)
                    {
                        gfx.DrawString(watermarkText, watermarkFont, watermarkBrush,
                            new XRect(x, y, textWidth, textHeight), XStringFormats.Center);
                    }
                }
            }

            gfx.Restore();
        }

        static string GenerateRandomSerialNumber()
        {
            Random random = new Random();
            string serialNumber = "RH" + DateTime.Now.ToString("yyyyMMdd") + random.Next(1000, 9999).ToString("D4");
            return serialNumber;
        }
    }
}