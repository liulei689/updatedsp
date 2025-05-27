using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
namespace AFWDPPS.PDF
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
            AddFirstPage(document);
            AddDeclarationPage(document);
            AddDetailPage(document);
            AddReslutPage(document);
            // 生成包含时间戳的文件名
            string filename = $"安防稳定平台数据分析报告{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf"; // 你可以根据需求
            document.Save(filename);
            // 使用默认浏览器打开文件
            Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        }
        // 添加页眉的方法
        private static void AddHeader(PdfPage page, XGraphics gfx)
        {
            // 设置页眉字体
            XFont headerFont = new XFont("SimHei", 10);

            // 页眉内容
            string headerText = "云南安防科技有限公司 - 数据分析报告";

            // 页眉位置和尺寸
            double headerY = 15; // 距离页面顶部的距离
            double headerWidth = page.Width.Point - 40; // 左右边距各20pt
            double headerHeight = 20;

            // 绘制页眉背景（可选）
            XRect headerRect = new XRect(20, headerY, headerWidth, headerHeight);
            gfx.DrawRectangle(new XPen(XColors.LightGray, 0.5), headerRect);

            // 绘制页眉文本 - 居中对齐
            gfx.DrawString(headerText, headerFont, XBrushes.Black,
                new XRect(0, headerY, page.Width.Point, headerHeight),
                XStringFormats.Center);

            // 绘制分隔线
            double lineY = headerY + headerHeight + 5;
            gfx.DrawLine(new XPen(XColors.LightGray, 0.5),
                new XPoint(20, lineY),
                new XPoint(page.Width.Point - 20, lineY));
        }
        // 设置字体和画笔
        public static void AddFirstPage(PdfDocument document)
        {
            PdfPage page = document.AddPage();
            page.Orientation = PageOrientation.Portrait;
            page.Size = PageSize.A4;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            // 设置字体和画笔
            XFont titleFont = new XFont("SimHei", 28); // 标题字体，字号36
            XFont subtitleFont = new XFont("SimHei", 36); // 副标题字体，字号28
            XFont infoFont = new XFont("SimHei", 18); // 信息字体，字号18
            XSolidBrush brush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));

            double margin = 100; // 上下边距

            // 计算文本绘制位置，确保内容居中并有适当的间距
            double titleY = margin;
            double subtitleY = titleY + 90;
            double infoY = subtitleY + 50;

            // 绘制公司名称
            gfx.DrawString("云南安防科技有限公司", titleFont, brush,
                new XRect(0, titleY, page.Width.Point, 0), XStringFormats.Center);

            // 绘制报告标题
            gfx.DrawString("水下稳定平台数据分析报告", subtitleFont, brush,
                new XRect(0, subtitleY, page.Width.Point, 0), XStringFormats.Center);

            // 绘制报告编号
            gfx.DrawString("（编号：CWTC(A)-2024-0603）", infoFont, brush,
                new XRect(0, infoY, page.Width.Point, 0), XStringFormats.Center);

            // 绘制日期
            gfx.DrawString("2024 年 06 月 12 日", infoFont, brush,
                new XRect(0, page.Height.Point - 100, page.Width.Point, 0), XStringFormats.Center);

            // 绘制页脚（页码和发布日期）
            Addfooter(page, gfx, 1, 4);

        }
        public static void AddDetailPage(PdfDocument document)
        {
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
            Addfooter(page, gfx, 3, 4);
        }
        public static void AddDeclarationPage(PdfDocument document)
        {
            // 添加一个A4页面
            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            // 获取图形上下文
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // 设置字体
            XFont titleFont = new XFont("SimHei", 24);
            XFont contentFont = new XFont("SimHei", 16); // 增大正文字体

            // 添加标题
            string title = "有效性声明";
            XSize titleSize = gfx.MeasureString(title, titleFont);
            gfx.DrawString(title, titleFont, XBrushes.Black,
                             new XRect(0, 50, page.Width.Point, titleSize.Height),
                             XStringFormats.Center);

            // 声明内容
            string[] declarationItems = {
        "1.本测试报告完全基于软件自动化分析流程生成，仅适用于报告中明确界定的特定产品、版本及型号，若产品发生任何形式的版本迭代或技术参数调整，本测试结论将不再具备有效性。",
        "2.本测试报告完全基于软件自动化分析流程生成，通过智能化数据处理算法，对被测产品相关数据进行了全面、系统的评估，旨在为相关使用者提供客观、精准的量化分析结果。",
        "3.本报告分析范围严格限定于正文所明示的测试范畴，任何超出此范围的引用或推断均不被认可。",
        "4.严令禁止对本报告内容进行任何形式的部分复制或选择性引用，以防止对被测产品性能产生误导性解读。若需引用报告数据，必须完整保留原始语境与统计口径，确保信息传递的准确性与一致性。",
        "5.本分析报告仅对报告中明确列举的测试项目结果承担准确性责任，所有测试结论均可回溯至自动化测试系统的原始数据记录与运算日志。",
        "6.本测试报告严禁作为商业宣传素材进行复制或改编，不得用于任何形式的市场营销活动。",
        "7.本报告须同时具备系统生成的唯一识别码以及防篡改水印标识方为有效，任何人工修改痕迹未获电子认证授权均视为无效文件。",
        "8.最终解释权归云南安防科技有限公司所有，报告使用者应严格遵循自动化分析系统的数据输出规范进行解读。",
    };

            // 页面边距和布局参数
            double margin = 50;
            double contentWidth = page.Width.Point - 2 * margin;
            double currentY = 100; // 内容起始Y坐标
            double lineHeight = gfx.MeasureString("X", contentFont).Height * 1.5; // 行高
            double paragraphSpacing = lineHeight * 0.5; // 段落间距

            // 预计算字符宽度表，提高性能
            Dictionary<char, double> charWidthCache = new Dictionary<char, double>();

            // 逐段处理
            foreach (string item in declarationItems)
            {
                // 使用智能换行算法，根据文字宽度自动换行
                string formattedText = ForceWrapByWidth(item, contentFont, gfx, contentWidth, charWidthCache);

                // 创建文本区域
                XRect textRect = new XRect(margin, currentY, contentWidth, 1000); // 足够高的临时高度

                // 使用XTextFormatter绘制文本
                XTextFormatter formatter = new XTextFormatter(gfx);
                formatter.Alignment = XParagraphAlignment.Left;
                formatter.DrawString(formattedText, contentFont, XBrushes.Black, textRect);

                // 计算实际占用高度
                int lineCount = formattedText.Count(c => c == '\n') + 1;
                double textHeight = lineCount * lineHeight;

                // 更新Y坐标，添加段落间距
                currentY += textHeight + paragraphSpacing;
            }

            Addfooter(page, gfx, 2, 4);
        }
        public static void AddReslutPage(PdfDocument document)
        {
            // 添加一个A4页面
            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            // 获取图形上下文
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // 设置字体
            XFont titleFont = new XFont("SimHei", 24);
            XFont contentFont = new XFont("SimHei", 16); // 增大正文字体

            // 添加标题
            string title = "检测结果总结";
            XSize titleSize = gfx.MeasureString(title, titleFont);
            gfx.DrawString(title, titleFont, XBrushes.Black,
                             new XRect(0, 50, page.Width.Point, titleSize.Height),
                             XStringFormats.Center);

            //声明内容
            string declarationText =
            "1.本测试报告基于软件自动化分析流程v1.0生成测试内容,涵盖水下稳定平台的关键性能指标。\n\n" +
            "2.测试项目及结果数据如下：\n\n" +
            "    平均滚转偏移角度0.51°实测值0.32°理论值" +
            "平均俯仰偏移角度0.83°实测值0.21°理论值" +
            "最大滚转偏移角度2.1°实测值1.21°理论值" +
            "最大俯仰偏移角度1.7°实测值1.11°理论值" +
            "声呐系统精度指标" +
            "平均滚转偏移角度0.42°实测值0.13°理论值" +
            "平均俯仰偏移角度0.31°实测值0.63°理论值" +
            "最大滚转偏移角度2.3°实测值1.27°理论值" +
            "最大俯仰偏移角度1.3°实测值0.21°理论值" +
            "滚转精度1.3°实测值1.27°理论值" +
            "俯仰精度1.3°实测值1.21°理论值。\n\n" +
            "3.检测结果总结\n\n      经自动化检测系统验证所有检测结果均为无异常符合设计指标要求";

            // 页面边距和布局参数
            double margin = 50;
            double contentWidth = page.Width.Point - 2 * margin;
            double currentY = 100; // 内容起始Y坐标
            double lineHeight = gfx.MeasureString("X", contentFont).Height * 1.5; // 行高
            double paragraphSpacing = lineHeight * 0.5; // 段落间距

            // 预计算字符宽度表，提高性能
            Dictionary<char, double> charWidthCache = new Dictionary<char, double>();

            // 逐段处理
            // 使用智能换行算法，根据文字宽度自动换行
            string formattedText = ForceWrapByWidth(declarationText, contentFont, gfx, contentWidth, charWidthCache);

            // 创建文本区域
            XRect textRect = new XRect(margin, currentY, contentWidth, 1000); // 足够高的临时高度

            // 使用XTextFormatter绘制文本
            XTextFormatter formatter = new XTextFormatter(gfx);
            formatter.Alignment = XParagraphAlignment.Left;
            formatter.DrawString(formattedText, contentFont, XBrushes.Black, textRect);

            // 计算实际占用高度
            int lineCount = formattedText.Count(c => c == '\n') + 1;
            double textHeight = lineCount * lineHeight;

            // 更新Y坐标，添加段落间距
            currentY += textHeight + paragraphSpacing;

            Addfooter(page, gfx, 4, 4);
        }

        // 根据文字宽度自动换行的方法
        // 改进的智能换行算法，支持在任意位置换行
        private static string ForceWrapByWidth(string text, XFont font, XGraphics gfx, double maxWidth, Dictionary<char, double> charWidthCache)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            StringBuilder wrappedText = new StringBuilder();
            double currentLineWidth = 0;
            int currentLineStartIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                double charWidth = GetCharWidth(c, font, gfx, charWidthCache);

                // 如果添加当前字符会超出宽度，则换行
                if (currentLineWidth + charWidth > maxWidth)
                {
                    wrappedText.Append('\n');
                    currentLineWidth = charWidth;
                    currentLineStartIndex = i;
                }
                else
                {
                    currentLineWidth += charWidth;
                }

                wrappedText.Append(c);
            }

            return wrappedText.ToString();
        }

        // 获取字符宽度，使用缓存提高性能
        private static double GetCharWidth(char c, XFont font, XGraphics gfx, Dictionary<char, double> charWidthCache)
        {
            if (!charWidthCache.TryGetValue(c, out double width))
            {
                width = gfx.MeasureString(c.ToString(), font).Width;
                charWidthCache[c] = width;
            }
            return width;
        }

        // 获取文本宽度
        /// </summary>
        public static void Addfooter(PdfPage page, XGraphics gfx, int pageindex, int totalpage)
        {
            XFont pageFont = new XFont("SimSun", 10); // 使用宋体，字号10
            XSolidBrush pageBrush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));
            double pageX = page.Width.Point / 2; // X轴居中位置
            double pageY = page.Height.Point - 20; // 距离底部20点位置

            // 使用XStringFormats.Center确保文本水平居中
            gfx.DrawString($"第{pageindex}/{totalpage}页", pageFont, pageBrush,
                new XRect(pageX, pageY, 0, 0), // 使用0宽高让SizeToFit生效
                XStringFormats.Center);
            AddWatermark(gfx, page);
            AddHeader(page, gfx);
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