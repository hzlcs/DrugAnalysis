using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using ScottPlot;
using OpenTK.Mathematics;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace ChartEditLibrary.Model
{
    public class PCAManager
    {
        public record Result(SamplePCA[] samples, double[] singularValues, double[] eigenVectors);


        public class SamplePCA(AreaDatabase database) : AreaDatabase(database)
        {
            public PointF[] Points { get; internal set; } = null!;
            internal int ResultIndex { get; set; }
        }


        public class PCAItem(string className, AreaDatabase areaDatabase)
        {
            public string ClassName { get; set; } = className;
            public AreaDatabase AreaDatabase { get; set; } = areaDatabase;
            public int ResultIndex { get; internal set; }
        }


        public static Result GetPCA(AreaDatabase[] databases)
        {
            List<double[]> dataX = new List<double[]>(); //All data of all classes together
            List<string> classes = new List<string>(); //List of classes
            SamplePCA[] result = databases.Select(v => new SamplePCA(v)).ToArray(); //Result of PCA for each class 
            foreach (var item in result)
            {
                var sample = item;
                sample.ResultIndex = classes.Count;
                for (int i = 0; i < sample.SampleNames.Length; ++i)
                {
                    classes.Add(sample.SampleNames[i]);
                    dataX.Add(sample.Rows.Select(v => v.Areas[i]).Where(v => v.HasValue).Select(v => Math.Round(v.GetValueOrDefault(), 2)).ToArray());
                }
            }
            

            var res = PrincipalComponentProgram.Calculate(dataX.ToArray());
            //Calculate z
            List<double[]> z = new List<double[]>(res.TransformedData);

            foreach (var item in result)
            {
                item.Points = z.GetRange(item.ResultIndex, item.SampleNames.Length).Select(v => new PointF((float)v[0], (float)v[1])).ToArray();
            }
            return new Result(result, res.SingularValues, res.EigenVectors);
        }

        private static double[] Mi(List<double[]> dataX, int pocetDimenzi)
        {
            double[] mi = new double[pocetDimenzi];
            for (int y = 0; y < dataX.Count; y++) //For every N
                for (int x = 0; x < dataX[y].Length; x++) //For every element in transaction
                    mi[x] += dataX[y][x];

            for (int d = 0; d < pocetDimenzi; d++)
            {
                mi[d] = mi[d] / dataX.Count;
            }
            return mi;
        }

        private static double[] Variance(List<double[]> dataX, int dimensions, double[] mi)
        {
            double[] varOfEachColumn = new double[dimensions];
            for (int i = 0; i < dimensions; i++)
            {
                for (int j = 0; j < dataX.Count; j++)
                {
                    varOfEachColumn[i] += Math.Pow(dataX[j][i] - mi[i], 2);
                }
            }
            for (int d = 0; d < varOfEachColumn.Length; d++)
            {
                varOfEachColumn[d] = varOfEachColumn[d] / dataX.Count;
            }
            return varOfEachColumn;
        }

        private static double Coveriance(double[] dimenzeA, double meanA, double[] dimenzeB, double meanB, int numberOfData)
        {
            double result = 0;
            for (int a = 0; a < dimenzeA.Length; a++) //Both dimensions are same length, so it doesnt matter which I put here
                result += (dimenzeA[a] - meanA) * (dimenzeB[a] - meanB);
            return result / numberOfData;
        }

        private static double[,] Covariance(List<double[]> dataX, int pocetDimenzi, double[] mi, double[] varKazdehoSloupce)
        {
            //Covariance (d*d matrix)
            double[,] coverianceMatrix = new double[pocetDimenzi, pocetDimenzi];
            double[] dimenzeA_temp = new double[dataX.Count];
            double[] dimenzeB_temp = new double[dataX.Count];

            for (int x = 0; x < pocetDimenzi; x++)
            {
                for (int y = 0; y < pocetDimenzi; y++)
                {
                    if (x == y) //Variance on the diagonal
                    {
                        coverianceMatrix[x, y] = varKazdehoSloupce[x];
                    }
                    else
                    {
                        for (int i = 0; i < dataX.Count; i++)
                        {
                            dimenzeA_temp[i] = dataX[i][x];
                            dimenzeB_temp[i] = dataX[i][y];
                        }

                        coverianceMatrix[x, y] = Coveriance(dimenzeA_temp, mi[x], dimenzeB_temp, mi[y], dataX.Count);
                    }
                }
            }
            return coverianceMatrix;
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
            int[] idxs = ArgSort(eigenVals);  // save to sort evecs
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
            double sum = 0.0;
            for (int i = 0; i < eigenVals.Length; ++i)
                sum += eigenVals[i];
            double[] eigenVectors = new double[2];
            for (int i = 0; i < eigenVectors.Length; ++i)
            {
                double pctExplained = eigenVals[i] / sum;
                eigenVectors[i] = pctExplained;
            }

            //Console.WriteLine("\nComputing transformed" +
            //  " data (2 components): ");
            double[][] transformed =
              MatProduct(stdX, MatTranspose(eigenVecs));  // all 
            double[][] reduced = MatExtractCols(transformed,
              new int[] { 0, 1 });  // first 2 
            float[] reducedF = new float[reduced.Length * reduced[0].Length];
            for (int i = 0, c = 0; i < reduced.Length; i++)
            {
                for (int j = 0; j < reduced[i].Length; j++)
                {
                    reducedF[c++] = (float)reduced[i][j];
                    reduced[i][j] = -reduced[i][j];
                }
            }
            //for (int i = 0; i < reduced.Length; ++i)
            //    VecShow(reduced[i], 4, 9);
            double[] singularValues = Singular(reduced);
            var res = new PCAResult(reduced, singularValues, eigenVectors);
            return res;
        } // Main

        static double[] Singular(double[][] data)
        {
            Matrix<double> matrix = Matrix<double>.Build.DenseOfRowArrays(data);
            Svd<double> svd = matrix.Svd();
            Vector<double> singularValues = svd.S;
            return singularValues.ToArray();
        }


        static double[][] MatStandardize(double[][] data,
          out double[] means, out double[] stds)
        {
            // scikit style z-score biased normalization
            int rows = data.Length;
            int cols = data[0].Length;
            double[][] result = MatCreate(rows, cols);

            // compute means
            double[] mns = new double[cols];
            for (int j = 0; j < cols; ++j)
            {
                double sum = 0.0;
                for (int i = 0; i < rows; ++i)
                    sum += data[i][j];
                mns[j] = sum / rows;
            } // j

            // compute std devs
            double[] sds = new double[cols];
            for (int j = 0; j < cols; ++j)
            {
                double sum = 0.0;
                for (int i = 0; i < rows; ++i)
                    sum += (data[i][j] - mns[j]) *
                      (data[i][j] - mns[j]);
                sds[j] = Math.Sqrt(sum / rows);  // biased version
            } // j

            // normalize
            for (int j = 0; j < cols; ++j)
            {
                for (int i = 0; i < rows; ++i)
                    result[i][j] = (data[i][j] - mns[j]) / sds[j];
            } // j

            means = mns;
            stds = sds;

            return result;
        }


        static int[] ArgSort(double[] vec)
        {
            int n = vec.Length;
            int[] idxs = new int[n];
            for (int i = 0; i < n; ++i)
                idxs[i] = i;
            Array.Sort(vec, idxs);  // sort idxs based on vec vals
            return idxs;
        }


        static double[][] MatExtractCols(double[][] mat,
          int[] cols)
        {
            int srcRows = mat.Length;
            int srcCols = mat[0].Length;
            int tgtCols = cols.Length;

            double[][] result = MatCreate(srcRows, tgtCols);
            for (int i = 0; i < srcRows; ++i)
            {
                for (int j = 0; j < tgtCols; ++j)
                {
                    int c = cols[j];
                    result[i][j] = mat[i][c];
                }
            }
            return result;
        }


        static double Covariance(double[] v1, double[] v2)
        {
            // compute means of v1 and v2
            int n = v1.Length;

            double sum1 = 0.0;
            for (int i = 0; i < n; ++i)
                sum1 += v1[i];
            double mean1 = sum1 / n;

            double sum2 = 0.0;
            for (int i = 0; i < n; ++i)
                sum2 += v2[i];
            double mean2 = sum2 / n;

            // compute covariance
            double sum = 0.0;
            for (int i = 0; i < n; ++i)
                sum += (v1[i] - mean1) * (v2[i] - mean2);
            double result = sum / (n - 1);

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

            int srcRows = source.Length;  // num features
            int srcCols = source[0].Length;  // not used

            double[][] result = MatCreate(srcRows, srcRows);

            for (int i = 0; i < result.Length; ++i)
            {
                for (int j = 0; j <= i; ++j)
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

            int n = mat.Length;  // assumes mat is nxn
            int nCols = mat[0].Length;
            if (n != nCols) Console.WriteLine("M not square ");

            double[][] Q = MatIdentity(n);
            double[][] R = MatCopy(mat);
            for (int i = 0; i < n - 1; ++i)
            {
                double[][] H = MatIdentity(n);
                double[] a = new double[n - i];
                int k = 0;
                for (int ii = i; ii < n; ++ii)
                    a[k++] = R[ii][i];

                double normA = VecNorm(a);
                if (a[0] < 0.0) { normA = -normA; }
                double[] v = new double[a.Length];
                for (int j = 0; j < v.Length; ++j)
                    v[j] = a[j] / (a[0] + normA);
                v[0] = 1.0;

                double[][] h = MatIdentity(a.Length);
                double vvDot = VecDot(v, v);
                double[][] alpha = VecToMat(v, v.Length, 1);
                double[][] beta = VecToMat(v, 1, v.Length);
                double[][] aMultB = MatProduct(alpha, beta);

                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        h[ii][jj] -= (2.0 / vvDot) * aMultB[ii][jj];

                // copy h into lower right of H
                int d = n - h.Length;
                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        H[ii + d][jj + d] = h[ii][jj];

                Q = MatProduct(Q, H);
                R = MatProduct(H, R);
            } // i

            if (standardize == true)
            {
                // standardize so R diagonal is all positive
                double[][] D = MatCreate(n, n);
                for (int i = 0; i < n; ++i)
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

            int n = M.Length;
            double[][] X = MatCopy(M);  // mat must be square
            double[][] Q; double[][] R;
            double[][] pq = MatIdentity(n);
            int maxCt = 10000;

            int ct = 0;
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
            double[] evals = new double[n];
            for (int i = 0; i < n; ++i)
                evals[i] = X[i][i];

            // eigenvectors are columns of pq
            double[][] evecs = MatCopy(pq);

            eigenVals = evals;
            eigenVecs = evecs;
        }


        static double[][] MatCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }


        static double[][] MatCopy(double[][] m)
        {
            int nRows = m.Length; int nCols = m[0].Length;
            double[][] result = MatCreate(nRows, nCols);
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = m[i][j];
            return result;
        }


        static double[][] MatProduct(double[][] matA,
          double[][] matB)
        {
            int aRows = matA.Length;
            int aCols = matA[0].Length;
            int bRows = matB.Length;
            int bCols = matB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            double[][] result = MatCreate(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k)
                        result[i][j] += matA[i][k] * matB[k][j];

            return result;
        }


        static bool MatIsUpperTri(double[][] mat,
          double tol)
        {
            int n = mat.Length;
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < i; ++j)
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
            for (int i = 0; i < n; ++i)
                result[i][i] = 1.0;
            return result;
        }


        static double[][] MatTranspose(double[][] m)
        {
            int nr = m.Length;
            int nc = m[0].Length;
            double[][] result = MatCreate(nc, nr);  // note
            for (int i = 0; i < nr; ++i)
                for (int j = 0; j < nc; ++j)
                    result[j][i] = m[i][j];
            return result;
        }




        static double VecDot(double[] v1, double[] v2)
        {
            double result = 0.0;
            int n = v1.Length;
            for (int i = 0; i < n; ++i)
                result += v1[i] * v2[i];
            return result;
        }


        static double VecNorm(double[] vec)
        {
            int n = vec.Length;
            double sum = 0.0;
            for (int i = 0; i < n; ++i)
                sum += vec[i] * vec[i];
            return Math.Sqrt(sum);
        }


        static double[][] VecToMat(double[] vec,
          int nRows, int nCols)
        {
            double[][] result = MatCreate(nRows, nCols);
            int k = 0;
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = vec[k++];
            return result;
        }


    }
}
