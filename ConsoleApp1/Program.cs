// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ChartEditLibrary.Model;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using ScottPlot.Plottables;
using System.Text;
using System;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;


BenchmarkRunner.Run<BenchmarkProgram.FileIO>();

Console.ReadLine();


#if DEBUG

#else
BenchmarkRunner.Run<BenchmarkProgram>();
#endif

public class BenchmarkProgram
{
    public class DataLine
    {
        [Benchmark]
        public string GetDataLine()
        {
            string line = "1\t,2\t,3\t,4\t,5\t,6\t,7\t,\t,8\t,9\t,10\t";
            int index = line.IndexOf(',');
            if (index == -1)
                return line;
            while (true)
            {
                int next = line.IndexOf(',', index + 1);
                if (next == -1)
                {
                    return line;
                }
                int length = next - index;
                if (length == 1 || (length == 2 && line[index + 1] == '\t'))
                    return line[..index];
                index = next;
            }
        }

        //[Benchmark]
        //public string GetDataLineSpan()
        //{
        //    string line = "1\t,2\t,3\t,4\t,5\t,6\t,7\t,\t,8\t,9\t,10\t";
        //    var span = line.AsSpan();
        //    foreach (var range in span.Split(','))
        //    {
        //        int length = range.End.Value - range.Start.Value;
        //        if (length == 1 && span[range.Start] == '\t')
        //            return line[..(range.Start.Value - 1)];
        //    }
        //    return line;
        //}
    }

    public class FileIO
    {
        [Benchmark]
        public void ReadSplit()
        {
            string path = @"E:\WeChat Files\wxid_3cmszo7zcnp922\FileStorage\File\2025-03\CS953A-dp4.csv";
            using (StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var data = sr.ReadToEnd().Split(Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
            }
        }
        [Benchmark]
        public void ReadLine()
        {
            string path = @"E:\WeChat Files\wxid_3cmszo7zcnp922\FileStorage\File\2025-03\CS953A-dp4.csv";
            List<string[]> datas = new();
            using (StreamReader streamReader = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                string? line;
                while (true)
                {
                    line = streamReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    datas.Add(line.Split(','));
                }
            }
        }
    }

    public class NumericComparer 
    {


    }

}




internal class PrincipalComponentProgram
{
    public record PCAResult(double[][] TransformedData, double[] SingularValues, double[] EigenVectors);


    public static PCAResult Calculate(double[][] data)
    {

        double[] means;
        double[] stds;
        double[][] stdX = MatStandardize(data,
          out means, out stds);

        //VecShow(means, 4, 9);
        //VecShow(stds, 4, 9);

        //Console.WriteLine("\nFirst three data items ");
        //for (int i = 0; i < stdX.Length; ++i)
        //    VecShow(stdX[i], 4, 9);
        //Console.WriteLine(". . . ");

        //Console.WriteLine("\nComputing covariance matrix: ");
        double[][] covarMat = CovarMatrix(stdX, false);
        //MatShow(covarMat, 4, 9);

        //Console.WriteLine("\nComputing and sorting" +
        //  " eigenvalues and eigenvectors: ");

        double[] eigenVals;
        double[][] eigenVecs;
        Eigen(covarMat, out eigenVals, out eigenVecs);

        // sort eigenvals from large to smallest
        var idxs = ArgSort(eigenVals);  // save to sort evecs
        Array.Reverse(idxs);
        //VecShow(idxs, 3);

        Array.Sort(eigenVals);
        Array.Reverse(eigenVals);

        //Console.WriteLine("\nEigenvalues (sorted): ");
        //VecShow(eigenVals, 4, 9);

        eigenVecs = MatExtractCols(eigenVecs, idxs);  // sort 
        eigenVecs = MatTranspose(eigenVecs);  // as rows

        //Console.WriteLine("\nEigenvectors (sorted, rows): ");
        //MatShow(eigenVecs, 4, 9);

        //Console.WriteLine("\nComputing variance" +
        //  " explained: ");
        var sum = 0.0;
        for (var i = 0; i < eigenVals.Length; ++i)
            sum += eigenVals[i];
        var eigenVectors = new double[2];
        for (var i = 0; i < eigenVectors.Length; ++i)
        {
            var pctExplained = eigenVals[i] / sum;
            eigenVectors[i] = pctExplained;
        }

        //Console.WriteLine("\nComputing transformed" +
        //  " data (2 components): ");
        double[][] transformed =
          MatProduct(stdX, MatTranspose(eigenVecs));  // all 
        double[][] reduced = MatExtractCols(transformed,
          new int[] { 0, 1 });  // first 2 
        var reducedF = new float[reduced.Length * reduced[0].Length];
        for (int i = 0, c = 0; i < reduced.Length; i++)
        {
            for (var j = 0; j < reduced[i].Length; j++)
            {
                reducedF[c++] = (float)reduced[i][j];
            }
        }
        //for (int i = 0; i < reduced.Length; ++i)
        //    VecShow(reduced[i], 4, 9);
        var singularValues = Singular(reduced);
        var res = new PCAResult(reduced, singularValues, eigenVectors);
        return res;
    } // Main

    static double[] Singular(double[][] data)
    {
        var matrix = Matrix<double>.Build.DenseOfRowArrays(data);
        var svd = matrix.Svd();
        var singularValues = svd.S;
        return singularValues.ToArray();
    }


    static double[][] MatStandardize(double[][] data,
      out double[] means, out double[] stds)
    {
        // scikit style z-score biased normalization
        var rows = data.Length;
        var cols = data[0].Length;
        double[][] result = MatCreate(rows, cols);

        // compute means
        var mns = new double[cols];
        for (var j = 0; j < cols; ++j)
        {
            var sum = 0.0;
            for (var i = 0; i < rows; ++i)
                sum += data[i][j];
            mns[j] = sum / rows;
        } // j

        // compute std devs
        var sds = new double[cols];
        for (var j = 0; j < cols; ++j)
        {
            var sum = 0.0;
            for (var i = 0; i < rows; ++i)
                sum += (data[i][j] - mns[j]) *
                  (data[i][j] - mns[j]);
            sds[j] = Math.Sqrt(sum / rows);  // biased version
        } // j

        // normalize
        for (var j = 0; j < cols; ++j)
        {
            for (var i = 0; i < rows; ++i)
                result[i][j] = (data[i][j] - mns[j]) / sds[j];
        } // j

        means = mns;
        stds = sds;

        return result;
    }


    static int[] ArgSort(double[] vec)
    {
        var n = vec.Length;
        var idxs = new int[n];
        for (var i = 0; i < n; ++i)
            idxs[i] = i;
        Array.Sort(vec, idxs);  // sort idxs based on vec vals
        return idxs;
    }


    static double[][] MatExtractCols(double[][] mat,
      int[] cols)
    {
        var srcRows = mat.Length;
        var srcCols = mat[0].Length;
        var tgtCols = cols.Length;

        double[][] result = MatCreate(srcRows, tgtCols);
        for (var i = 0; i < srcRows; ++i)
        {
            for (var j = 0; j < tgtCols; ++j)
            {
                var c = cols[j];
                result[i][j] = mat[i][c];
            }
        }
        return result;
    }


    static double Covariance(double[] v1, double[] v2)
    {
        // compute means of v1 and v2
        var n = v1.Length;

        var sum1 = 0.0;
        for (var i = 0; i < n; ++i)
            sum1 += v1[i];
        var mean1 = sum1 / n;

        var sum2 = 0.0;
        for (var i = 0; i < n; ++i)
            sum2 += v2[i];
        var mean2 = sum2 / n;

        // compute covariance
        var sum = 0.0;
        for (var i = 0; i < n; ++i)
            sum += (v1[i] - mean1) * (v2[i] - mean2);
        var result = sum / (n - 1);

        return result;
    }


    static double[][] CovarMatrix(double[][] data,
      bool rowVar)
    {
        // rowVar == true means each row is a variable
        // if false, each column is a variable

        double[][] source;
        if (rowVar == true)
            source = data;  // by ref
        else
            source = MatTranspose(data);

        var srcRows = source.Length;  // num features
        var srcCols = source[0].Length;  // not used

        double[][] result = MatCreate(srcRows, srcRows);

        for (var i = 0; i < result.Length; ++i)
        {
            for (var j = 0; j <= i; ++j)
            {
                result[i][j] = Covariance(source[i], source[j]);
                result[j][i] = result[i][j];
            }
        }

        return result;
    }


    static void MatDecomposeQR(double[][] mat,
      out double[][] q, out double[][] r,
      bool standardize)
    {
        // QR decomposition, Householder algorithm.
        // assumes square matrix

        var n = mat.Length;  // assumes mat is nxn
        var nCols = mat[0].Length;
        if (n != nCols) Console.WriteLine("M not square ");

        double[][] Q = MatIdentity(n);
        double[][] R = MatCopy(mat);
        for (var i = 0; i < n - 1; ++i)
        {
            double[][] H = MatIdentity(n);
            var a = new double[n - i];
            var k = 0;
            for (var ii = i; ii < n; ++ii)
                a[k++] = R[ii][i];

            var normA = VecNorm(a);
            if (a[0] < 0.0) { normA = -normA; }
            var v = new double[a.Length];
            for (var j = 0; j < v.Length; ++j)
                v[j] = a[j] / (a[0] + normA);
            v[0] = 1.0;

            double[][] h = MatIdentity(a.Length);
            var vvDot = VecDot(v, v);
            double[][] alpha = VecToMat(v, v.Length, 1);
            double[][] beta = VecToMat(v, 1, v.Length);
            double[][] aMultB = MatProduct(alpha, beta);

            for (var ii = 0; ii < h.Length; ++ii)
                for (var jj = 0; jj < h[0].Length; ++jj)
                    h[ii][jj] -= (2.0 / vvDot) * aMultB[ii][jj];

            // copy h into lower right of H
            var d = n - h.Length;
            for (var ii = 0; ii < h.Length; ++ii)
                for (var jj = 0; jj < h[0].Length; ++jj)
                    H[ii + d][jj + d] = h[ii][jj];

            Q = MatProduct(Q, H);
            R = MatProduct(H, R);
        } // i

        if (standardize == true)
        {
            // standardize so R diagonal is all positive
            double[][] D = MatCreate(n, n);
            for (var i = 0; i < n; ++i)
            {
                if (R[i][i] < 0.0) D[i][i] = -1.0;
                else D[i][i] = 1.0;
            }
            Q = MatProduct(Q, D);
            R = MatProduct(D, R);
        }

        q = Q;
        r = R;

    } // QR decomposition


    static void Eigen(double[][] M,
      out double[] eigenVals, out double[][] eigenVecs)
    {
        // compute eigenvalues eigenvectors at the same time

        var n = M.Length;
        double[][] X = MatCopy(M);  // mat must be square
        double[][] Q; double[][] R;
        double[][] pq = MatIdentity(n);
        var maxCt = 10000;

        var ct = 0;
        while (ct < maxCt)
        {
            MatDecomposeQR(X, out Q, out R, false);
            pq = MatProduct(pq, Q);
            X = MatProduct(R, Q);  // note order
            ++ct;

            if (MatIsUpperTri(X, 1.0e-8) == true)
                break;
        }

        // eigenvalues are diag elements of X
        var evals = new double[n];
        for (var i = 0; i < n; ++i)
            evals[i] = X[i][i];

        // eigenvectors are columns of pq
        double[][] evecs = MatCopy(pq);

        eigenVals = evals;
        eigenVecs = evecs;
    }


    static double[][] MatCreate(int rows, int cols)
    {
        double[][] result = new double[rows][];
        for (var i = 0; i < rows; ++i)
            result[i] = new double[cols];
        return result;
    }


    static double[][] MatCopy(double[][] m)
    {
        var nRows = m.Length; var nCols = m[0].Length;
        double[][] result = MatCreate(nRows, nCols);
        for (var i = 0; i < nRows; ++i)
            for (var j = 0; j < nCols; ++j)
                result[i][j] = m[i][j];
        return result;
    }


    static double[][] MatProduct(double[][] matA,
      double[][] matB)
    {
        var aRows = matA.Length;
        var aCols = matA[0].Length;
        var bRows = matB.Length;
        var bCols = matB[0].Length;
        if (aCols != bRows)
            throw new Exception("Non-conformable matrices");

        double[][] result = MatCreate(aRows, bCols);

        for (var i = 0; i < aRows; ++i) // each row of A
            for (var j = 0; j < bCols; ++j) // each col of B
                for (var k = 0; k < aCols; ++k)
                    result[i][j] += matA[i][k] * matB[k][j];

        return result;
    }


    static bool MatIsUpperTri(double[][] mat,
      double tol)
    {
        var n = mat.Length;
        for (var i = 0; i < n; ++i)
        {
            for (var j = 0; j < i; ++j)
            {  // check lower vals
                if (Math.Abs(mat[i][j]) > tol)
                {
                    return false;
                }
            }
        }
        return true;
    }


    static double[][] MatIdentity(int n)
    {
        double[][] result = MatCreate(n, n);
        for (var i = 0; i < n; ++i)
            result[i][i] = 1.0;
        return result;
    }


    static double[][] MatTranspose(double[][] m)
    {
        var nr = m.Length;
        var nc = m[0].Length;
        double[][] result = MatCreate(nc, nr);  // note
        for (var i = 0; i < nr; ++i)
            for (var j = 0; j < nc; ++j)
                result[j][i] = m[i][j];
        return result;
    }


    static void MatShow(double[][] m, int dec, int wid)
    {
        for (var i = 0; i < m.Length; ++i)
        {
            for (var j = 0; j < m[0].Length; ++j)
            {
                var v = m[i][j];
                if (Math.Abs(v) < 1.0e-5) v = 0.0;
                Console.Write(v.ToString("F" + dec).PadLeft(wid));
            }
            Console.WriteLine("");
        }
    }


    static double VecDot(double[] v1, double[] v2)
    {
        var result = 0.0;
        var n = v1.Length;
        for (var i = 0; i < n; ++i)
            result += v1[i] * v2[i];
        return result;
    }


    static double VecNorm(double[] vec)
    {
        var n = vec.Length;
        var sum = 0.0;
        for (var i = 0; i < n; ++i)
            sum += vec[i] * vec[i];
        return Math.Sqrt(sum);
    }


    static double[][] VecToMat(double[] vec,
      int nRows, int nCols)
    {
        double[][] result = MatCreate(nRows, nCols);
        var k = 0;
        for (var i = 0; i < nRows; ++i)
            for (var j = 0; j < nCols; ++j)
                result[i][j] = vec[k++];
        return result;
    }


    static void VecShow(double[] vec,
      int dec, int wid)
    {
        for (var i = 0; i < vec.Length; ++i)
        {
            var x = vec[i];
            if (Math.Abs(x) < 1.0e-8) x = 0.0;
            Console.Write(x.ToString("F" + dec).
              PadLeft(wid));
        }
        Console.WriteLine("");
    }

}


internal interface IMinMaxValue<TSelf> where TSelf : IMinMaxValue<TSelf>?
{
    static abstract TSelf MaxValue { get; }
    static abstract TSelf MinValue { get; }
}