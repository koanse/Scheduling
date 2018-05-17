﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;
using System.IO;


namespace Scheduling
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
    [Serializable]
    public class ExMatrix
    {
        int m, n;
        double[,] elements;
        public int M
        {
            get
            {
                return m;
            }
        }
        public int N
        {
            get
            {
                return n;
            }
        }
        public double[,] Elements
        {
            get
            {
                return elements;
            }
            set
            {
                if (value.GetLength(0) <= 0 || value.GetLength(1) <= 0)
                    throw new ArgumentException();
                this.elements = value;
                this.m = elements.GetLength(0);
                this.n = elements.GetLength(1);
            }
        }
        public ExMatrix(int n)
        {
            if (n <= 0)
                throw new ArgumentException();
            this.m = this.n = n;
            this.elements = new double[n, n];
            for (int i = 0; i < n; i++)
                elements[i, i] = 1;
        }
        public ExMatrix(int m, int n)
        {
            if (m <= 0 || n <= 0)
                throw new ArgumentException();
            this.m = m;
            this.n = n;
            elements = new double[m, n];
        }
        public ExMatrix(double[,] elements)
        {
            this.m = elements.GetLength(0);
            this.n = elements.GetLength(1);
            if (m <= 0 || n <= 0)
                throw new ArgumentException();
            this.elements = elements;
        }
        public ExMatrix(ExMatrix[] matrices)
        {
            int rows = matrices[0].M;
            int cols = 0;
            foreach (ExMatrix matr in matrices)
            {
                if (matr.M != rows)
                    throw new ArgumentException();
                cols += matr.N;
            }
            this.elements = new double[rows, cols];
            this.m = rows;
            this.n = cols;
            cols = 0;
            foreach (ExMatrix matr in matrices)
            {
                for (int j = 0; j < matr.N; j++)
                    for (int i = 0; i < matr.M; i++)
                        elements[i, cols + j] = matr.Elements[i, j];
                cols += matr.N;
            }
        }
        public ExMatrix(StreamReader r)
        {
            StreamRead(r);
        }
        public static ExMatrix operator +(ExMatrix matr1, ExMatrix matr2)
        {
            if (matr1.M != matr2.M || matr1.N != matr2.N)
                throw new ArgumentException();
            ExMatrix res = new ExMatrix(matr1.M, matr1.N);
            for (int i = 0; i < res.M; i++)
                for (int j = 0; j < res.N; j++)
                    res.Elements[i, j] = matr1.Elements[i, j]
                        + matr2.Elements[i, j];
            return res;
        }
        public static ExMatrix operator -(ExMatrix matr1, ExMatrix matr2)
        {
            if (matr1.M != matr2.M || matr1.N != matr2.N)
                throw new ArgumentException();
            ExMatrix res = new ExMatrix(matr1.M, matr1.N);
            for (int i = 0; i < res.M; i++)
                for (int j = 0; j < res.N; j++)
                    res.Elements[i, j] = matr1.Elements[i, j]
                        - matr2.Elements[i, j];
            return res;
        }
        public static ExMatrix operator *(ExMatrix matr1, ExMatrix matr2)
        {
            if (matr1.N != matr2.M)
                throw new ArgumentException();
            ExMatrix res = new ExMatrix(matr1.M, matr2.N);
            for (int i = 0; i < res.M; i++)
                for (int j = 0; j < res.N; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matr1.N; k++)
                        sum += matr1.Elements[i, k] * matr2.Elements[k, j];
                    res.Elements[i, j] = sum;
                }
            return res;
        }
        public string ToHtml()
        {
            string s = "";
            s += "<TABLE BORDER = 2>";
            for (int i = 0; i < m; i++)
            {
                s += "<TR>";
                for (int j = 0; j < n; j++)
                    s += "<TD>" + elements[i, j].ToString() + "</TD>";
                s += "</TR>";
            }
            s += "</TABLE>";
            return s;
        }
        public void StreamWrite(StreamWriter w)
        {
            w.WriteLine("Строк " + m.ToString());
            w.WriteLine("Столбцов " + n.ToString());
            w.WriteLine("Элементы");
            for (int i = 0; i < m; i++)
            {
                string s = "";
                for (int j = 0; j < n; j++)
                {
                    if (j > 0)
                        s += "\t";
                    s += elements[i, j].ToString();
                }
                w.WriteLine(s);
            }
        }
        public void StreamRead(StreamReader r)
        {
            char[] sep = new char[4] { ' ', '\t', '\r', '\n' };
            string[] arrStr = r.ReadLine().Split(sep);
            m = (int)int.Parse(arrStr[1]);
            arrStr = r.ReadLine().Split(sep);
            n = (int)int.Parse(arrStr[1]);
            r.ReadLine();
            elements = new double[m, n];
            for (int i = 0; i < m; i++)
            {
                arrStr = r.ReadLine().Split(sep);
                for (int j = 0; j < n; j++)
                    elements[i, j] = double.Parse(arrStr[j]);
            }
        }
    }
    public class MsMethod
    {
        int varCount, eqCount, iterNum;
        int[] basIndInit;
        ExMatrix[] P;
        ExMatrix b, C;
        double cCoeff;
        public ArrayList states;
        public MsMethod(ExMatrix[] P, ExMatrix b, ExMatrix C, double cCoeff)
        {
            this.P = P;
            this.b = b;
            this.C = C;
            this.cCoeff = cCoeff;
            this.varCount = P.Length;
            this.eqCount = P[0].M;
            this.iterNum = 0;
            this.states = new ArrayList();
            this.basIndInit = null;
        }
        public void CheckUpData(int[] basInd)
        {
            this.basIndInit = basInd;
            ExMatrix matr = new ExMatrix(eqCount, varCount + 1);
            for (int j = 0; j < varCount; j++)
                for (int i = 0; i < eqCount; i++)
                    matr.Elements[i, j] = P[j].Elements[i, 0];
            for (int i = 0; i < eqCount; i++)
                matr.Elements[i, varCount] = b.Elements[i, 0];

            for (int eqNum = 0; eqNum < eqCount; eqNum++)
            {
                double elem = matr.Elements[eqNum, basInd[eqNum]];
                if (elem == 0)
                {
                    int eqIndex = eqNum + 1, varIndex = basInd[eqNum] + 1;
                    throw new ArgumentException("Нулевой коэффициент при базисной переменной номер "
                        + varIndex.ToString() + " в уравнении номер " + eqIndex.ToString() +
                        ". Выберете другой начальный базис");
                }
                for (int j = 0; j < matr.N; j++)
                    matr.Elements[eqNum, j] /= elem;
                for (int i = 0; i < matr.M; i++)
                {
                    if (i == eqNum)
                        continue;
                    elem = matr.Elements[i, basInd[eqNum]];
                    for (int j = 0; j < matr.N; j++)
                        matr.Elements[i, j] -= matr.Elements[eqNum, j] * elem;
                }
            }
            return;
        }
        public bool DoIteration()
        {
            IterationState s;
            if (iterNum > 0)
            {
                IterationState prevState = states[states.Count - 1] as IterationState;
                s = new IterationState(prevState);
            }
            else
                s = new IterationState(varCount, eqCount, P, C, b, cCoeff, basIndInit);

            states.Add(s);

            s.Cb = new ExMatrix(1, eqCount);
            for (int i = 0; i < eqCount; i++)
                s.Cb.Elements[0, i] = C.Elements[0, s.basInd[i]];
            s.gamma = s.Cb * s.Binv;
            ArrayList zcPMatrices = new ArrayList();
            ExMatrix zcC = new ExMatrix(1, varCount - eqCount);
            s.zcInd = new int[varCount - eqCount];
            int k = 0;
            for (int i = 0; i < varCount; i++)
            {
                if (s.IsBaseVar(i))
                    continue;
                s.zcInd[k] = i;
                zcPMatrices.Add(P[i]);
                zcC.Elements[0, k] = C.Elements[0, i];
                k++;
            }
            ExMatrix zcP =
                new ExMatrix(zcPMatrices.ToArray(typeof(ExMatrix)) as ExMatrix[]);
            s.zc = s.gamma * zcP - zcC;
            s.Xb = s.Binv * b;
            s.isDecisionLegal = true;
            for (int i = 0; i < s.Xb.M; i++)
                if (s.Xb.Elements[i, 0] < 0)
                {
                    s.isDecisionLegal = false;
                    break;
                }

            bool res;
            if (s.isDecisionLegal)
                res = SetSwapVarsLegalDecision(s);
            else
                res = SetSwapVarsIllegalDecision(s);
            if (res == false)
                return false;

            s.xi = new ExMatrix(eqCount, 1);
            for (int i = 0; i < s.xi.M; i++)
                if (i == s.eqIndex)
                    s.xi.Elements[i, 0] = 1 / s.alphar;
                else
                    s.xi.Elements[i, 0] = -s.alpha.Elements[i, 0] / s.alphar;

            ExMatrix matr = new ExMatrix(eqCount);
            for (int i = 0; i < matr.M; i++)
                matr.Elements[i, s.eqIndex] = s.xi.Elements[i, 0];
            s.BinvNext = matr * s.Binv;

            iterNum++;
            return true;
        }
        public void SetBasis(int[] basInd)
        {
            this.basIndInit = basInd;
            ExMatrix matr = new ExMatrix(eqCount, varCount + 1);
            for (int j = 0; j < varCount; j++)
                for (int i = 0; i < eqCount; i++)
                    matr.Elements[i, j] = P[j].Elements[i, 0];
            for (int i = 0; i < eqCount; i++)
                matr.Elements[i, varCount] = b.Elements[i, 0];

            for (int eqNum = 0; eqNum < eqCount; eqNum++)
            {
                double elem = matr.Elements[eqNum, basInd[eqNum]];
                if (elem == 0)
                {
                    int eqIndex = eqNum + 1, varIndex = basInd[eqNum] + 1;
                    throw new ArgumentException("Нулевой коэффициент при базисной переменной номер "
                        + varIndex.ToString() + " в уравнении номер " + eqIndex.ToString() +
                        ". Выберете другой начальный базис");
                }
                for (int j = 0; j < matr.N; j++)
                    matr.Elements[eqNum, j] /= elem;
                for (int i = 0; i < matr.M; i++)
                {
                    if (i == eqNum)
                        continue;
                    elem = matr.Elements[i, basInd[eqNum]];
                    for (int j = 0; j < matr.N; j++)
                        matr.Elements[i, j] -= matr.Elements[eqNum, j] * elem;
                }
            }

            for (int i = 0; i < eqCount; i++)
                for (int j = 0; j < varCount; j++)
                    P[j].Elements[i, 0] = matr.Elements[i, j];
            for (int i = 0; i < eqCount; i++)
                b.Elements[i, 0] = matr.Elements[i, varCount];

            for (int eqNum = 0; eqNum < eqCount; eqNum++)
            {
                double elem = C.Elements[0, basIndInit[eqNum]];
                for (int j = 0; j < varCount; j++)
                    C.Elements[0, j] -= P[j].Elements[eqNum, 0] * elem;
                cCoeff -= -(b.Elements[eqNum, 0]) * elem;
            }
        }
        bool SetSwapVarsLegalDecision(IterationState s)
        {
            s.basIndex = -1;
            double epslon = 0.000001;
            double min = float.MaxValue;
            for (int i = 0; i < s.zc.N; i++)
                if (s.zc.Elements[0, i] < 0 - epslon &&
                    s.zc.Elements[0, i] < min)
                {
                    min = s.zc.Elements[0, i];
                    s.basIndex = s.zcInd[i];
                }
            if (s.basIndex == -1)
                return false;

            s.alpha = s.Binv * P[s.basIndex];

            s.theta = double.MaxValue;
            for (int i = 0; i < eqCount; i++)
            {
                double tmp = s.Xb.Elements[i, 0] / s.alpha.Elements[i, 0];
                if (tmp > 0 && tmp < s.theta)
                {
                    s.oldIndex = s.basInd[i];
                    s.theta = tmp;
                    s.eqIndex = i;
                }
            }
            if (s.theta == double.MaxValue)
                throw new ArgumentException("Допустимое решение не существует");
            s.alphar = s.alpha.Elements[s.eqIndex, 0];
            return true;
        }
        bool SetSwapVarsIllegalDecision(IterationState s)
        {
            s.basIndex = -1;
            double min = double.MaxValue;
            for (int i = 0; i < eqCount; i++)
            {
                double b = s.Xb.Elements[i, 0];
                if (b >= 0)
                    continue;
                for (int j = 0; j < varCount; j++)
                {
                    if (s.IsBaseVar(j))
                        continue;
                    double a = s.GetVarCoeff(j, i);
                    if (a >= 0)
                        continue;
                    if (b / a < min)
                    {
                        s.eqIndex = i;
                        s.basIndex = j;
                        min = b / a;
                    }
                }
            }
            if (s.basIndex == -1)
                throw new ArgumentException("Допустимое решение не существует");

            s.oldIndex = s.basInd[s.eqIndex];

            s.alpha = s.Binv * P[s.basIndex];
            s.alphar = s.alpha.Elements[s.eqIndex, 0];
            return true;
        }
    }
    public class IterationState
    {
        public ExMatrix Cb, Binv, gamma, zc;
        public ExMatrix Xb, alpha, xi, BinvNext;
        ExMatrix C, b;
        ExMatrix[] P;
        public int[] basInd, zcInd;
        int[] basIndInit;
        public int basIndex, oldIndex, eqIndex;
        public double theta, alphar;
        public bool isDecisionLegal;
        int varCount, eqCount;
        double cCoeff;
        public IterationState(int varCount, int eqCount,
            ExMatrix[] P, ExMatrix C, ExMatrix b, double cCoeff,
            int[] basInd)
        {
            this.varCount = varCount;
            this.eqCount = eqCount;
            this.P = P;
            this.C = C;
            this.b = b;
            this.cCoeff = cCoeff;
            this.basInd = basInd;
            this.basIndInit = this.basInd.Clone() as int[];
            this.Binv = new ExMatrix(eqCount);
            this.basIndex = this.eqIndex = this.oldIndex = -1;
        }
        public IterationState(IterationState prevState)
        {
            this.varCount = prevState.varCount;
            this.eqCount = prevState.eqCount;
            this.P = prevState.P;
            this.C = prevState.C;
            this.b = prevState.b;
            this.cCoeff = prevState.cCoeff;
            this.basIndInit = prevState.basIndInit;
            this.basInd = prevState.basInd.Clone() as int[];
            this.basInd[prevState.eqIndex] = prevState.basIndex;
            this.Binv = prevState.BinvNext;
            this.basIndex = this.eqIndex = this.oldIndex = -1;
        }
        public string GetReport()
        {
            eqCount = Xb.M;
            string s = GetSimplexTablePart();
            try
            {
                s += "<P>Значение целевой функции ";
                s += "F = " + GetFuncValue().ToString() + "</P>";

                s += "<P>ВСПОМОГАТЕЛЬНЫЕ ПЕРЕМЕННЫЕ</P>";
                s += "<P>Базисное решение" + GetBasisDecision() + "</P>";

                s += "<P>Базисные переменные: ";
                for (int i = 0; i < eqCount; i++)
                    s += "y<SUB>" + basInd[i].ToString() + "</SUB> ";
                s += "</P>";

                s += "<P>ИСХОДНЫЕ ПЕРЕМЕННЫЕ</P>";
                s += "<P>Текущие значения исходных переменных:</P>";
                int varCountMinOne = varCount - 1;
                for (int i = 0; i < varCount - 1; i++)
                {
                    s += "<P>x<SUB>" + i.ToString() + "</SUB>" +
                        " = y<SUB>" + i.ToString() + "</SUB>" +
                        " / y<SUB>" + varCountMinOne.ToString() + "</SUB> = ";
                    if (GetVarValue(varCount - 1) == 0)
                        s += "не определено";
                    else
                        s += Math.Round(GetVarValue(i) / GetVarValue(varCount - 1), 5) + "</P>";
                }

                return s;

                /*s += "<P>Матрица C<SUB>b</SUB>";
                s += Cb.ToHtml() + "</P>";

                s += "<P>Матрица B<SUP>-1</SUP>";
                s += Binv.ToHtml() + "</P>";

                s += "<P>Матрица γ";
                s += gamma.ToHtml() + "</P>";

                s += "<P>Матрица разностей z<SUB>i</SUB> - c<SUB>i</SUB>";
                s += zc.ToHtml() + "</P>";

                s += "<P>Вектор базисных решений X<SUB>b</SUB>";
                s += Xb.ToHtml() + "</P>";

                if (isDecisionLegal)
                    s += "<P>Базисное решение является допустимым</P>";
                else
                    s += "<P>Базисное решение является недопустимым</P>";

                if (basIndex < 0)
                    throw new Exception();
                s += "<P>Выводим из базиса переменную x<SUB>" + basIndex.ToString() + "</SUB></P>";

                s += "<P>Матрица α";
                s += alpha.ToHtml() + "</P>";

                int eqNum = eqIndex + 1;
                s += "<P>Минимальное отношение θ = " + theta.ToString() + "</P>";
                s += "<P>Выводим из базиса переменную x<SUB>" + oldIndex.ToString()
                    + "</SUB> из уравнения номер " + eqNum.ToString() + "</P>";

                s += "<P>Ведущий элемент α<SUB>r</SUB> = " + alphar.ToString() + "</P>";

                s += "<P>Матрица ξ";
                s += xi.ToHtml() + "</P>";

                s += "<P>Матрица B<SUP>-1</SUP><SUB>next</SUB>";
                s += BinvNext.ToHtml() + "</P>";*/
            }
            catch { }
            return s;
        }
        public string GetBasisDecision()
        {
            string s = "<TABLE>";
            for (int i = 0; i < varCount; i++)
            {
                s += "<TR><TD>y<SUB>" + i.ToString() + "</SUB> = ";
                s += GetVarValue(i);
                s += "</TD></TR>";
            }
            return s + "</TABLE>";
        }
        public bool IsBaseVar(int varNum)
        {
            for (int i = 0; i < eqCount; i++)
                if (basInd[i] == varNum)
                    return true;
            return false;
        }
        public double GetVarCoeff(int varNum, int eqNum)
        {
            double epsilon = 0.000001;
            ExMatrix res = Binv * P[varNum];
            double coeff = res.Elements[eqNum, 0];
            if (coeff > -epsilon && coeff < epsilon)
                coeff = 0;
            return Math.Round(coeff, 5);
        }
        public double GetVarValue(int varNum)
        {
            if (IsBaseVar(varNum))
            {
                for (int eqNum = 0; eqNum < eqCount; eqNum++)
                    if (basInd[eqNum] == varNum)
                        return Math.Round(Xb.Elements[eqNum, 0], 5);
            }
            return 0;
        }
        public double GetFuncValue()
        {
            ExMatrix value = Cb * Xb;
            return Math.Round(value.Elements[0, 0] + cCoeff, 5);
        }
        public double[] GetFuncCoeffs()
        {
            double epsilon = 0.000001;
            double[] res = new double[varCount + 1];
            for (int i = 0; i < varCount; i++)
                res[i] = C.Elements[0, i];
            res[varCount] = cCoeff;
            for (int eqNum = 0; eqNum < eqCount; eqNum++)
            {
                double elem = res[basInd[eqNum]];
                for (int j = 0; j < varCount; j++)
                    res[j] -= GetVarCoeff(j, eqNum) * elem;
                res[varCount] -= -(GetVarValue(basInd[eqNum])) * elem;
            }
            for (int i = 0; i < varCount + 1; i++)
            {
                if (res[i] > -epsilon && res[i] < epsilon)
                    res[i] = 0;
                res[i] = Math.Round(res[i], 5);
            }
            double[] tmp = new double[varCount + 1];
            tmp[0] = res[varCount];
            for (int i = 0; i < varCount; i++)
                tmp[i + 1] = res[i];
            return tmp;
        }
        public string GetSimplexTablePart()
        {
            string s = "<TABLE BORDER = 1>";
            s += "<TR><TH>Базис</TH><TH>Решение</TH>";
            for (int i = 0; i < varCount; i++)
                s += "<TH>y<SUB>" + i.ToString() + "</SUB></TH>";
            s += "<TH>Отношения</TH></TR>";

            for (int i = 0; i < eqCount; i++)
            {
                s += "<TR";
                if (i == eqIndex)
                    s += " BGCOLOR = Red";
                s += "><TD>y<SUB>" + basInd[i].ToString() + "</SUB></TD>";
                s += "<TD>" + GetVarValue(basInd[i]) + "</TD>";
                for (int j = 0; j < varCount; j++)
                {
                    s += "<TD";
                    if (j == basIndex)
                        s += " BGCOLOR = LightGreen";
                    s += ">" + GetVarCoeff(j, i) + "</TD>";
                }
                if (alpha == null || alpha.Elements[i, 0] == 0)
                    s += "<TD>-</TD>";
                else
                {
                    double tmp = Math.Round(Xb.Elements[i, 0] / alpha.Elements[i, 0], 5);
                    s += "<TD>" + tmp.ToString() + "</TD>";
                }
                s += "</TR>";
            }

            double[] arrCoeffs = GetFuncCoeffs();
            for (int i = 1; i < arrCoeffs.Length; i++)
                arrCoeffs[i] *= -1;
            s += "<TR BGCOLOR = LightYellow><TD>F</TD>";
            for (int j = 0; j < varCount + 1; j++)
                s += "<TD>" + arrCoeffs[j] + "</TD>";
            s += "<TD>-</TD></TR>";
            return s + "</TABLE>";
        }
        public string GetReport(string[] varNames, bool max)
        {
            eqCount = Xb.M;
            string s = GetSimplexTablePart(varNames, false);
            try
            {
                if (max)
                    s += string.Format("<P>Значение целевой функции: F = {0}</P>",
                        GetFuncValue());
                else
                    s += string.Format("<P>Значение целевой функции: F = {0}</P>",
                        -GetFuncValue());

                s += "<P>Базисное решение:" + GetBasisDecision(varNames) + "</P>";

                /*s += "<P>Базисные переменные: ";
                for (int i = 0; i < eqCount; i++)
                    s += "y<SUB>" + basInd[i].ToString() + "</SUB> ";
                s += "</P>";

                s += "<P>ИСХОДНЫЕ ПЕРЕМЕННЫЕ</P>";
                s += "<P>Текущие значения исходных переменных:</P>";
                int varCountMinOne = varCount - 1;
                for (int i = 0; i < varCount - 1; i++)
                {
                    s += "<P>x<SUB>" + i.ToString() + "</SUB>" +
                        " = y<SUB>" + i.ToString() + "</SUB>" +
                        " / y<SUB>" + varCountMinOne.ToString() + "</SUB> = ";
                    if (GetVarValue(varCount - 1) == 0)
                        s += "не определено";
                    else
                        s += Math.Round(GetVarValue(i) / GetVarValue(varCount - 1), 5) + "</P>";
                }

                return s;

                s += "<P>Матрица C<SUB>b</SUB>";
                s += Cb.ToHtml() + "</P>";

                s += "<P>Матрица B<SUP>-1</SUP>";
                s += Binv.ToHtml() + "</P>";

                s += "<P>Матрица γ";
                s += gamma.ToHtml() + "</P>";

                s += "<P>Матрица разностей z<SUB>i</SUB> - c<SUB>i</SUB>";
                s += zc.ToHtml() + "</P>";

                s += "<P>Вектор базисных решений X<SUB>b</SUB>";
                s += Xb.ToHtml() + "</P>";

                if (isDecisionLegal)
                    s += "<P>Базисное решение является допустимым</P>";
                else
                    s += "<P>Базисное решение является недопустимым</P>";

                if (basIndex < 0)
                    throw new Exception();
                s += "<P>Выводим из базиса переменную x<SUB>" + basIndex.ToString() + "</SUB></P>";

                s += "<P>Матрица α";
                s += alpha.ToHtml() + "</P>";

                int eqNum = eqIndex + 1;
                s += "<P>Минимальное отношение θ = " + theta.ToString() + "</P>";
                s += "<P>Выводим из базиса переменную x<SUB>" + oldIndex.ToString()
                    + "</SUB> из уравнения номер " + eqNum.ToString() + "</P>";

                s += "<P>Ведущий элемент α<SUB>r</SUB> = " + alphar.ToString() + "</P>";

                s += "<P>Матрица ξ";
                s += xi.ToHtml() + "</P>";

                s += "<P>Матрица B<SUP>-1</SUP><SUB>next</SUB>";
                s += BinvNext.ToHtml() + "</P>";*/
            }
            catch { }
            return s;
        }
        public string GetBasisDecision(string[] varNames)
        {
            string s = "<TABLE>";
            for (int i = 0; i < varCount; i++)
            {
                s += "<TR><TD>" + varNames[i] + " = ";
                s += GetVarValue(i);
                s += "</TD></TR>";
            }
            return s + "</TABLE>";
        }
        public string GetSimplexTablePart(string[] varNames, bool max)
        {
            string s = "<TABLE BORDER = 1>";
            s += "<TR><TH>Базис</TH><TH>Решение</TH>";
            for (int i = 0; i < varCount; i++)
                s += "<TH>" + varNames[i] + "</TH>";
            s += "<TH>Отношения</TH></TR>";

            for (int i = 0; i < eqCount; i++)
            {
                s += "<TR";
                if (i == eqIndex)
                    s += " BGCOLOR = Red";
                s += "><TD>" + varNames[basInd[i]] + "</TD>";
                s += "<TD>" + GetVarValue(basInd[i]) + "</TD>";
                for (int j = 0; j < varCount; j++)
                {
                    s += "<TD";
                    if (j == basIndex)
                        s += " BGCOLOR = LightGreen";
                    s += ">" + GetVarCoeff(j, i) + "</TD>";
                }
                if (alpha == null || alpha.Elements[i, 0] == 0)
                    s += "<TD>-</TD>";
                else
                {
                    double tmp = Math.Round(Xb.Elements[i, 0] / alpha.Elements[i, 0], 5);
                    s += "<TD>" + tmp.ToString() + "</TD>";
                }
                s += "</TR>";
            }

            double[] arrCoeffs = GetFuncCoeffs();
            if (max)
                for (int i = 1; i < arrCoeffs.Length; i++)
                    arrCoeffs[i] *= -1;
            s += "<TR BGCOLOR = LightYellow><TD>F</TD>";
            for (int j = 0; j < varCount + 1; j++)
                s += "<TD>" + arrCoeffs[j] + "</TD>";
            s += "<TD>-</TD></TR>";
            return s + "</TABLE>";
        }
    }
}
