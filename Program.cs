using System.Diagnostics;
using System.Text;

namespace Lab1;
class Program
{
    static readonly string DataFileName = "Data.txt";
    static readonly string ResultsFileName = "Results.txt";

    static object printResultLock = new(); 

    static void Main()
    {
        if(!File.Exists(DataFileName))
        {
            DataWorker.GenerateData(DataFileName);
        }

        DataWorker.ReadData(DataFileName, out double[,] MT, out double[,] MX, out double[] B, out double[] E);

        var MDTask = new Task(() => 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double[,] MD = CountMD(MT, MX);
            stopwatch.Stop();

            long MDTime = stopwatch.ElapsedMilliseconds;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"MD was calculated in {MDTime} ms.");
            Console.ResetColor();            
            PrintMatrix(MD);

            DataWorker.WriteResult(ResultsFileName, MD, $"MD is calculated in {MDTime}. Result: ");
        });

        MDTask.Start();

        var MDKahanTask = new Task(() => 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double[,] MDKahan = CountMDKahan(MT, MX);
            stopwatch.Stop();

            long MDKahanTime = stopwatch.ElapsedMilliseconds;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"MD by Kahan was calculated in {MDKahanTime} ms.");
            Console.ResetColor();
            PrintMatrix(MDKahan);

            DataWorker.WriteResult(ResultsFileName, MDKahan, $"MD by Kahan method is calculated in {MDKahanTime}. Result: ");
        });

        MDKahanTask.Start();

        Task.WaitAll(MDTask, MDKahanTask);

        Console.WriteLine("--- End of MD matrix calculations ---");

        var DTask = new Task(() => 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double[] D = CountD(MT, MX, B, E);
            stopwatch.Stop();

            long DTime = stopwatch.ElapsedMilliseconds;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"D was calculated in {DTime} ms.");
            Console.ResetColor();
            PrintVector(D);

            DataWorker.WriteResult(ResultsFileName, D, $"D was calculated in {DTime}. Result: ");
        });
        DTask.Start();

        var DKahanTask = new Task(() => 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double[] D = CountDKahan(MT, MX, B, E);
            stopwatch.Stop();

            long DTime = stopwatch.ElapsedMilliseconds;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"D by Kahan was calculated in {DTime} ms.");
            Console.ResetColor();
            PrintVector(D);

            DataWorker.WriteResult(ResultsFileName, D, $"D by Kahan was calculated in {DTime}. Result: ");
        });
        DKahanTask.Start();

        Task.WaitAll(DTask, DKahanTask);
    }

    public static double[,] CountMD(double[,] MT, double[,] MX)
    {
        double min = 0;
        var findMinTask = new Task(() => min = Min(MT));
        findMinTask.Start();

        double[,] mTmXSum = {};
        var findSumTask = new Task(() => mTmXSum = Sum(MT, MX));
        findSumTask.Start();

        double[,] mTmXMult = {};
        var multiplyTask = new Task(() => mTmXMult = Multiply(MT, MX));
        multiplyTask.Start();

        Task.WaitAll(findMinTask, findSumTask, multiplyTask);

        return Substitude(Multiply(min, mTmXSum), mTmXMult);
    }

    public static double[,] CountMDKahan(double[,] MT, double[,] MX)
    {
        double min = 0;
        var findMinTask = new Task(() => min = Min(MT));
        findMinTask.Start();

        double[,] mTmXSum = {};
        var findSumTask = new Task(() => mTmXSum = Sum(MT, MX));
        findSumTask.Start();

        double[,] mTmXMult = {};
        var multiplyTask = new Task(() => mTmXMult = KahanMultiply(MT, MX));
        multiplyTask.Start();

        Task.WaitAll(findMinTask, findSumTask, multiplyTask);

        return Substitude(Multiply(min, mTmXSum), mTmXMult);   
    }

    public static double[] CountD(double[,] MT, double[,] MX, double[] B, double[] E)
    {
        double[,] mTmXSum = {};
        var findSumTask = new Task(() => mTmXSum = Sum(MT, MX));
        findSumTask.Start();

        double[,] mTMinusMX = {};
        var findMtMinusMxTask = new Task(() => mTMinusMX = Minus(MT, MX));
        findMtMinusMxTask.Start();

        Task.WaitAll(findSumTask, findMtMinusMxTask);

        double[] bMultMtMxSum = {};
        var findBMultMtMxSumTask = new Task(() => bMultMtMxSum = Multiply(B, mTmXSum));
        findBMultMtMxSumTask.Start();

        double[] eMultMtMinusMx = {};
        var findEMultMtMinusMxTask = new Task(() => eMultMtMinusMx = Multiply(E, mTmXSum));
        findEMultMtMinusMxTask.Start();

        Task.WaitAll(findBMultMtMxSumTask, findEMultMtMinusMxTask);

        return Substitude(bMultMtMxSum, eMultMtMinusMx);
    }

    public static double[] CountDKahan(double[,] MT, double[,] MX, double[] B, double[] E)
    {
        double[,] mTmXSum = {};
        var findSumTask = new Task(() => mTmXSum = Sum(MT, MX));
        findSumTask.Start();

        double[,] mTMinusMX = {};
        var findMtMinusMxTask = new Task(() => mTMinusMX = Minus(MT, MX));
        findMtMinusMxTask.Start();

        Task.WaitAll(findSumTask, findMtMinusMxTask);

        double[] bMultMtMxSum = {};
        var findBMultMtMxSumTask = new Task(() => bMultMtMxSum = MultiplyKahan(B, mTmXSum));
        findBMultMtMxSumTask.Start();

        double[] eMultMtMinusMx = {};
        var findEMultMtMinusMxTask = new Task(() => eMultMtMinusMx = MultiplyKahan(E, mTmXSum));
        findEMultMtMinusMxTask.Start();

        Task.WaitAll(findBMultMtMxSumTask, findEMultMtMinusMxTask);

        return Substitude(bMultMtMxSum, eMultMtMinusMx);
    }

    private static double[,] KahanMultiply(double[,] matrixA, double[,] matrixB)
    {
        int aRows = matrixA.GetLength(0);
        int aCols = matrixA.GetLength(1);
        int bRows = matrixB.GetLength(0);
        int bCols = matrixB.GetLength(1);

        if (aCols != bRows)
        {
            throw new ArgumentException("The number of columns in Matrix A must be equal to the number of rows in Matrix B for multiplication.");
        }

        double[,] resultMatrix = new double[aRows, bCols];

        for (int i = 0; i < aRows; i++)
        {
            for (int j = 0; j < bCols; j++)
            {
                double sum = 0.0;
                double compensation = 0.0;

                for (int k = 0; k < aCols; k++)
                {
                    double y = matrixA[i, k] * matrixB[k, j] - compensation;
                    double tempSum = sum + y;
                    compensation = (tempSum - sum) - y;
                    sum = tempSum;
                }

                resultMatrix[i, j] = sum;
            }
        }

        return resultMatrix;
    }

    private static double Min(double[,] matrix)
    {
        var min = double.MaxValue;

        foreach(var num in matrix)
        {
            if(num < min)
            {
                min = num;
            }
        }

        return min;
    }
    
    private static double[,] Sum(double[,] matrixA, double[,] matrixB)
    {
        int rows = matrixA.GetLength(0);
        int cols = matrixA.GetLength(1);

        if (rows != matrixB.GetLength(0) || cols != matrixB.GetLength(1))
        {
            throw new ArgumentException("Matrices must have the same dimensions");
        }

        double[,] result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = matrixA[i, j] + matrixB[i, j];
            }
        }

        return result;
    }

    private static double[,] Minus(double[,] matrixA, double[,] matrixB)
    {
        int rows = matrixA.GetLength(0);
        int cols = matrixA.GetLength(1);

        if (rows != matrixB.GetLength(0) || cols != matrixB.GetLength(1))
        {
            throw new ArgumentException("Matrices must have the same dimensions");
        }

        double[,] result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = matrixA[i, j] - matrixB[i, j];
            }
        }

        return result;
    }
    
    private static double[,] Multiply(double[,] matrixA, double[,] matrixB)
    {
        int aRows = matrixA.GetLength(0);
        int aCols = matrixA.GetLength(1);
        int bRows = matrixB.GetLength(0);
        int bCols = matrixB.GetLength(1);

        if (aCols != bRows)
        {
            throw new ArgumentException("The number of columns in Matrix A must be equal to the number of rows in Matrix B");
        }

        double[,] resultMatrix = new double[aRows, bCols];

        for (int i = 0; i < aRows; i++)
        {
            for (int j = 0; j < bCols; j++)
            {
                resultMatrix[i, j] = 0;
                for (int k = 0; k < aCols; k++)
                {
                    resultMatrix[i, j] += matrixA[i, k] * matrixB[k, j];
                }
            }
        }

        return resultMatrix;
    }

    private static double[,] Multiply(double number, double[,] matrix)
    {
        var result = new double[matrix.GetLength(0), matrix.GetLength(1)];

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                result[i, j] = number * matrix[i, j];
            }
        }

        return result;
    }

    private static double[] Multiply(double[] vector, double[,] matrix)
    {
        int vectorLength = vector.Length;
        int matrixRows = matrix.GetLength(0);
        int matrixCols = matrix.GetLength(1);

        if (vectorLength != matrixRows)
        {
            throw new ArgumentException("The length of the vector must match the number of rows in the matrix.");
        }

        double[] resultVector = new double[matrixCols];

        for (int j = 0; j < matrixCols; j++)
        {
            resultVector[j] = 0.0;
            for (int i = 0; i < vectorLength; i++)
            {
                resultVector[j] += vector[i] * matrix[i, j];
            }
        }

        return resultVector;
    }

     private static double[] MultiplyKahan(double[] vector, double[,] matrix)
    {
        int vectorLength = vector.Length;
        int matrixRows = matrix.GetLength(0);
        int matrixCols = matrix.GetLength(1);

        if (vectorLength != matrixRows)
        {
            throw new ArgumentException("The length of the vector must match the number of rows in the matrix.");
        }

        double[] resultVector = new double[matrixCols];

        for (int j = 0; j < matrixCols; j++)
        {
            double sum = 0.0;
            double c = 0.0;

            for (int i = 0; i < vectorLength; i++)
            {
                double y = vector[i] * matrix[i, j] - c;
                double t = sum + y;
                c = (t - sum) - y;
                sum = t;
            }

            resultVector[j] = sum;
        }

        return resultVector;
    }

    private static double[,] Substitude(double[,] matrixA, double[,] matrixB)
    {
        int rows = matrixA.GetLength(0);
        int cols = matrixA.GetLength(1);

        if (rows != matrixB.GetLength(0) || cols != matrixB.GetLength(1))
        {
            throw new ArgumentException("Matrices must have the same dimensions for subtraction.");
        }

        double[,] result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = matrixA[i, j] - matrixB[i, j];
            }
        }

        return result;
    }

    private static double[] Substitude(double[] vectorA, double[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Both vectors must have the same length.");
        }

        double[] resultVector = new double[vectorA.Length];

        for (int i = 0; i < vectorA.Length; i++)
        {
            resultVector[i] = vectorA[i] - vectorB[i];
        }

        return resultVector;
    }

    private static void PrintMatrix(double[,] matrix)
    {
        lock (printResultLock)
        {
            Console.WriteLine("First 3 elements of result matrix: ");
            var line = new StringBuilder();
            for (int j = 0; j < (matrix.GetLength(1) > 3 ? 3 : matrix.GetLength(1)); j++)
            {
                line.Append(matrix[0, j]);
                if (j != matrix.GetLength(1) - 1)
                {
                    line.Append(' ');
                }
            }

            Console.WriteLine(line.ToString());
        }
    }

    private static void PrintVector(double[] vector)
    {
        lock (printResultLock)
        {
            Console.WriteLine("First 3 elements of result vector: ");
            var line = new StringBuilder();
            for (int j = 0; j < (vector.Length > 3 ? 3 : vector.Length); j++)
            {
                line.Append(vector[j]);
                if (j != vector.Length - 1)
                {
                    line.Append(' ');
                }
            }

            Console.WriteLine(line.ToString());
        }
    }
}