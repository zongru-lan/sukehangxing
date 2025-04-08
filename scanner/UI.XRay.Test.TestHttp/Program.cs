using System;
using System.Drawing;
using ImageMagick;

public class MagickNetExample
{
    public static ushort[,] ReadTiffAsUShortArray(string filePath)
    {
        // 创建16位量化的MagickImage实例
        using (MagickImage image = new MagickImage(filePath))
        {
            // 获取图像尺寸
            int width = (int)image.Width;  // 显式转换uint到int
            int height = (int)image.Height; // 显式转换uint到int
            Console.WriteLine($"图像尺寸: {width}x{height}");

            // 检查色彩空间
            if (image.ColorSpace != ColorSpace.Gray)
            {
                Console.WriteLine($"警告: 图像不是灰度图 (ColorSpace: {image.ColorSpace})");
                // 转换为灰度
                image.ColorSpace = ColorSpace.Gray;
            }

            // 检查位深度
            int depth = (int)image.Depth;  // 显式转换uint到int
            Console.WriteLine($"位深度: {depth}位");

            // 创建结果数组
            ushort[,] result = new ushort[height, width];

            // 获取像素数据 - 使用Q16格式确保16位精度
            // 修正泛型参数问题
            using (IPixelCollection<ushort> pixels = image.GetPixels())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // 获取像素值 - 对于灰度图只有一个通道
                        var pixel = pixels.GetPixel(x, y);

                        // 获取第一个通道的值 (R=G=B 对于灰度图)
                        ushort pixelValue = pixel.GetChannel(0);
                        result[y, x] = pixelValue;
                    }
                }
            }

            Console.WriteLine("成功读取图像数据");
            return result;
        }
    }

    public static int FindLeftQuyu(ushort material_num_temp, int[] huidu)
    {
        for (int i = huidu.Length - 1; i >= 0; i--)    //找最大的
            if (material_num_temp >= huidu[i])         //先找出所有大于等于huidu的下标  再取出最大的那个
                return i;
        return -1;
    }

    public static int FindRightQuyu(ushort material_num_temp, int[] huidu)
    {
        for (int i = 0; i < huidu.Length; i++)      //找最小的
            if (material_num_temp <= huidu[i])      //先找出所有小于等于huidu的下标  再取出最小的那个
                return i;
        return -1;
    }

    public static double[,] GenerateAtom(ushort[,] material_num, int height, int width)
    {
        int Height = height;
        int Width = width;
        double[] yuanzixushu = { 5.53, 7.51, 8.85, 9.45, 10, 13, 15.62, 16.8, 18.12, 19.68, 20, 26 };
        int[] huidu = { 5, 19, 45, 60, 85, 115, 128, 140, 165, 190, 215, 240 }; //  可能要变
        double[,] cailiaozhi_num = new double[Height, Width];

        for (int i = 0; i < Height; i++)
        {
            for (int j = 0; j < Width; j++)
            {
                ushort material_num_temp = material_num[i, j];
                int left_quyu = FindLeftQuyu(material_num_temp, huidu);
                int right_quyu = FindRightQuyu(material_num_temp, huidu);

                double yuanzixushu_temp;

                if (material_num_temp == 1 || material_num_temp == 2)
                    yuanzixushu_temp = 27;
                else if (material_num_temp == 50)
                    yuanzixushu_temp = 0;
                else if (left_quyu == -1)
                    yuanzixushu_temp = 5.53;  // 当小于最小灰度值时，应当用最小原子序数
                else if (right_quyu == -1)
                    yuanzixushu_temp = 26;    // 当大于最大灰度值时，应当用最大原子序数
                else
                {
                    double left_xishu, right_xishu;

                    if (left_quyu == right_quyu)
                    {
                        left_xishu = 1;
                        right_xishu = 0;
                    }
                    else
                    {
                        // 计算分母只做一次
                        double denominator = huidu[right_quyu] - huidu[left_quyu];
                        left_xishu = (huidu[right_quyu] - material_num_temp) / denominator;
                        // 避免再次计算
                        right_xishu = 1.000 - left_xishu; 
                    }
                    yuanzixushu_temp = yuanzixushu[left_quyu] * left_xishu + yuanzixushu[right_quyu] * right_xishu;
                    // 再精确到特定位数
                    //yuanzixushu_temp = Math.Round(yuanzixushu_temp, 4); // 保留4位小数
                }
                cailiaozhi_num[i, j] = yuanzixushu_temp;
            }
        }
        return cailiaozhi_num;
    }




    /// <summary>
    /// 将double数组保存为符合特定参数要求的16位TIFF图像
    /// </summary>
    /// <param name="doubleArray">输入的double数组</param>
    /// <param name="outputPath">输出的TIFF文件路径或目录</param>
    /// <param name="saveMetadata">是否保存元数据文件（默认保存）</param>
    /// <returns>转换是否成功</returns>
    public static bool SaveDoubleArrayToTiff(double[,] doubleArray, string outputPath, bool saveMetadata = true)
    {
        try
        {
            // 检查并调整输出路径
            if (Directory.Exists(outputPath) || (!File.Exists(outputPath) && !Path.HasExtension(outputPath)))
            {
                // 输入的是目录路径，添加默认文件名
                outputPath = Path.Combine(outputPath, $"atomic_result_{DateTime.Now:yyyyMMdd_HHmmss}.tiff");
                Console.WriteLine($"输出路径是目录，已自动生成文件名: {outputPath}");
            }

            // 获取数组尺寸
            int height = doubleArray.GetLength(0);
            int width = doubleArray.GetLength(1);

            // 计算统计数据
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            double sum = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double value = doubleArray[y, x];
                    sum += value;
                    if (value < minValue) minValue = value;
                    if (value > maxValue) maxValue = value;
                }
            }

            double average = sum / (width * height);
            Console.WriteLine($"数据统计: 最小值={minValue:F2}, 最大值={maxValue:F2}, 平均值={average:F2}");

            // 检查是否有值超出ushort范围
            if (maxValue > ushort.MaxValue)
            {
                Console.WriteLine($"警告: 数据中存在超过ushort最大值(65535)的值，这些值将被截断");
            }
            if (minValue < 0)
            {
                Console.WriteLine($"警告: 数据中存在负值，这些值将被设置为0");
            }

            // 转换为ushort数组（直接使用四舍五入）
            ushort[,] ushortArray = new ushort[height, width];

            Console.WriteLine("使用直接值转换（四舍五入）将double值转换为ushort");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 四舍五入为最接近的整数，并确保在ushort范围内
                    double roundedValue = Math.Round(doubleArray[y, x]);
                    roundedValue = Math.Max(0, Math.Min(roundedValue, ushort.MaxValue));
                    ushortArray[y, x] = (ushort)roundedValue;
                }
            }

            // 确保输出目录存在
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine($"创建输出目录: {directory}");
                    Directory.CreateDirectory(directory);
                }

                // 测试目录写入权限
                try
                {
                    string testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFile, "Test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: 目录 {directory} 可能没有写入权限: {ex.Message}");
                    // 尝试使用用户目录
                    string userDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fileName = Path.GetFileName(outputPath);
                    outputPath = Path.Combine(userDir, fileName);
                    Console.WriteLine($"尝试使用备用路径: {outputPath}");
                }
            }

            Console.WriteLine($"准备保存图像到: {outputPath}");

            // 创建新的MagickImage并填充数据
            using (MagickImage image = new MagickImage(new MagickColor("black"), (uint)width, (uint)height))
            {
                // 设置图像格式为TIFF
                image.Format = MagickFormat.Tiff;

                // 设置基本参数 - 匹配目标要求
                image.ColorSpace = ColorSpace.Gray;  // 灰度模式
                image.Depth = 16;                   // 16位深度

                // 设置分辨率为72dpi
                image.Density = new Density(72, 72, DensityUnit.PixelsPerInch);

                // 获取像素集合并填充
                using (IPixelCollection<ushort> pixels = image.GetPixels())
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var pixel = pixels.GetPixel(x, y);
                            pixel.SetChannel(0, ushortArray[y, x]); // 设置灰度值
                            pixels.SetPixel(pixel);
                        }
                    }
                }

                // 设置TIFF特定选项，完全匹配要求
                // 小端序
                image.Settings.SetDefine(MagickFormat.Tiff, "endian", "lsb");

                // PackBits压缩
                image.Settings.SetDefine(MagickFormat.Tiff, "compression", "PackBits");

                // 设置每条带行数为18
                image.Settings.SetDefine(MagickFormat.Tiff, "rows-per-strip", "18");

                // 确保使用Chunky配置（像素交错）
                image.Settings.SetDefine(MagickFormat.Tiff, "planar-configuration", "contig");

                // 设置光度解释为BlackIsZero
                image.Settings.SetDefine(MagickFormat.Tiff, "photometric", "minisblack");

                // 保存图像
                image.Write(outputPath);
                Console.WriteLine($"已成功保存TIFF图像: {outputPath}");
            }

            // 保存元数据（可选）
            if (saveMetadata)
            {
                string metadataPath = Path.ChangeExtension(outputPath, ".meta.txt");
                using (StreamWriter writer = new StreamWriter(metadataPath))
                {
                    writer.WriteLine($"# 元数据 - {Path.GetFileName(outputPath)}");
                    writer.WriteLine($"生成时间: {DateTime.Now}");
                    writer.WriteLine($"尺寸: {width} x {height}");
                    writer.WriteLine($"格式: TIFF");
                    writer.WriteLine($"位深度: 16位");
                    writer.WriteLine($"颜色类型: 灰度");
                    writer.WriteLine($"字节序: 小端序(little-endian)");
                    writer.WriteLine($"压缩方式: PackBits");
                    writer.WriteLine($"像素排列: Chunky");
                    writer.WriteLine($"每条带行数: 18");
                    writer.WriteLine($"分辨率: 72 dpi");
                    writer.WriteLine($"分辨率单位: 英寸");
                    writer.WriteLine();
                    writer.WriteLine($"转换方法: 直接值转换（四舍五入）");
                    writer.WriteLine($"像素值 = 原始值的四舍五入整数");
                    writer.WriteLine($"原始数据范围: {minValue:F6} 到 {maxValue:F6}");
                    writer.WriteLine($"原始数据平均值: {average:F6}");
                }
                Console.WriteLine($"已保存元数据文件: {metadataPath}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"转换失败: {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            return false;
        }
    }





    // 程序入口点
    public static void Main(string[] args)
    {
        // 显示标题
        Console.WriteLine("16位TIFF图像读取器");
        Console.WriteLine("==================");

        try
        {
            // 获取文件路径 - 可以从命令行参数获取或者提示用户输入
            string tiffPath;

            if (args.Length > 0)
            {
                tiffPath = args[0];
            }
            else
            {
                Console.Write("请输入TIFF文件路径: ");
                tiffPath = Console.ReadLine();

                // 检查路径是否为空
                if (string.IsNullOrWhiteSpace(tiffPath))
                {
                    Console.WriteLine("未提供文件路径，使用默认测试路径");
                    tiffPath = "E:\\desktop\\Mcode\\matOrM.tiff"; // 使用您图像的实际路径
                }
            }

            // 检查文件是否存在
            if (!System.IO.File.Exists(tiffPath))
            {
                Console.WriteLine($"错误: 文件不存在 - {tiffPath}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 读取图像
            Console.WriteLine($"正在读取文件: {tiffPath}");
            ushort[,] imageData = ReadTiffAsUShortArray(tiffPath);

            // 打印基本统计信息
            int height = imageData.GetLength(0);
            int width = imageData.GetLength(1);

            ulong sum = 0;
            ushort min = ushort.MaxValue;
            ushort max = ushort.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ushort value = imageData[y, x];
                    sum += value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            double average = (double)sum / (width * height);
            Console.WriteLine($"成功读取图像: {width}x{height}");
            Console.WriteLine($"像素值统计: 最小值={min}, 最大值={max}, 平均值={average:F2}");

            //嵌入 GenerateAtom
            double[,] cailiaozhi_num = GenerateAtom(imageData, height, width);

            //生成tiff图像
            bool Ifsuccess = SaveDoubleArrayToTiff(cailiaozhi_num, "E:\\desktop\\Mcode\\result.tiff");
            if (Ifsuccess)
                Console.WriteLine("处理完成!");
            else
                Console.WriteLine("失败中的失败!!!GameOver");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理TIFF图像时出错: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        // 等待用户按键退出
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}