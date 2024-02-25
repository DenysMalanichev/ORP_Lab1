using System.Text;

namespace Lab1;

class DataWorker
{
    static object fileWriteObject = new();

    public static void GenerateData(string fileName)
    {
        using StreamWriter outputFile = new(fileName);
        WriteMatrix("MT", outputFile);

        WriteMatrix("MX", outputFile);

        outputFile.WriteLine("B");
        outputFile.WriteLine(GetStringFromArray(GenerateVector()));

        outputFile.WriteLine("E");
        outputFile.WriteLine(GetStringFromArray(GenerateVector()));
    }

    public static void ReadData(string fileName, out double[,] MT, out double[,] MX, out double[] B, out double[] E)
    {
        using StreamReader readerFile = new(fileName);

        var MTList = new List<double[]>();

        readerFile.ReadLine();
        var currentLine = readerFile.ReadLine();
        while(currentLine is not null && char.IsNumber(currentLine[0]))
        {
            MTList.Add(currentLine.Split(' ').Select(double.Parse).ToArray());
            currentLine = readerFile.ReadLine();
        }
        MT = CreateRectangularArray(MTList);

        var MXList = new List<double[]>();

        currentLine = readerFile.ReadLine();
        while(currentLine is not null && char.IsNumber(currentLine[0]))
        {
            MXList.Add(currentLine.Split(' ').Select(double.Parse).ToArray());
            currentLine = readerFile.ReadLine();
        }
        MX = CreateRectangularArray(MXList);

        B = readerFile.ReadLine()!.Split(' ').Select(double.Parse).ToArray();
        readerFile.ReadLine();

        E = readerFile.ReadLine()!.Split(' ').Select(double.Parse).ToArray();
    }

    public static void WriteResult(string fileName, double[,] matrix, string title)
    {
        lock(fileWriteObject)
        {
            using StreamWriter outputFile = new(fileName, true);
            outputFile.WriteLine(title);
            for (int row = 0; row < matrix.GetLength(0); row++)
            {
                outputFile.WriteLine(GetStringFromArray(ExtractRow(matrix, row)));
            }
        }        
    }

    public static void WriteResult(string fileName, double[] matrix, string title)
    {
        lock(fileWriteObject)
        {
            using StreamWriter outputFile = new(fileName);
            outputFile.WriteLine(title);
            outputFile.WriteLine(GetStringFromArray(matrix));
        }        
    }

    private static string GetStringFromArray(double[] array)
    {
        var line = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            line.Append(array[i]);
            if (i != array.Length - 1)
            {
                line.Append(' ');
            }
        }

        return line.ToString();
    }

    private static double[] GenerateVector()
    {
        var random = new Random();
        var vector = new double[1000];
        for(int i = 0; i < 1000; i++)
        {
            vector[i] = Math.Abs(random.Next(0, int.MaxValue) + random.NextDouble());
        }

        return vector;
    }

    private static double[,] GenerateMatrix()
    {
        var random = new Random();
        var matrix = new double[1000, 1000];
        for(int i = 0; i < 1000; i++)
        {
            for(int j = 0; j < 1000; j++)
            {
                matrix[i, j] = random.NextDouble() / random.Next(1000, 5000);
            }
        }

        return matrix;
    }

    private static double[] ExtractRow(double[,] matrix, int rowToExtract)
    {
        var extractedRow = new double[matrix.GetLength(1)];

        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            extractedRow[i] = matrix[rowToExtract, i];
        }

        return extractedRow;
    }

    private static void WriteMatrix(string name, StreamWriter outputFile)
    {
        outputFile.WriteLine(name);
        var matrix = GenerateMatrix();
        for (int row = 0; row < 1000; row++)
        {
            outputFile.WriteLine(GetStringFromArray(ExtractRow(matrix, row)));
        }
    }

    private static double[,] CreateRectangularArray(IList<double[]> arrays)
    {
        int minorLength = arrays[0].Length;
        double[,] ret = new double[arrays.Count, minorLength];
        for (int i = 0; i < arrays.Count; i++)
        {
            var array = arrays[i];
            if (array.Length != minorLength)
            {
                throw new ArgumentException("All arrays must be the same length");
            }
            for (int j = 0; j < minorLength; j++)
            {
                ret[i, j] = array[j];
            }
        }
        return ret;
    }
}