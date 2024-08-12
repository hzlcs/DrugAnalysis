using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using ScottPlot;

namespace ChartEditLibrary.Model
{
    public class PCAManager
    {
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


        public static SamplePCA[] GetPCA(AreaDatabase[] databases)
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
                    dataX.Add(sample.Rows.Select(v => (double)v.Areas[i]!.Value).ToArray());
                }
            }

            int numOfDimensions = dataX[0].Length; //Number of input dimensions
            int newDimensions = 2; //Number of dimensions to be reduced to
            int numberOfData = dataX.Count; //Number of all data

            //Calculate mean of all data
            double[] mi = Mi(dataX, numOfDimensions);

            //Calculate variance of all data
            double[] variance = Variance(dataX, numOfDimensions, mi);

            //Calculate covariance of all data
            double[,] coverianceMatrix = Covariance(dataX, numOfDimensions, mi, variance);

            //Get sorted eigenvelues and eigenvectors
            Accord.Math.Decompositions.EigenvalueDecomposition dve = new Accord.Math.Decompositions.EigenvalueDecomposition(coverianceMatrix, false, true);

            //Calculate projection matrix using 2 eigenvectors associated with 2 largest eigenvalues
            double[,] W = new double[dve.Eigenvectors.GetLength(1), newDimensions];
            for (int i = 0; i < dve.Eigenvectors.GetLength(1); i++)
            {
                for (int q = 0; q < newDimensions; q++)
                {
                    W[i, q] = dve.Eigenvectors[i, q];
                }
            }

            //Calculate z
            List<double[]> z = new List<double[]>();
            for (int i = 0; i < numberOfData; i++)
                z.Add(Matrix.Dot(Matrix.Transpose(W), Elementwise.Subtract(dataX[i], mi)));
            foreach(var item in result)
            {
                item.Points = z.GetRange(item.ResultIndex, item.SampleNames.Length).Select(v => new PointF((float)v[0], (float)v[1])).ToArray();
            }
            return result;
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
}